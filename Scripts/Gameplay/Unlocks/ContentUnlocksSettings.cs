using System.Collections.Generic;
using Gameplay.RestorationSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Unlocks {
	public enum UnlockKind {
		Relic,   // key = RelicDefinition.Id
		Spell,   // key = имя ItemType (SpellFreeze...)
		Faction, // key = имя FactionId
		Tier,    // key = имя UnitTier (T3...)
		Region,  // key = имя RegionId — только для попапа-анонса (PlayerProgressController), в профиле/реестре не хранится
		Item,    // key = "ItemType:count" — только для попапа «получены предметы» (подарок туториала), в профиле/реестре не хранится
		Level,   // key = Level.Id — остров на карте хаба (UnlockLevelAction)
	}

	// Запись реестра «что и где открывается»: генерируется Tools/Unlocks/Rebuild Unlock Registry
	// сканом Unlock*Action в Assets/Prefabs/Hub/*_progress.prefab. Нужна для подсказок книги
	// («восстановите X в зоне Y») — книга открывается и в бою, где объектов хаба нет.
	[System.Serializable]
	public class UnlockRegistryEntry {
		public UnlockKind kind;
		public string key;
		public RegionId regionId;
		public string stepId;   // суффикс ключа локализации ProgressStepTitle<stepId>
		public string stepGuid; // save-identity шага (SaveDataPlayerLocation.IsStepCompleted)

		public bool Matches(UnlockKind kind, string key) => this.kind == kind && this.key == key;
	}

	// Стартовый набор открытого контента + каталог фракций для реверс-лукапа юнитов + реестр подсказок.
	// Ассет: Assets/Settings/ContentUnlocksSettings.asset, доступ — GameConfigs.Instance.ContentUnlocksSettings,
	// запросы — статический ContentUnlocksManager.
	[CreateAssetMenu(fileName = "ContentUnlocksSettings", menuName = "Settings/ContentUnlocksSettings")]
	public class ContentUnlocksSettings : ScriptableObject {
		[Header("Открыто со старта")]
		[ValueDropdown(nameof(RelicIdOptions))]
		[SerializeField] List<string> defaultRelicIds = new();
		[SerializeField] List<ItemType> defaultSpells = new();
		[SerializeField] List<FactionId> defaultFactions = new();
		[Tooltip("Максимальный ярус юнитов, доступный без анлоков хаба.")]
		[SerializeField] UnitTier defaultMaxTier = UnitTier.T2;
		[Tooltip("Острова (Level.Id из LevelSettings), доступные на карте хаба без анлоков. Остальные открываются UnlockLevelAction шагов хаба.")]
		[SerializeField] List<string> defaultLevelIds = new() { "Home", "Level1" };

		[Header("Каталог фракций")]
		[Tooltip("Все ассеты фракций — чтобы книга и мета знали, к какой фракции/ярусу относится юнит вне сцены уровня.")]
		[SerializeField] List<FactionSettings> factionCatalog = new();

		[Header("Реестр подсказок (генерируется)")]
		[Tooltip("Tools/Unlocks/Rebuild Unlock Registry. Руками не править — перетрётся.")]
		[ReadOnly]
		[SerializeField] List<UnlockRegistryEntry> registry = new();

		public IReadOnlyList<string> DefaultRelicIds => defaultRelicIds;
		public IReadOnlyList<ItemType> DefaultSpells => defaultSpells;
		public IReadOnlyList<FactionId> DefaultFactions => defaultFactions;
		public UnitTier DefaultMaxTier => defaultMaxTier;
		public IReadOnlyList<string> DefaultLevelIds => defaultLevelIds;
		public IReadOnlyList<FactionSettings> FactionCatalog => factionCatalog;
		public IReadOnlyList<UnlockRegistryEntry> Registry => registry;

		public UnlockRegistryEntry FindEntry(UnlockKind kind, string key) {
			foreach (var entry in registry) {
				if (entry != null && entry.Matches(kind, key))
					return entry;
			}
			return null;
		}

		// Источник дропдауна для инспектора (Odin). Не под #if UNITY_EDITOR — nameof должен резолвиться и в билде.
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

#if UNITY_EDITOR
		public void EditorSetRegistry(List<UnlockRegistryEntry> entries) {
			registry = entries ?? new List<UnlockRegistryEntry>();
		}

		private void OnValidate() {
			foreach (var spell in defaultSpells) {
				if (!SaveDataProfile.IsSpell(spell))
					Debug.LogWarning($"[ContentUnlocksSettings] '{name}': {spell} — не свиток (ItemType < 50).", this);
			}
			var seen = new HashSet<FactionId>();
			foreach (var faction in factionCatalog) {
				if (faction == null) {
					Debug.LogWarning($"[ContentUnlocksSettings] '{name}': пустая запись в каталоге фракций.", this);
					continue;
				}
				if (!seen.Add(faction.factionId))
					Debug.LogWarning($"[ContentUnlocksSettings] '{name}': factionId {faction.factionId} встречается у нескольких ассетов фракций.", this);
			}
		}
#endif
	}
}
