namespace Gameplay.Relics {
	// Геймплейные значения, которые могут модифицировать реликвии.
	// Каждое значение обязано иметь живой хук в геймплейном коде (см. RelicsManager).
	// Только добавление в конец — значения сериализуются в RelicsSettings.asset.
	public enum RelicValue {
		CoinsPerKill,
		CrystalChance,
		TowerDamage,
		TowerAttackSpeed,
		TowerRadius,
		TowerXPGain,
		CastleRegeneration,
		CastleResistance,
		Luck,
		EnemySpeed,
		EnemyMaxHP,
		EnemyDamageTaken,
		TowerBuildPrice,
		ChestPrice,
		UpgradeRerollPrice,
		SpellSaveChance,
		TowerTraitSlots,
		// Доля опыта за килл, которую получают башни, НЕ участвовавшие в убийстве (flat: 0.25 = 25%)
		TowerSharedXP
	}
}
