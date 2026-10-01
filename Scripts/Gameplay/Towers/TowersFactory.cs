using System.Collections.Generic;
using UnityEngine;

public interface ITowersFactory {
	BaseTower Create(TowerType type);
	BaseTower Create(TowerType type, Vector3 position, Quaternion rotation, Transform parent);
}


public class TowersFactory : ITowersFactory {

	private List<BaseTower> towerPrefabs;

	public TowersFactory(List<BaseTower> towerPrefabs) {
		this.towerPrefabs = towerPrefabs;
	}

	public BaseTower Create(TowerType type) {
		var towerPrefab = GetTowerByType(type);
		return Object.Instantiate(towerPrefab);
	}

	public BaseTower Create(TowerType type, Vector3 position, Quaternion rotation, Transform parent) {
		var towerPrefab = GetTowerByType(type);
		return Object.Instantiate(towerPrefab, position, rotation, parent);
	}

	private BaseTower GetTowerByType(TowerType type) {
		foreach (var tower in towerPrefabs) {
			if (tower.towerType == type)
				return tower;
		}
		return null;
	}
}
