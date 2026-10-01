public class PoisonTrait : AbstractTrait {
	public PoisonTrait(BaseTower tower) : base(tower) {
		type = TowerTraitType.Poison;
	}

	public override void Install() {
		tower.AddCharacteristic(CharacteristicType.PoisonDamage);
		tower.AddCharacteristic(CharacteristicType.PoisonDuration);
	}

	// Яд не стакается параллельно: повторное попадание обновляет длительность и поднимает концентрацию
	public override void DamageUnitWithProjectile(Unit u) {
		Poison(u);
	}

	public override void DamageUnitInArea(Unit u) {
		Poison(u);
	}

	private void Poison(Unit u) {
		u.ApplyPoison(tower, tower.GetCharacteristicValue(CharacteristicType.PoisonDamage), tower.GetCharacteristicValue(CharacteristicType.PoisonDuration));
	}
}
