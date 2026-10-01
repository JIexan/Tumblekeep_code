using Gameplay.RestorationSystem;
using UnityEngine;

namespace Gameplay.LevelUpgrades {
	// Требование готовности региона в ПРОЦЕНТАХ (0–100). Расчёт процента общий
	// с гейтом открытия зон — RegionProgressUtility (читает сейв, работает в любой сцене).
	[System.Serializable]
	public class RegionProgressUpgradeRequirement : BaseUpgradeRequirement {
		[SerializeField] RegionId regionId;
		[SerializeField, Range(0, 100)] int minPercent = 100;

		public RegionId RegionId => regionId;
		public int MinPercent => minPercent;

		public override bool IsMet(string levelId) {
			return RegionProgressUtility.GetPercent(regionId) >= minPercent;
		}

		public override (int current, int required) GetProgress(string levelId) {
			return (RegionProgressUtility.GetPercent(regionId), minPercent);
		}

#if UNITY_EDITOR
		public void EditorInit(RegionId regionId, int minPercent) {
			this.regionId = regionId;
			this.minPercent = minPercent;
		}
#endif
	}
}
