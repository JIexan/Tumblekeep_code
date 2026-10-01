namespace Gameplay.IslandExperience {
	// Опыт/левелы островов (игровых уровней), ключ — Level.Id. Статический query-service
	// в стиле LevelUpgradesManager; инициализируется из GameConfigs.
	// XP за катку копится в MatchRewards и начисляется через GameController.BankRewards при победе.
	public static class IslandExperienceManager {
		private static IslandExperienceSettings settings;

		public static string CurrentLevelId => CurrentLevelContext.Level?.Id;

		public static void Init(IslandExperienceSettings islandExperienceSettings) {
			settings = islandExperienceSettings;
		}

		public static int GetTotalXP(string levelId) {
			return SavesManager.Profile.islandExperience.GetTotalXP(levelId);
		}

		public static int GetLevel(string levelId) {
			if (settings == null)
				return 1;
			return settings.GetLevelFromTotalXP(GetTotalXP(levelId));
		}

		public static float GetProgressToNextLevel(string levelId) {
			if (settings == null)
				return 0f;
			return settings.GetProgressToNextLevel(GetTotalXP(levelId));
		}

		public static bool IsMaxLevel(string levelId) {
			return settings != null && GetLevel(levelId) >= settings.MaxLevel;
		}

		// --- Хелперы по произвольному totalXP (для UI-анимаций начисления, см. VictoryWindow) ---

		public static int GetLevelForTotalXP(int totalXP) {
			if (settings == null)
				return 1;
			return settings.GetLevelFromTotalXP(totalXP);
		}

		// XP, набранный внутри текущего уровня.
		public static int GetXPIntoLevel(int totalXP) {
			if (settings == null)
				return 0;
			return totalXP - settings.GetTotalXPForLevel(settings.GetLevelFromTotalXP(totalXP));
		}

		// Стоимость следующего левелапа; 0 на максимальном уровне.
		public static int GetXPToNextLevel(int totalXP) {
			if (settings == null)
				return 0;
			int level = settings.GetLevelFromTotalXP(totalXP);
			return level >= settings.MaxLevel ? 0 : settings.GetXPForLevel(level + 1);
		}

		// XP на максимальном левеле не клампится: левел и прогресс и так капаются вычислением.
		public static void AddXP(string levelId, int amount) {
			if (string.IsNullOrEmpty(levelId) || amount <= 0)
				return;

			SavesManager.Profile.islandExperience.AddXP(levelId, amount);
			SavesManager.Instance.SaveProfile();
			InGameEventManager.SendEvent(InGameEvents.OnIslandXPChanged, levelId);
		}
	}
}
