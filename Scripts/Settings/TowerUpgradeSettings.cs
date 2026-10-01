using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityType {
	PoisonSlowness,
	PoisonDamage,
	Ignite,
	ExplosiveArrow,
	HeadShot,
	FlyingSlowdownOnHit,
	AdditionalTarget,
	AdditionalDamage,
	FlyingDamage,
	FlyingSlowdown,
	ElecticAdditionalShot,
	ReloadModifier,
	AOERadiusIncrease,
	MortarAdditionalShot,
	Heal
	//хз шо еще
}

[CreateAssetMenu]
public class TowerUpgradeSettings : ScriptableObject
{
	public List<TowerSettings> settings;

	[System.Serializable]
	public class TowerSettings {
		public string id;
		public string name;
		public string discription;
		public string prefabID;
		public string iconName;
		public string nextUpgradesIDs;
		public TowerType type;
		public int level;
		public float minDamage;
        public float maxDamage;
		public float damageRadius; //используется только если башня АОЕ
		public float radius;
		public float reloadTime;
		public float fireTime;
		public int price;
		public List<AbilitySettings> firstAblityBranch;
		public List<AbilitySettings> secondAblityBranch;

		public bool HasAbilities {
			get { return firstAblityBranch.Count != 0 && secondAblityBranch.Count != 0; }
		}

		private TowerSettings[] nextUpgrades;

		public int NextUpgradesCount {
			get { return nextUpgrades.Length; }
		}

		public TowerSettings GetNextUpgrade(int i) {
			return nextUpgrades[i];
		}

		public void Init(TowerSettings[] nextUpgrades) {
			this.nextUpgrades = nextUpgrades;
		}
	}

	public void Init() {
		foreach (var td in settings) {
			td.Init(GetTowerSettingsList(td.nextUpgradesIDs));
		}
	}

	private TowerSettings[] GetTowerSettingsList(string idString) {
		List<TowerSettings> res = new List<TowerSettings>(1);
		string[] idList = idString.Split(',');
		foreach(var id in idList) {
			TowerSettings s = GetTowerSettings(id);
			if(s != null)
				res.Add(s);
		}
		return res.ToArray();
	}

	[System.Serializable]
	public class AbilitySettings {
		public string id;
		public AbilityType type;
		public float value1;
		public float value2;
		public float value3;
	}


	public int GetTowerPrice(TowerType type, int level = 0) {
		foreach (var td in settings) {
			if (td.type == type && td.level == level) {
				return td.price;
			}
		}

		Debug.LogError("Please add price for level " + level + " " + type + " tower type");
		return 0;

	}

	public TowerSettings GetTowerSettings(TowerType type, int level) {
		foreach (var td in settings) {
			if (td.type == type && td.level == level) {
				return td;
			}
		}
		return null;
	}

	public TowerSettings GetTowerSettings(string id) {
		foreach (var td in settings) {
			if (td.id == id) {
				return td;
			}
		}
		return null;
	}

}
