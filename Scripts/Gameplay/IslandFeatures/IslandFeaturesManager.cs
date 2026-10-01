using Gameplay.Relics;

namespace Gameplay.IslandFeatures {
	// Query-сервис особенностей островов (паттерн LevelUpgradesManager). Сам эффекты не считает:
	// на старте катки отдаёт эффекторы особенности в RelicsManager, дальше работают хуки реликвий.
	public static class IslandFeaturesManager {
		private static IslandFeaturesSettings settings;

		// Особенность текущей катки; null — у уровня её нет или сцена запущена напрямую из редактора.
		public static IslandFeatureDefinition Current { get; private set; }

		public static void Init(IslandFeaturesSettings islandFeaturesSettings) {
			settings = islandFeaturesSettings;
		}

		public static IslandFeatureDefinition GetFeature(string levelId) {
			return settings != null ? settings.GetFeature(levelId) : null;
		}

		// GameController.Awake, сразу после RelicsManager.ResetRun. Зовётся всегда, в т.ч. с null —
		// иначе на уровне без особенности остались бы эффекторы прошлой катки.
		public static void ActivateForCurrentLevel() {
			Current = GetFeature(CurrentLevelContext.Level?.Id);
			RelicsManager.SetLevelEffectors(Current?.Effectors);
		}

		// Выгрузка сцены уровня: в меню эффекторы не должны дёргать CoinsManager/замок
		public static void Deactivate() {
			Current = null;
			RelicsManager.SetLevelEffectors(null);
		}
	}
}
