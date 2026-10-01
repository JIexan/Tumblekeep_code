using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gameplay.Unlocks {
	// Что открыл игрок при событии OnContentUnlocked
	public readonly struct ContentUnlock {
		public readonly UnlockKind Kind;
		public readonly string Key;

		public ContentUnlock(UnlockKind kind, string key) {
			Kind = kind;
			Key = key;
		}

		// Анонс «получены предметы» (не анлок): окно разбирает key обратно в тип и количество
		public static ContentUnlock ForItem(ItemType type, int count) {
			return new ContentUnlock(UnlockKind.Item, type + ":" + count);
		}
	}

	// Мета-анлоки контента (реликвии, свитки, фракции, ярусы юнитов, острова): дефолты из ContentUnlocksSettings ∪
	// SaveDataUnlocks профиля. Статический сервис (потребители — RelicsManager, ChestsSettings, WavesGenerator,
	// книга), Init из GameConfigs.Load. Профиль per-scene и подменяется автоплеем — ссылку не кэшируем,
	// без SavesManager (edit-mode, ранний вызов из HUD) действуют только дефолты.
	public static class ContentUnlocksManager {
		private static ContentUnlocksSettings configured;

		// После domain reload статик пуст, пока не отработал GameConfigs.Load (в play mode — Bootstrap до сцены,
		// в edit-mode — первое обращение к Instance). Без ленивой подтяжки редакторские запросы считали бы всё открытым
		private static ContentUnlocksSettings settings {
			get {
				if (configured == null && GameConfigs.Instance != null)
					configured = GameConfigs.Instance.ContentUnlocksSettings;
				return configured;
			}
		}

		// Взводится при реальной записи в профиль. PlayerProgressController досохраняет профиль после
		// восстановления шагов на входе в хаб (Apply() идёт ПОСЛЕ его собственного SaveProfile).
		public static bool Dirty { get; private set; }

		public static ContentUnlocksSettings Settings => settings;

		public static void Init(ContentUnlocksSettings unlocksSettings) {
			configured = unlocksSettings;
			if (unlocksSettings == null)
				Debug.LogWarning("[ContentUnlocks] ContentUnlocksSettings не подключён в GameConfigs — весь контент считается открытым.");
		}

		public static bool ConsumeDirty() {
			bool was = Dirty;
			Dirty = false;
			return was;
		}

		private static SaveDataUnlocks Save {
			get {
				// В бою SavesManager создаётся в GameController.Awake — HUD может спросить раньше
				if (SavesManager.Instance == null)
					return null;
				var profile = SavesManager.Profile;
				if (profile == null)
					return null;
				profile.unlocks ??= new SaveDataUnlocks();
				return profile.unlocks;
			}
		}

		private static bool UnlockAll => Save != null && Save.unlockAll;

		// ---------- Запросы ----------

		public static bool IsRelicUnlocked(string relicId) {
			if (string.IsNullOrEmpty(relicId))
				return false;
			if (settings == null || UnlockAll)
				return true;
			if (settings.DefaultRelicIds.Contains(relicId))
				return true;
			var save = Save;
			return save != null && save.HasRelic(relicId);
		}

		// Не-свитки (валюты) всегда открыты
		public static bool IsSpellUnlocked(ItemType type) {
			if (!SaveDataProfile.IsSpell(type))
				return true;
			if (settings == null || UnlockAll)
				return true;
			if (settings.DefaultSpells.Contains(type))
				return true;
			var save = Save;
			return save != null && save.HasSpell(type);
		}

		public static bool IsFactionUnlocked(FactionId id) {
			if (settings == null || UnlockAll)
				return true;
			if (settings.DefaultFactions.Contains(id))
				return true;
			var save = Save;
			return save != null && save.HasFaction(id);
		}

		public static UnitTier MaxTier {
			get {
				if (settings == null || UnlockAll)
					return UnitTier.T4;
				int max = (int)settings.DefaultMaxTier;
				var save = Save;
				if (save != null && save.maxTier > max)
					max = save.maxTier;
				return (UnitTier)max;
			}
		}

		public static bool IsTierUnlocked(UnitTier tier) => (int)tier <= (int)MaxTier;

		// Остров на карте хаба (Level.Id). Открывается UnlockLevelAction шага восстановления
		public static bool IsLevelUnlocked(string levelId) {
			if (string.IsNullOrEmpty(levelId))
				return false;
			if (settings == null || UnlockAll)
				return true;
			if (settings.DefaultLevelIds.Contains(levelId))
				return true;
			var save = Save;
			return save != null && save.HasLevel(levelId);
		}

		// Юнит открыт, если хотя бы одно его вхождение в открытую фракцию имеет открытый ярус.
		// Юниты вне каталога фракций (мусор порталов, экипажи) — открыты.
		public static bool IsUnitUnlocked(UnitType type) {
			if (settings == null || UnlockAll)
				return true;
			bool listed = false;
			foreach (var faction in settings.FactionCatalog) {
				if (faction == null)
					continue;
				foreach (var cfg in faction.units) {
					if (cfg == null || cfg.unitType != type)
						continue;
					listed = true;
					if (IsFactionUnlocked(faction.factionId) && IsTierUnlocked(cfg.tier))
						return true;
				}
			}
			return !listed;
		}

		// Причины лока юнита для подсказок книги: фракция (если ни одна его фракция не открыта) или ярус.
		// Из нескольких фракций предпочитаем ту, у которой есть запись в реестре (подсказка будет конкретной).
		public static List<ContentUnlock> GetUnitLockReasons(UnitType type) {
			var reasons = new List<ContentUnlock>();
			if (settings == null || IsUnitUnlocked(type))
				return reasons;

			FactionId? lockedFaction = null;
			bool lockedFactionHasHint = false;
			UnitTier? minLockedTier = null;
			foreach (var faction in settings.FactionCatalog) {
				if (faction == null)
					continue;
				foreach (var cfg in faction.units) {
					if (cfg == null || cfg.unitType != type)
						continue;
					if (IsFactionUnlocked(faction.factionId)) {
						// фракция открыта — значит закрыт ярус
						if (minLockedTier == null || (int)cfg.tier < (int)minLockedTier.Value)
							minLockedTier = cfg.tier;
					} else {
						bool hasHint = settings.FindEntry(UnlockKind.Faction, faction.factionId.ToString()) != null;
						if (lockedFaction == null || (hasHint && !lockedFactionHasHint)) {
							lockedFaction = faction.factionId;
							lockedFactionHasHint = hasHint;
						}
					}
				}
			}

			if (minLockedTier != null)
				reasons.Add(new ContentUnlock(UnlockKind.Tier, minLockedTier.Value.ToString()));
			else if (lockedFaction != null)
				reasons.Add(new ContentUnlock(UnlockKind.Faction, lockedFaction.Value.ToString()));
			return reasons;
		}

		public static bool IsUnlocked(UnlockKind kind, string key) {
			switch (kind) {
				case UnlockKind.Relic: return IsRelicUnlocked(key);
				case UnlockKind.Spell: return System.Enum.TryParse(key, out ItemType spell) && IsSpellUnlocked(spell);
				case UnlockKind.Faction: return System.Enum.TryParse(key, out FactionId faction) && IsFactionUnlocked(faction);
				case UnlockKind.Tier: return System.Enum.TryParse(key, out UnitTier tier) && IsTierUnlocked(tier);
				case UnlockKind.Level: return IsLevelUnlocked(key);
				default: return true;
			}
		}

		// Реестр «где открывается» (для книги). null — путь открытия ещё не заавторен
		public static UnlockRegistryEntry GetHint(UnlockKind kind, string key) {
			return settings != null ? settings.FindEntry(kind, key) : null;
		}

		// ---------- Записи (Unlock*Action) ----------

		public static bool UnlockRelic(string relicId) {
			var save = Save;
			if (save == null || string.IsNullOrEmpty(relicId))
				return false;
			if (settings != null && settings.DefaultRelicIds.Contains(relicId))
				return false;
			return Commit(save.AddRelic(relicId), UnlockKind.Relic, relicId);
		}

		public static bool UnlockSpell(ItemType type) {
			var save = Save;
			if (save == null || !SaveDataProfile.IsSpell(type))
				return false;
			if (settings != null && settings.DefaultSpells.Contains(type))
				return false;
			return Commit(save.AddSpell(type), UnlockKind.Spell, type.ToString());
		}

		public static bool UnlockFaction(FactionId id) {
			var save = Save;
			if (save == null)
				return false;
			if (settings != null && settings.DefaultFactions.Contains(id))
				return false;
			return Commit(save.AddFaction(id), UnlockKind.Faction, id.ToString());
		}

		public static bool UnlockTier(UnitTier tier) {
			var save = Save;
			if (save == null)
				return false;
			if (settings != null && (int)tier <= (int)settings.DefaultMaxTier)
				return false;
			return Commit(save.RaiseTier(tier), UnlockKind.Tier, tier.ToString());
		}

		public static bool UnlockLevel(string levelId) {
			var save = Save;
			if (save == null || string.IsNullOrEmpty(levelId))
				return false;
			if (settings != null && settings.DefaultLevelIds.Contains(levelId))
				return false;
			return Commit(save.AddLevel(levelId), UnlockKind.Level, levelId);
		}

		private static bool Commit(bool changed, UnlockKind kind, string key) {
			if (!changed)
				return false;
			Dirty = true;
			var unlock = new ContentUnlock(kind, key);
			ContentUnlockAnnouncer.Enqueue(unlock);
			InGameEventManager.SendEvent(InGameEvents.OnContentUnlocked, unlock);
			return true;
		}

		// ---------- Читы ----------

		public static void SetUnlockAll(bool value) {
			var save = Save;
			if (save == null)
				return;
			save.unlockAll = value;
			Dirty = true;
		}

		public static void ResetAll() {
			var save = Save;
			if (save == null)
				return;
			save.Clear();
			Dirty = true;
		}

		public static string DescribeState() {
			var save = Save;
			string s = "[ContentUnlocks] maxTier=" + MaxTier + (UnlockAll ? " (UNLOCK ALL)" : "");
			s += "\n  factions: " + Join(System.Enum.GetValues(typeof(FactionId)), v => IsFactionUnlocked((FactionId)v) ? v.ToString() : null);
			s += "\n  spells: " + Join(System.Enum.GetValues(typeof(ItemType)), v => SaveDataProfile.IsSpell((ItemType)v) && IsSpellUnlocked((ItemType)v) ? v.ToString() : null);
			if (settings != null && GameConfigs.Instance != null && GameConfigs.Instance.RelicsSettings != null)
				s += "\n  relics: " + Join(GameConfigs.Instance.RelicsSettings.Relics, r => r is Gameplay.Relics.RelicDefinition relic && IsRelicUnlocked(relic.Id) ? relic.Id : null);
			if (save != null)
				s += $"\n  saved extra: relics {save.relicIds.Count}, spells {save.spellIds.Count}, factions {save.factionIds.Count}, tier {save.maxTier}, levels [{string.Join(", ", save.levelIds)}]";
			return s;
		}

		private static string Join(System.Collections.IEnumerable items, System.Func<object, string> map) {
			var parts = new List<string>();
			foreach (var item in items) {
				string v = map(item);
				if (v != null)
					parts.Add(v);
			}
			return string.Join(", ", parts);
		}
	}
}
