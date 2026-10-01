using System;
using System.Collections.Generic;
using Gameplay.Relics;
using Gameplay.Unlocks;
using Sirenix.OdinInspector;
using UnityEngine;

// Настройки наградных сундуков: три тира (Bronze/Iron/Gold), у каждого — префаб, базовая цена,
// вес выпадения (раздувается удачей замка), шанс окна апгрейда замка и таблицы дропа
// предметов/реликвий. Пришло на смену ItemData.chestRewardData (ItemSettings) и
// RelicsSettings.chestRelicChance. Ассет: Assets/Settings/ChestsSettings.asset,
// доступ — GameConfigs.Instance.ChestsSettings; первичная сборка и дефолты —
// Tools/Chests/Build Chest Assets (ChestsAssetsBuilder).
[CreateAssetMenu(fileName = "ChestsSettings", menuName = "Settings/ChestsSettings")]
public class ChestsSettings : ScriptableObject {
	[Tooltip("Рост цены за каждый открытый сундук: basePrice * exponent^opened (счётчик общий на все тиры)")]
	[SerializeField] float priceExponent = 1.5f;
	[Tooltip("Высота (Y), на которую ставится упавший сундук")]
	[SerializeField] float dropHeight = 0.3f;
	[ListDrawerSettings(ListElementLabelName = "tier")]
	[SerializeField] List<ChestTierConfig> tiers = new();

	public float PriceExponent => priceExponent;
	public float DropHeight => dropHeight;
	public IReadOnlyList<ChestTierConfig> Tiers => tiers;

	public ChestTierConfig GetTier(ChestTier tier) {
		foreach (var cfg in tiers) {
			if (cfg != null && cfg.tier == tier)
				return cfg;
		}
		return null;
	}

	// Вес тира = dropWeight * (1 + luck01 * luckCoefficient): чем больше удача замка,
	// тем чаще выпадают тиры с большим коэффициентом. Тиры без префаба не участвуют.
	public ChestTierConfig RollTier() {
		float luck01 = LuckUtils.GetLuck01();
		var picked = WeightedRandom.Pick(tiers, cfg => cfg != null && cfg.prefab != null ? cfg.GetDropWeight(luck01) : 0f);
		if (picked != null)
			return picked;
		// Все веса нулевые — берём первый тир с префабом, чтобы сундук всё же выпал.
		foreach (var cfg in tiers) {
			if (cfg != null && cfg.prefab != null)
				return cfg;
		}
		return null;
	}

	// Ровно прежняя формула ChestsManager (усечение pow до int) — баланс цен не меняется.
	public int GetBasePrice(ChestTierConfig cfg, int openedChests) {
		return cfg.basePrice * (int)Mathf.Pow(priceExponent, openedChests);
	}

#if UNITY_EDITOR
	// Программное наполнение (ChestsAssetsBuilder); в рантайме не используется.
	public void EditorSetTiers(List<ChestTierConfig> tiers) {
		this.tiers = tiers ?? new List<ChestTierConfig>();
	}

	private void OnValidate() {
		var seen = new HashSet<ChestTier>();
		var catalog = GameConfigs.Instance != null ? GameConfigs.Instance.RelicsSettings : null;
		foreach (var cfg in tiers) {
			if (cfg == null) {
				Debug.LogWarning($"[ChestsSettings] '{name}': пустая запись тира.", this);
				continue;
			}
			if (!seen.Add(cfg.tier))
				Debug.LogWarning($"[ChestsSettings] '{name}': тир {cfg.tier} задан дважды — используется первый.", this);
			if (cfg.prefab == null)
				Debug.LogWarning($"[ChestsSettings] '{name}': у тира {cfg.tier} не задан префаб — он не будет выпадать.", this);
			if (cfg.items.Count == 0 && cfg.relics.Count == 0)
				Debug.LogWarning($"[ChestsSettings] '{name}': у тира {cfg.tier} пустые таблицы дропа.", this);
			// Свитки гейтятся метой; без хотя бы одного не-свитка с весом пул может опустеть — игрок заплатит за пустой сундук
			if (!cfg.items.Exists(i => i != null && i.weight > 0f && !SaveDataProfile.IsSpell(i.itemType)))
				Debug.LogWarning($"[ChestsSettings] '{name}': у тира {cfg.tier} нет не-свитка (например, Crystal) с весом > 0 — при залоченных свитках награда будет пустой.", this);
			foreach (var item in cfg.items) {
				if (item != null && item.minCount > item.maxCount)
					Debug.LogWarning($"[ChestsSettings] '{name}': {cfg.tier}/{item.itemType}: minCount > maxCount.", this);
			}
			if (catalog == null)
				continue;
			foreach (var relic in cfg.relics) {
				if (relic != null && catalog.GetRelic(relic.relicId) == null)
					Debug.LogWarning($"[ChestsSettings] '{name}': {cfg.tier}: реликвия '{relic.relicId}' не найдена в каталоге.", this);
			}
		}
	}
#endif
}

[Serializable]
public class ChestTierConfig {
	public ChestTier tier;
	public RewardChest prefab;
	[Tooltip("Базовая цена открытия в монетах катки (дальше растёт по ChestsSettings.priceExponent)")]
	[Min(0)] public int basePrice = 2;

	[Header("Выпадение тира")]
	[Tooltip("Вес тира при дропе сундука за зачищенную волну")]
	[Min(0)] public float dropWeight = 1f;
	[Tooltip("Насколько удача замка раздувает вес: вес * (1 + luck01 * k). 0 — удача не влияет")]
	[Min(0)] public float luckCoefficient = 0f;

