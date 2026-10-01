using UnityEngine;

namespace Gameplay.Relics {
	// Условия эффекторов реликвий. Состояние-условия (ХП замка, монеты, волна) можно
	// вычислять в любой момент; юнит-условия (RequiresUnit) осмысленны только с ctx.Unit.
	[System.Serializable]
	public abstract class RelicCondition {
		public abstract bool Evaluate(in RelicContext ctx);

		// Условию нужен юнит из контекста; без него оно вычисляется в false.
		public virtual bool RequiresUnit => false;
	}

	[System.Serializable]
	public class CastleHpCondition : RelicCondition {
		public enum Mode {
			Below,
			Above
		}

		[SerializeField] Mode mode = Mode.Below;
		[SerializeField] [Range(0f, 1f)] float percent = 0.5f;

		public override bool Evaluate(in RelicContext ctx) {
			var castle = Castle.Instance;
			if (castle == null)
				return false;
			float normalized = castle.NormalizedHP;
			// Меню-замок и замок до Start: totalHP == 0 => NaN
			if (float.IsNaN(normalized) || float.IsInfinity(normalized))
				return false;
			return mode == Mode.Below ? normalized < percent : normalized >= percent;
		}

#if UNITY_EDITOR
		public void EditorInit(Mode mode, float percent) {
			this.mode = mode;
			this.percent = percent;
		}
#endif
	}

	[System.Serializable]
	public class WaveNumberCondition : RelicCondition {
		[SerializeField] int minWave = 1;

		public override bool Evaluate(in RelicContext ctx) {
			return RelicsManager.CurrentWave >= minWave;
		}

#if UNITY_EDITOR
		public void EditorInit(int minWave) {
			this.minWave = minWave;
		}
#endif
	}

	[System.Serializable]
	public class CoinsBalanceCondition : RelicCondition {
		public enum Mode {
			Below,
			Above
		}

		[SerializeField] Mode mode = Mode.Above;
		[SerializeField] int amount;

		public override bool Evaluate(in RelicContext ctx) {
			var coins = CoinsManager.Instance;
			if (coins == null)
				return false;
			return mode == Mode.Below ? coins.Amount < amount : coins.Amount >= amount;
		}

#if UNITY_EDITOR
		public void EditorInit(Mode mode, int amount) {
			this.mode = mode;
			this.amount = amount;
		}
#endif
	}

	[System.Serializable]
	public class UnitIsBossCondition : RelicCondition {
		public override bool RequiresUnit => true;

		public override bool Evaluate(in RelicContext ctx) {
			return ctx.Unit != null && ctx.Unit.IsBoss;
		}
	}

	[System.Serializable]
	public class UnitIsFlyingCondition : RelicCondition {
		public override bool RequiresUnit => true;

		public override bool Evaluate(in RelicContext ctx) {
			return ctx.Unit != null && ctx.Unit.typeUnit == TypeMovement.flyin;
		}
	}

	[System.Serializable]
	public class InfiniteModeCondition : RelicCondition {
		public override bool Evaluate(in RelicContext ctx) {
			return GameController.Instance != null && GameController.Instance.IsInfiniteMode;
		}
	}
}
