public class CastleCharacteristicUpgradeData : CharacteristicUpgradeData
{
	public override void Upgrade() {
		Castle.Instance.Upgrade(this);
	}
}
