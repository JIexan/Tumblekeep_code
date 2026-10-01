using UnityEngine;

namespace Gameplay.Relics {
	// «Петарда алхимика»: с шансом chance% (стакается) убитый враг взрывается, нанося соседям
	// percentOfMaxHp% от своего максимального ХП. Цепные взрывы ограничены глубиной
	// в RelicsManager.NotifyRewardedKill.
	[System.Serializable]
	public class ExplodeOnKillRelicEffector : BaseRelicEffector, ITriggeredRelicEffector {
		[SerializeField] [Range(0f, 100f)] float chance = 10f;
		[SerializeField] float percentOfMaxHp = 30f;
		[SerializeField] float radius = 8f;
		[SerializeField] DamageType damageType = DamageType.Fire;

		public RelicTrigger Trigger => RelicTrigger.UnitKilled;

		public void Execute(in RelicContext ctx, int stacks) {
			if (ctx.Unit == null)
				return;
			if (Random.value * 100f >= chance * stacks)
				return;
			var controller = GameController.Instance;
			if (controller == null)
				return;

			float damage = ctx.Unit.CombinedHP * percentOfMaxHp / 100f;
			if (damage <= 0f)
				return;

			Vector3 center = ctx.Unit.transform.position;
			float sqrRadius = radius * radius;
			// Снапшот: Damage может убить юнита и модифицировать живой список врагов.
			var targets = controller.GetAllEnemies().ToArray();
			for (int i = 0; i < targets.Length; i++) {
				var enemy = targets[i];
				if (enemy == null || enemy == ctx.Unit || enemy.isDead)
					continue;
				if ((enemy.transform.position - center).sqrMagnitude > sqrRadius)
					continue;
				enemy.Damage(null, damage, damageType);
			}
		}

#if UNITY_EDITOR
		public void EditorInit(float chance, float percentOfMaxHp, float radius, DamageType damageType = DamageType.Fire) {
			this.chance = chance;
			this.percentOfMaxHp = percentOfMaxHp;
			this.radius = radius;
			this.damageType = damageType;
		}
#endif
	}
}
