using System.Collections.Generic;

[System.Serializable]
public class SaveDataLevelUpgrades : SaveDataBase {
	public Dictionary<string, List<string>> purchasedByLevel = new Dictionary<string, List<string>>();

	public override void CheckValues() {
		base.CheckValues();
		if (purchasedByLevel == null)
			purchasedByLevel = new Dictionary<string, List<string>>();
	}

	public bool IsPurchased(string levelId, string upgradeId) {
		if (string.IsNullOrEmpty(levelId) || string.IsNullOrEmpty(upgradeId))
			return false;
		return purchasedByLevel.TryGetValue(levelId, out var list) && list != null && list.Contains(upgradeId);
	}

	public void MarkPurchased(string levelId, string upgradeId) {
		if (string.IsNullOrEmpty(levelId) || string.IsNullOrEmpty(upgradeId))
			return;
		if (!purchasedByLevel.TryGetValue(levelId, out var list) || list == null) {
			list = new List<string>();
			purchasedByLevel[levelId] = list;
		}
		if (!list.Contains(upgradeId))
			list.Add(upgradeId);
	}

	public List<string> GetPurchased(string levelId) {
		if (!string.IsNullOrEmpty(levelId) && purchasedByLevel.TryGetValue(levelId, out var list) && list != null)
			return list;
		return new List<string>();
	}
}
