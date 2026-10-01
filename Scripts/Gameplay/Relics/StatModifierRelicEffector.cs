using UnityEngine;

namespace Gameplay.Relics {
	// Универсальный модификатор значения: +percent% и/или +flat к выбранному RelicValue.
	// Закрывает большинство реликвий; scaleWithStacks линейно умножает величины на число стаков.
	[System.Serializable]
	public class StatModifierRelicEffector : BaseRelicEffector, IRelicValueContributor {
		[SerializeField] RelicValue target;
		[SerializeField] float percent;
		[SerializeField] float flat;
		[SerializeField] bool scaleWithStacks = true;

		public RelicValue Target => target;

		public float GetPercent(int stacks) {
			return scaleWithStacks ? percent * stacks : percent;
		}

		public float GetFlat(int stacks) {
			return scaleWithStacks ? flat * stacks : flat;
		}

#if UNITY_EDITOR
		public void EditorInit(RelicValue target, float percent, float flat, bool scaleWithStacks = true) {
			this.target = target;
			this.percent = percent;
			this.flat = flat;
			this.scaleWithStacks = scaleWithStacks;
		}
#endif
	}
}
