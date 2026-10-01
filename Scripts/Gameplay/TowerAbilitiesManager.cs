using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerAbilitiesManager
{
	public static void ApplyOnHit(TowerUpgradeSettings.AbilitySettings ability, Unit target) {
		switch(ability.type) {
			case AbilityType.PoisonDamage:
				target.AddPeriodicEffect(null, (int)ability.value1, 1f, UnitEffect.Poison, ability.value2, 1);
				break;
			case AbilityType.PoisonSlowness:
				target.AddTemporaryEffect(ability.value2, UnitEffect.Poison, () => target.speed.SubPercent(Mathf.RoundToInt(ability.value1 * 100)), () => target.speed.AddPercent(Mathf.RoundToInt(ability.value1 * 100)), 1);
				break;
			case AbilityType.Ignite:
				if(Random.value > ability.value3) {
					target.AddPeriodicEffect(null, (int)ability.value1, 1f, UnitEffect.Fire, ability.value2, 1);
				}
				break;
			case AbilityType.ExplosiveArrow:
				Unit[] targets = GameController.Instance.GetEnemiesInRadius(target.transform.position, ability.value2, TowerType.Mortar);
				foreach(var unit in targets) { 
					if(unit != target)
						target.Damage(null, ability.value1, DamageType.Fire);
				}
				break;
			case AbilityType.HeadShot:

				break;
		}
	}

}
