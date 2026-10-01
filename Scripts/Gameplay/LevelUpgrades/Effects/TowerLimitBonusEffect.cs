using UnityEngine;

namespace Gameplay.LevelUpgrades {
	// Увеличивает число башен, которые можно построить одновременно.
	[System.Serializable]
	public class TowerLimitBonusEffect : BaseUpgradeEffect {
		[SerializeField, Min(1)] int bonusTowers = 1;

		public int BonusTowers => Mathf.Max(0, bonusTowers);

#if UNITY_EDITOR
		public void EditorInit(int bonusTowers) {
			this.bonusTowers = Mathf.Max(1, bonusTowers);
		}
#endif
	}
}
