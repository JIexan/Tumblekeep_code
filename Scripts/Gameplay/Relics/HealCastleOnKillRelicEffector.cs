using UnityEngine;

namespace Gameplay.Relics {
	// «Чудо-пирожок»: с шансом chance% (стакается) убитый враг лечит замок на healAmount ХП.
	[System.Serializable]
	public class HealCastleOnKillRelicEffector : BaseRelicEffector, ITriggeredRelicEffector {
		[SerializeField] [Range(0f, 100f)] float chance = 5f;
		[SerializeField] float healAmount = 3f;

		public RelicTrigger Trigger => RelicTrigger.UnitKilled;

		public void Execute(in RelicContext ctx, int stacks) {
			if (Random.value * 100f >= chance * stacks)
				return;
			var castle = Castle.Instance;
			if (castle != null)
				castle.Heal(healAmount);
		}

#if UNITY_EDITOR
		public void EditorInit(float chance, float healAmount) {
			this.chance = chance;
			this.healAmount = healAmount;
		}
#endif
	}
}
