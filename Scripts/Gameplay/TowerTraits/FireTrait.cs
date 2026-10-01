public class FireTrait : AbstractTrait {
	public FireTrait(BaseTower tower) : base(tower) {
		type = TowerTraitType.Fire;
	}

	public override void Install() {
		tower.AddCharacteristic(CharacteristicType.FireDamage);
		tower.AddCharacteristic(CharacteristicType.FireDuration);
	}

	// Огонь — короткий сильный DoT, стаки от разных попаданий складываются до fireMaxStacks
	public override void DamageUnitWithProjectile(Unit u) {
		Ignite(u);
	}

	public override void DamageUnitInArea(Unit u) {
		Ignite(u);
	}

	private void Ignite(Unit u) {
		float damage = tower.GetCharacteristicValue(CharacteristicType.FireDamage);
		float duration = tower.GetCharacteristicValue(CharacteristicType.FireDuration);
		var settings = GameConfigs.Instance.StatusEffectsSettings;
		int limit = settings != null ? settings.fireMaxStacks : 3;
		u.AddPeriodicEffect(tower, damage, 1f, UnitEffect.Fire, duration, limit);
	}
}
