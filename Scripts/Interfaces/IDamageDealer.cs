using UnityEngine;

public interface IDamageDealer
{
	public void DamageUnitWithProjectile(Unit u, Transform projectile);

	public void DamageUnitsWithProjectileInRadius(Vector3 position);
}
