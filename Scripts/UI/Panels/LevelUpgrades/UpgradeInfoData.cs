// Данные для UpgradeInfoWindow: пара строк вместо ссылок на объекты —
// окно само резолвит свежее состояние через LevelUpgradesManager.
public readonly struct UpgradeInfoData {
	public readonly string LevelId;
	public readonly string UpgradeId;

	public UpgradeInfoData(string levelId, string upgradeId) {
		LevelId = levelId;
		UpgradeId = upgradeId;
	}
}