	[Header("Награда")]
	[Tooltip("Шанс, что вместо рулетки предметов откроется окно апгрейда замка")]
	[Range(0f, 1f)] public float castleUpgradeChance = 0.5f;
	[Tooltip("Шанс, что рулетка выдаст реликвию вместо предмета (если есть подходящие реликвии)")]
	[Range(0f, 1f)] public float relicChance = 0.35f;
	[ListDrawerSettings(ListElementLabelName = "itemType")]
	public List<ChestItemDrop> items = new();
	[ListDrawerSettings(ListElementLabelName = "relicId")]
	public List<ChestRelicDrop> relics = new();

	public float GetDropWeight(float luck01) {
		return dropWeight * (1f + luck01 * luckCoefficient);
	}

	// Сначала реликвия (гейт relicChance + взвешенный выбор среди подходящих), иначе предмет.
	// null — таблицы пусты, награды нет.
	public ChestReward RollReward() {
		var relic = RollRelic();
		if (relic != null)
			return new ChestReward { Relic = relic };
		var item = RollItem();
		if (item == null)
			return null;
		return new ChestReward { Item = item.itemType, Count = item.RollCount() };
	}

	// Свитки, ещё не открытые метой (ContentUnlocksManager), в ролл не попадают — вес 0.
	public ChestItemDrop RollItem() {
		return WeightedRandom.Pick(items, drop => drop != null && ContentUnlocksManager.IsSpellUnlocked(drop.itemType) ? drop.weight : 0f);
	}

	// null => реликвия не выпала (не прошёл шанс или нет подходящих/неизвестные id).
	// Вес записи домножается на множитель удачи её рарности (RarityConfig.luckCoefficient),
	// уже полученные нестакаемые/упёршиеся в лимит отсеиваются через RelicsManager.CanDrop.
	public RelicDefinition RollRelic() {
		if (relics.Count == 0 || UnityEngine.Random.value >= relicChance)
			return null;
		var catalog = GameConfigs.Instance != null ? GameConfigs.Instance.RelicsSettings : null;
		if (catalog == null)
			return null;

		float luck01 = LuckUtils.GetLuck01();
		var candidates = new List<RelicCandidate>();
		foreach (var entry in relics) {
			if (entry == null || entry.weight <= 0f || string.IsNullOrEmpty(entry.relicId))
				continue;
			var definition = catalog.GetRelic(entry.relicId);
			if (definition == null || !RelicsManager.CanDrop(definition))
				continue;
			candidates.Add(new RelicCandidate {
				definition = definition,
				weight = entry.weight * LuckUtils.GetRarityLuckMultiplier(definition.Rarity, luck01)
			});
		}
		var picked = WeightedRandom.Pick(candidates, c => c.weight);
		return picked != null ? picked.definition : null;
	}

	private class RelicCandidate {
		public RelicDefinition definition;
		public float weight;
	}

#if UNITY_EDITOR
	[Sirenix.OdinInspector.Button("Добавить все реликвии рарности"), PropertyOrder(100)]
	private void AddRelicsOfRarity(RarityType rarity, float weight = 1f) {
		var catalog = GameConfigs.Instance != null ? GameConfigs.Instance.RelicsSettings : null;
		if (catalog == null) {
			Debug.LogWarning("[ChestsSettings] RelicsSettings не подключён в GameConfigs.");
			return;
		}
		foreach (var relic in catalog.Relics) {
			if (relic == null || relic.Rarity != rarity || string.IsNullOrEmpty(relic.Id))
				continue;
			if (relics.Exists(r => r != null && r.relicId == relic.Id))
				continue;
			relics.Add(new ChestRelicDrop { relicId = relic.Id, weight = weight });
		}
	}
#endif
}

[Serializable]
public class ChestItemDrop {
	public ItemType itemType = ItemType.Crystal;
	[Min(1)] public int minCount = 1;
	[Min(1)] public int maxCount = 1;
	[Tooltip("Вес записи при взвешенном выборе предмета")]
	[Min(0)] public float weight = 1f;

	public int RollCount() {
		int min = Mathf.Min(minCount, maxCount);
		int max = Mathf.Max(minCount, maxCount);
		return UnityEngine.Random.Range(min, max + 1);
	}
}

[Serializable]
public class ChestRelicDrop {
	[ValueDropdown(nameof(RelicIdOptions))]
	public string relicId;
	[Tooltip("Вес записи при взвешенном выборе реликвии (домножается на множитель удачи рарности)")]
	[Min(0)] public float weight = 1f;

	// Источник дропдауна для инспектора (Odin). Намеренно не под #if UNITY_EDITOR —
	// nameof должен резолвиться и в билде.
	private static IEnumerable<ValueDropdownItem<string>> RelicIdOptions() {
		var catalog = GameConfigs.Instance != null ? GameConfigs.Instance.RelicsSettings : null;
		if (catalog == null)
			yield break;
		foreach (var relic in catalog.Relics) {
			if (relic == null || string.IsNullOrEmpty(relic.Id))
				continue;
			yield return new ValueDropdownItem<string>(relic.Rarity + "/" + relic.Id, relic.Id);
		}
	}
}

// Результат ролла сундука: либо реликвия (состояние катки), либо предмет с количеством (профиль).
public sealed class ChestReward {
	public RelicDefinition Relic;
	public ItemType Item;
	public int Count;
	public bool IsRelic => Relic != null;
}
