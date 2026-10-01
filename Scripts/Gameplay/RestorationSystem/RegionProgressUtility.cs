using UnityEngine;

namespace Gameplay.RestorationSystem {
	// Единая точка расчёта «процента готовности» региона по сейву.
	// Используется требованиями апгрейдов и гейтом открытия зон — работает в любой сцене.
	public static class RegionProgressUtility {
		// Особые случаи: completed — всегда 100%; totalSteps ещё не записан
		// (регион ни разу не был InProgress в хабе) — 0%. Guid'ы удалённых из контента
		// шагов могут слегка завышать счёт — процент клампится сверху.
		public static int GetPercent(RegionId regionId) {
			var region = SavesManager.Profile.playerLocation.GetRegionSave(regionId);
			if (region == null)
				return 0;
			if (region.completed)
				return 100;
			if (region.totalSteps <= 0)
				return 0;

			int completed = region.completedStepIds?.Count ?? 0;
			return Mathf.Clamp(completed * 100 / region.totalSteps, 0, 100);
		}
	}
}
