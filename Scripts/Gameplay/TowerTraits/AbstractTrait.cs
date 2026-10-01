public abstract class AbstractTrait
{
	protected BaseTower tower;
	protected TowerTraitType type;

	public TowerTraitType Type => type;

	public AbstractTrait(BaseTower tower) {
		this.tower = tower;
	}

	public abstract void DamageUnitWithProjectile(Unit u);

	public abstract void DamageUnitInArea(Unit u);

	public abstract void Install();
}
