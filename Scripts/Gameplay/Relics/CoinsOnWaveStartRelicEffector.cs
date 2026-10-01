using UnityEngine;

namespace Gameplay.Relics {
	// «Гусыня с золотыми яйцами»: в начале волны +percentOfBalance% от текущего золота.
	[System.Serializable]
	public class CoinsOnWaveStartRelicEffector : BaseRelicEffector, ITriggeredRelicEffector {
		[SerializeField] float percentOfBalance = 5f;

		public RelicTrigger Trigger => RelicTrigger.WaveStarted;

		public void Execute(in RelicContext ctx, int stacks) {
			var coins = CoinsManager.Instance;
			if (coins == null)
				return;
			int bonus = Mathf.RoundToInt(coins.Amount * (percentOfBalance * stacks) / 100f);
			if (bonus > 0)
				coins.Add(bonus);
		}

#if UNITY_EDITOR
		public void EditorInit(float percentOfBalance) {
			this.percentOfBalance = percentOfBalance;
		}
#endif
	}
}
