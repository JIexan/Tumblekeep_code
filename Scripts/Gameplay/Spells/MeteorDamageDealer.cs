using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorDamageDealer : IDamageDealer {

	private float baseRadius;
	private float baseDamage;

	public void Init(float damage, float radius) {
		baseDamage = damage;
		baseRadius = radius;
	}

	public void DamageUnitsWithProjectileInRadius(Vector3 position) {
		Unit[] units = GameController.Instance.GetEnemiesInRadius(position, baseRadius, TowerType.Mortar);
		foreach (var u in units) {
			u.Damage(null, baseDamage, DamageType.Fire);
		}
	}

	public void DamageUnitWithProjectile(Unit u, Transform projectile) {
	
	}
}
