using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.LevelUpgrades {
	// Query-service апгрейдов уровней: агрегирует эффекты
	// купленных апгрейдов текущего уровня и отвечает на запросы сценного кода.
	// Статический, т.к. потребители включают неинжектируемый код
	// (BaseTower.GetPossibleTraits, читы, UIController).
	public static class LevelUpgradesManager {
		private static LevelUpgradesSettings settings;

		// Текущий уровень; null (прямой запуск сцены в редакторе) => fallback «без конфига».
		public static string CurrentLevelId => CurrentLevelContext.Level?.Id;

		public static void Init(LevelUpgradesSettings levelUpgradesSettings) {
			settings = levelUpgradesSettings;
		}

		public static LevelUpgradesConfig GetConfig(string levelId) {
			return settings != null ? settings.GetConfig(levelId) : null;
		}

		public static bool IsPurchased(string levelId, string upgradeId) {
			if (SavesManager.Profile.levelUpgrades.IsPurchased(levelId, upgradeId))
				return true;

			// startPurchased-записи (стартовые башни) считаются купленными без сейва.
			var upgrade = GetConfig(levelId)?.GetUpgrade(upgradeId);
			return upgrade != null && upgrade.StartPurchased;
		}

		public static bool CanPurchase(string levelId, string upgradeId) {
			var upgrade = GetConfig(levelId)?.GetUpgrade(upgradeId);
			if (upgrade == null || IsPurchased(levelId, upgradeId))
				return false;

			foreach (var requirement in upgrade.Requirements) {
				if (requirement != null && !requirement.IsMet(levelId))
					return false;
			}
			return true;
		}

		// Есть ли апгрейд, который можно купить прямо сейчас (все требования выполнены, включая цену).
		public static bool HasAvailableUpgrades(string levelId) {
			var config = GetConfig(levelId);
			if (config == null)
				return false;

			foreach (var upgrade in config.Upgrades) {
				if (upgrade != null && CanPurchase(levelId, upgrade.Id))
					return true;
			}
			return false;
		}

		// Апгрейд, для которого прогресс зоны хаба — одно из требований
		public struct RegionGatedUpgrade {
			public string LevelId;
			public LevelUpgradeEntry Entry;
			public int MinPercent;
		}

		// Все апгрейды всех уровней, завязанные на процент готовности зоны (для засечек на прогресс-баре зоны).
		// Апгрейд может требовать и другие зоны — здесь смотрим только на порог по этой.
		public static List<RegionGatedUpgrade> GetRegionGatedUpgrades(Gameplay.RestorationSystem.RegionId regionId) {
			var result = new List<RegionGatedUpgrade>();
			if (settings == null)
				return result;
			foreach (var config in settings.Configs) {
				if (config == null)
					continue;
				foreach (var upgrade in config.Upgrades) {
					if (upgrade == null)
						continue;
					foreach (var requirement in upgrade.Requirements) {
						if (requirement is RegionProgressUpgradeRequirement region && region.RegionId == regionId)
							result.Add(new RegionGatedUpgrade { LevelId = config.LevelId, Entry = upgrade, MinPercent = region.MinPercent });
					}
				}
			}
			return result;
		}

		// Транзакционно: сначала полная валидация (CanPurchase), потом списание.
		public static bool TryPurchase(string levelId, string upgradeId) {
			if (!CanPurchase(levelId, upgradeId))
				return false;

			var upgrade = GetConfig(levelId).GetUpgrade(upgradeId);
			foreach (var requirement in upgrade.Requirements) {
				requirement?.Consume(levelId);
			}

			SavesManager.Profile.levelUpgrades.MarkPurchased(levelId, upgradeId);
			SavesManager.Instance.SaveProfile();
			InGameEventManager.SendEvent(InGameEvents.OnLevelUpgradePurchased, upgrade);
			return true;
		}

		// Эффекты типа T из всех КУПЛЕННЫХ апгрейдов уровня.
		public static IEnumerable<T> GetActiveEffects<T>(string levelId) where T : BaseUpgradeEffect {
			var config = GetConfig(levelId);
			if (config == null)
				yield break;

			foreach (var upgrade in config.Upgrades) {
				if (upgrade == null)
					continue;
				// Флаг проверяем на месте — без повторного поиска записи через IsPurchased.
				if (!upgrade.StartPurchased && !SavesManager.Profile.levelUpgrades.IsPurchased(levelId, upgrade.Id))
					continue;
				foreach (var effect in upgrade.Effects) {
					if (effect is T typed)
						yield return typed;
				}
			}
		}

		// Шкала прокачки острова 0..1: сумма progressWeight купленных апгрейдов / сумма всех.
		// Уровень без конфига или с неразмеченными весами => 0 (таски уровня остаются на стартовых значениях).
		public static float GetIslandProgress01(string levelId) {
			var config = GetConfig(levelId);
			if (config == null)
				return 0f;

			float total = 0f;
			float purchased = 0f;
			foreach (var upgrade in config.Upgrades) {
				if (upgrade == null || upgrade.ProgressWeight <= 0f)
					continue;
				total += upgrade.ProgressWeight;
				if (upgrade.StartPurchased || SavesManager.Profile.levelUpgrades.IsPurchased(levelId, upgrade.Id))
					purchased += upgrade.ProgressWeight;
			}
			return total > 0f ? Mathf.Clamp01(purchased / total) : 0f;
		}

		// Виды доп. тасков, открытые КУПЛЕННЫМИ апгрейдами уровня.
		public static HashSet<Gameplay.TasksSystem.TaskType> GetUnlockedTaskTypes(string levelId) {
			var res = new HashSet<Gameplay.TasksSystem.TaskType>();
			var config = GetConfig(levelId);
			if (config == null)
				return res;

			foreach (var upgrade in config.Upgrades) {
				if (upgrade == null)
					continue;
				if (!upgrade.StartPurchased && !SavesManager.Profile.levelUpgrades.IsPurchased(levelId, upgrade.Id))
					continue;
				foreach (var taskType in upgrade.UnlockedTasks)
					res.Add(taskType);
			}
			return res;
		}

		// Id купленных апгрейдов уровня (включая startPurchased) — для логов автоплея и читов.
		public static List<string> GetPurchasedUpgradeIds(string levelId) {
			var res = new List<string>();
			var config = GetConfig(levelId);
			if (config == null)
				return res;

			foreach (var upgrade in config.Upgrades) {
				if (upgrade != null && IsPurchased(levelId, upgrade.Id))
					res.Add(upgrade.Id);
			}
			return res;
		}

		// ---- Запросы по текущему уровню (fallback-безопасные) ----

		public static int GetTowerLimit() {
			int limit = GetConfig(CurrentLevelId)?.InitialTowerLimit ?? 2;
			foreach (var effect in GetActiveEffects<TowerLimitBonusEffect>(CurrentLevelId)) {
				limit += effect.BonusTowers;
			}
			return Mathf.Max(0, limit);
		}

		public static int GetStartCoinsBonus() {
			int bonus = 0;
			foreach (var effect in GetActiveEffects<StartCoinsBonusEffect>(CurrentLevelId)) {
				bonus += effect.BonusCoins;
			}
			return bonus;
		}

		public static bool IsTowerTypeAvailable(TowerType type) {
			var config = GetConfig(CurrentLevelId);
			if (config == null)
				return true; // уровень без конфига — всё как раньше

			if ((config.DefaultAvailableTowers & type) == type)
				return true;

			foreach (var effect in GetActiveEffects<UnlockTowerTypeEffect>(CurrentLevelId)) {
				if (effect.Unlocks(type))
					return true;
			}
			return false;
		}

		// Полный каталог трейтов башни по ВСЕМ конфигам уровней без учёта покупки (справочник/книга).
		// Для одного трейта возвращается первая найденная запись (иконка/ключи UpgradeName_{id}).
		public static List<(TowerTraitType trait, LevelUpgradeEntry entry)> GetAllCatalogTraits(TowerType type) {
			var res = new List<(TowerTraitType, LevelUpgradeEntry)>();
			if (settings == null)
				return res;

			foreach (var config in settings.Configs) {
				if (config == null)
					continue;
				foreach (var upgrade in config.Upgrades) {
					if (upgrade == null)
						continue;
					foreach (var effect in upgrade.Effects) {
						if (effect is not ExtraTraitsEffect extra || !extra.AppliesTo(type))
							continue;
						foreach (var trait in extra.Traits) {
							if (!res.Exists(p => p.Item1 == trait))
								res.Add((trait, upgrade));
						}
					}
				}
			}
			return res;
		}

		public static List<TowerTraitType> GetExtraTraits(TowerType type) {
			var res = new List<TowerTraitType>();
			foreach (var effect in GetActiveEffects<ExtraTraitsEffect>(CurrentLevelId)) {
				if (!effect.AppliesTo(type))
					continue;
				foreach (var trait in effect.Traits) {
					if (!res.Contains(trait))
						res.Add(trait);
				}
			}
			return res;
		}
	}
}
