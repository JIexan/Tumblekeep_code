using UnityEngine;

namespace Gameplay.Relics {
	// «Дракон-скряга»: +percentPerStep% к урону башен за каждые coinsPerStep монет на балансе.
	// Динамический вклад в TowerDamage; читает только CoinsManager (без циклов через замок).
	[System.Serializable]
	public class DamagePerCoinsRelicEffector : BaseRelicEffector, IRelicValueContributor {
		[SerializeField] float percentPerStep = 1f;
		[SerializeField] int coinsPerStep = 50;

		public RelicValue Target => RelicValue.TowerDamage;

		public float GetPercent(int stacks) {
			var coins = CoinsManager.Instance;
			if (coins == null || coinsPerStep <= 0)
				return 0f;
			return (coins.Amount / coinsPerStep) * percentPerStep * stacks;
		}

		public float GetFlat(int stacks) {
			return 0f;
		}

#if UNITY_EDITOR
		public void EditorInit(float percentPerStep, int coinsPerStep) {
			this.percentPerStep = percentPerStep;
			this.coinsPerStep = coinsPerStep;
		}
#endif
	}
}
