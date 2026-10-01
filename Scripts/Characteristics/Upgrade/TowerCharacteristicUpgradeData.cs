public class TowerCharacteristicUpgradeData : CharacteristicUpgradeData
{
	public BaseTower tower;

	public override void Upgrade() {
		if (tower != null) {
			tower.Upgrade(this);
		}
	}
}
