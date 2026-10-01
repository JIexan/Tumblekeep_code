using UnityEngine;

namespace Gameplay.Relics {
	// Бонус от баланса монет: +percentPerStep% и/или +flatPerStep к выбранному RelicValue
	// за каждые coinsPerStep монет на счету. Обобщение «Дракона-скряги» на любое значение
	// (особенность острова «Звонкая казна» — удача). Читает только CoinsManager (без циклов через замок).
	[System.Serializable]
	public class StatPerCoinsRelicEffector : BaseRelicEffector, IRelicValueContributor {
		[SerializeField] RelicValue target;
		[SerializeField] float percentPerStep;
		[SerializeField] float flatPerStep;
		[SerializeField] int coinsPerStep = 1;

		public RelicValue Target => target;

		public float GetPercent(int stacks) {
			return GetSteps() * percentPerStep * stacks;
		}

		public float GetFlat(int stacks) {
			return GetSteps() * flatPerStep * stacks;
		}

		private int GetSteps() {
			var coins = CoinsManager.Instance;
			if (coins == null || coinsPerStep <= 0)
				return 0;
			return Mathf.Max(0, coins.Amount) / coinsPerStep;
		}

#if UNITY_EDITOR
		public void EditorInit(RelicValue target, float percentPerStep, float flatPerStep, int coinsPerStep) {
			this.target = target;
			this.percentPerStep = percentPerStep;
			this.flatPerStep = flatPerStep;
			this.coinsPerStep = coinsPerStep;
		}
#endif
	}
}
