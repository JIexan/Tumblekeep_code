using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.LevelUpgrades {
	[System.Serializable]
	public class LevelUpgradeEntry {
		[SerializeField] string id;
		[SerializeReference] List<BaseUpgradeRequirement> requirements = new();
		[SerializeReference] List<BaseUpgradeEffect> effects = new();
		[SerializeField] UpgradeCategory category = UpgradeCategory.Towers;
		[SerializeField] Sprite icon;
		// Апгрейд «куплен» с самого начала (стартовые башни): в дереве показан открытым,
		// эффекты активны без покупки, в сейв не пишется.
		[SerializeField] bool startPurchased;
		// Вклад в шкалу прокачки острова (LevelUpgradesManager.GetIslandProgress01). Процент считается
		// нормализованно (купленные / все), но сумму по конфигу держим равной 100 — OnValidate следит.
		[SerializeField] float progressWeight;
		// Какие виды доп. тасков уровня открывает покупка. Один вид могут открывать разные апгрейды.
		[SerializeField] List<Gameplay.TasksSystem.TaskType> unlockedTasks = new();

		public string Id => id;
		public List<BaseUpgradeRequirement> Requirements => requirements;
		public List<BaseUpgradeEffect> Effects => effects;
		public UpgradeCategory Category => category;
		public Sprite Icon => icon;
		public bool StartPurchased => startPurchased;
		public float ProgressWeight => progressWeight;
		public List<Gameplay.TasksSystem.TaskType> UnlockedTasks => unlockedTasks;

#if UNITY_EDITOR
		// Программное наполнение конфига (Level1UpgradesBuilder); в рантайме не используется.
		public void EditorInit(string id, UpgradeCategory category, Sprite icon,
			List<BaseUpgradeRequirement> requirements, List<BaseUpgradeEffect> effects, bool startPurchased = false) {
			this.id = id;
			this.category = category;
			this.icon = icon;
			this.requirements = requirements ?? new List<BaseUpgradeRequirement>();
			this.effects = effects ?? new List<BaseUpgradeEffect>();
			this.startPurchased = startPurchased;
		}

		public void EditorSetTasksData(float progressWeight, List<Gameplay.TasksSystem.TaskType> unlockedTasks) {
			this.progressWeight = progressWeight;
			this.unlockedTasks = unlockedTasks ?? new List<Gameplay.TasksSystem.TaskType>();
		}
#endif
	}

	// Набор апгрейдов одного уровня; levelId должен совпадать с Level.Id из LevelSettings.
	[CreateAssetMenu(fileName = "LevelUpgradesConfig", menuName = "Settings/LevelUpgradesConfig")]
	public class LevelUpgradesConfig : ScriptableObject {
		[SerializeField] string levelId;
		[SerializeField, Min(0)] int initialTowerLimit = 2;
		[SerializeField] TowerType defaultAvailableTowers =
			TowerType.Laser | TowerType.Arrow | TowerType.Mortar | TowerType.Electric;
		[ListDrawerSettings(ListElementLabelName = "Id")]
		[SerializeField] List<LevelUpgradeEntry> upgrades = new();

		public string LevelId => levelId;
		public int InitialTowerLimit => initialTowerLimit;
		public TowerType DefaultAvailableTowers => defaultAvailableTowers;
		public List<LevelUpgradeEntry> Upgrades => upgrades;

		public LevelUpgradeEntry GetUpgrade(string upgradeId) {
			foreach (var upgrade in upgrades) {
				if (upgrade != null && upgrade.Id == upgradeId)
					return upgrade;
			}
			return null;
		}

#if UNITY_EDITOR
		public void EditorSetInitialTowerLimit(int limit) {
			initialTowerLimit = Mathf.Max(0, limit);
		}

		public void EditorSetUpgrades(string levelId, TowerType defaultAvailableTowers, List<LevelUpgradeEntry> upgrades) {
			this.levelId = levelId;
			this.defaultAvailableTowers = defaultAvailableTowers;
			this.upgrades = upgrades ?? new List<LevelUpgradeEntry>();
		}
#endif

#if UNITY_EDITOR
		private void OnValidate() {
			var seen = new HashSet<string>();
			foreach (var upgrade in upgrades) {
				if (upgrade == null)
					continue;
				if (string.IsNullOrEmpty(upgrade.Id))
					Debug.LogWarning($"[LevelUpgradesConfig] '{name}': апгрейд с пустым id.", this);
				else if (!seen.Add(upgrade.Id))
					Debug.LogWarning($"[LevelUpgradesConfig] '{name}': дублирующийся id апгрейда '{upgrade.Id}'.", this);
			}

			foreach (var upgrade in upgrades) {
				if (upgrade != null && upgrade.StartPurchased && upgrade.Requirements.Count > 0)
					Debug.LogWarning($"[LevelUpgradesConfig] '{name}': апгрейд '{upgrade.Id}' startPurchased, но имеет требования — они никогда не проверятся.", this);
			}

			float totalWeight = 0f;
			foreach (var upgrade in upgrades) {
				if (upgrade == null)
					continue;
				totalWeight += upgrade.ProgressWeight;
				if (upgrade.StartPurchased && upgrade.ProgressWeight > 0f)
					Debug.LogWarning($"[LevelUpgradesConfig] '{name}': апгрейд '{upgrade.Id}' startPurchased, но имеет progressWeight — шкала прокачки стартует не с нуля.", this);
			}
			// 0 = веса ещё не размечены (шкала прокачки просто выключена), это не ошибка
			if (totalWeight > 0f && Mathf.Abs(totalWeight - 100f) > 0.01f)
				Debug.LogWarning($"[LevelUpgradesConfig] '{name}': сумма progressWeight = {totalWeight:0.##}, ожидается 100.", this);

			var categories = new Dictionary<string, UpgradeCategory>();
			foreach (var upgrade in upgrades) {
				if (upgrade != null && !string.IsNullOrEmpty(upgrade.Id))
					categories[upgrade.Id] = upgrade.Category;
			}

			ValidateChains(seen, categories);
		}

		// Ссылки цепочек (PurchasedUpgradesUpgradeRequirement): битые id, self-reference,
		// дубли в списке, циклы и кросс-категорийные пререквизиты. Цикл — вечный дедлок
		// без единой ошибки в рантайме, поэтому ловим его здесь. Кросс-категорийная цепочка
		// работает в рантайме, но ребро между деревьями разных категорий не рисуется.
		private void ValidateChains(HashSet<string> knownIds, Dictionary<string, UpgradeCategory> categories) {
			var prerequisites = new Dictionary<string, HashSet<string>>();

			foreach (var upgrade in upgrades) {
				if (upgrade == null || string.IsNullOrEmpty(upgrade.Id))
					continue;

				var prereqs = new HashSet<string>();
				prerequisites[upgrade.Id] = prereqs;

				foreach (var requirement in upgrade.Requirements) {
					if (requirement is not PurchasedUpgradesUpgradeRequirement chain)
						continue;

					var inThisList = new HashSet<string>();
					foreach (var refId in chain.UpgradeIds) {
						if (string.IsNullOrEmpty(refId) || !knownIds.Contains(refId))
							Debug.LogWarning($"[LevelUpgradesConfig] '{name}': апгрейд '{upgrade.Id}' требует несуществующий апгрейд '{refId}'.", this);
						else if (refId == upgrade.Id)
							Debug.LogWarning($"[LevelUpgradesConfig] '{name}': апгрейд '{upgrade.Id}' требует сам себя.", this);
						else {
							prereqs.Add(refId);
							if (categories.TryGetValue(refId, out var refCategory) && refCategory != upgrade.Category)
								Debug.LogWarning($"[LevelUpgradesConfig] '{name}': апгрейд '{upgrade.Id}' ({upgrade.Category}) требует апгрейд '{refId}' из другой категории ({refCategory}) — ребро в дереве не будет нарисовано.", this);
						}

						if (!inThisList.Add(refId))
							Debug.LogWarning($"[LevelUpgradesConfig] '{name}': апгрейд '{upgrade.Id}' дублирует требование '{refId}'.", this);
					}
				}
			}

			// Fixed-point: помечаем «достижимые» апгрейды, у которых все пререквизиты достижимы;
			// незатронутый остаток заперт в цикле. Битые/self-ссылки выше уже исключены из рёбер.
			var reachable = new HashSet<string>();
			bool changed = true;
			while (changed) {
				changed = false;
				foreach (var pair in prerequisites) {
					if (reachable.Contains(pair.Key))
						continue;
					if (pair.Value.IsSubsetOf(reachable)) {
						reachable.Add(pair.Key);
						changed = true;
					}
				}
			}

			if (reachable.Count < prerequisites.Count) {
				var stuck = new List<string>();
				foreach (var pair in prerequisites) {
					if (!reachable.Contains(pair.Key))
						stuck.Add(pair.Key);
				}
				Debug.LogWarning($"[LevelUpgradesConfig] '{name}': цикл в цепочках апгрейдов — навсегда недоступны: {string.Join(", ", stuck)}.", this);
			}
		}
#endif
	}
}
