using UnityEngine;

namespace Gameplay.LevelUpgrades {
	// Аддитивный бонус к стартовым монетам уровня (суммируется по всем купленным апгрейдам).
	[System.Serializable]
	public class StartCoinsBonusEffect : BaseUpgradeEffect {
		[SerializeField] int bonusCoins;

		public int BonusCoins => bonusCoins;

#if UNITY_EDITOR
		public void EditorInit(int bonusCoins) {
			this.bonusCoins = bonusCoins;
		}
#endif
	}
}
