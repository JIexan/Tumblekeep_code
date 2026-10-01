public class TowerTraitUpgradeData : UpgradeData
{
	public BaseTower tower;
	public TowerTraitType trait;

	public override void Upgrade() {
		if (tower != null) {
			tower.Upgrade(this);
		}
	}
}
