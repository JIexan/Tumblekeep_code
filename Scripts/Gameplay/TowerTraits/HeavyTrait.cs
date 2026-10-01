public class HeavyTrait : AbstractTrait {
	public HeavyTrait(BaseTower tower) : base(tower) {
		type = TowerTraitType.Heavy;
	}

	public override void Install() { 
		tower.Characteristics.Get(CharacteristicType.Damage)?.AddPercent(20);
		tower.Characteristics.Get(CharacteristicType.ProjectileDamage)?.AddPercent(20);
		tower.Characteristics.Get(CharacteristicType.Speed)?.SubPercent(20);
		tower.Characteristics.Get(CharacteristicType.ProjectileSpeed)?.SubPercent(15);
	}

	public override void DamageUnitWithProjectile(Unit u) {	}

	public override void DamageUnitInArea(Unit u) {
		
	}
}
