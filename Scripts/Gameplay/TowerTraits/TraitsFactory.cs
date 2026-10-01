using System;

public interface ITraitFactory {
	AbstractTrait Create(BaseTower tower, TowerTraitType type);
}

public class TraitsFactory : ITraitFactory {
	public AbstractTrait Create(BaseTower tower, TowerTraitType type) {
		return type switch {
			TowerTraitType.Heavy => new HeavyTrait(tower),
			TowerTraitType.Double => new DoubleTrait(tower),
			TowerTraitType.Fire => new FireTrait(tower),
			TowerTraitType.Poison => new PoisonTrait(tower),
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}
}
