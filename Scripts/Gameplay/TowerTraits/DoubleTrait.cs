public class DoubleTrait : AbstractTrait {
	public DoubleTrait(BaseTower tower) : base(tower) {
		type = TowerTraitType.Double;
	}

	public override void Install() {
		if (tower.HasDistinctSecondWeapon) {
			// Урон на ствол режется вдвое — компенсация в самом втором стволе
			tower.Characteristics.ModifyValue(CharacteristicType.Damage, 0.5f);
			tower.Characteristics.ModifyValue(CharacteristicType.ProjectileDamage, 0.5f);
		} else {
			// Второго ствола нет (Laser/Mortar): вместо чистого нерфа ×0.5 ускоряем перезарядку
			tower.Characteristics.Get(CharacteristicType.Speed)?.AddBonus(40f);
		}
	}

	public override void DamageUnitWithProjectile(Unit u) {	}

	public override void DamageUnitInArea(Unit u) {
		
	}
}
