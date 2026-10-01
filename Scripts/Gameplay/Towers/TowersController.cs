using System;
using System.Collections.Generic;
using Gameplay.LevelUpgrades;
using UnityEngine;

public class TowersController : MonoBehaviour {
	private List<BaseTower> towers = new List<BaseTower>();
	private int buildPointsCount;

	private TowerUpgradeSettings towerSettings;
	private ITowersFactory towersFactory;

	private void Awake() {
		towerSettings = GameConfigs.Instance.TowerUpgradeSettings;
		towerSettings.Init();
		towersFactory = new TowersFactory(GameConfigs.Instance.TowerPrefabs);
		InitBuildPoints();
	}

	// Все площадки доступны с начала боя. Их число фиксируется до уничтожения
	// плейсхолдеров при постройке и ограничивает физическую вместимость уровня.
	private void InitBuildPoints() {
		var placeholders = FindObjectsOfType<BuildingPlaceholder>();
		buildPointsCount = placeholders.Length;
	}

	public TowerUpgradeSettings.TowerSettings GetTowerSettings(TowerType type, int level) {
		return towerSettings.GetTowerSettings(type, level);
	}

	public TowerUpgradeSettings.TowerSettings GetTowerSettings(string id) {
		return towerSettings.GetTowerSettings(id);
	}

	public int GetTowerPrice(TowerType type) {
		return Gameplay.Relics.RelicsManager.ApplyPrice(Gameplay.Relics.RelicValue.TowerBuildPrice, towerSettings.GetTowerPrice(type));
	}

	public BaseTower BuildTower(int type, Vector3 position) {
		if (!CanBuildAnotherTower())
			return null;

		BaseTower tower = towersFactory.Create((TowerType)type, position, Quaternion.identity, transform);
		towers.Add(tower);
		GameAudio.Play(GameSound.TowerBuild, tower.transform.position);
		return tower;
	}

	public float GetTowersDPS() {
		float res = 0f;

		foreach (var tower in towers) {
			res += tower.DPS;
		}

		return res;
	}

	public float GetRouteCoverage(int r, float routeLength) {
		float step = 0.5f;

		float currentPosition = 0f;
		int totalCounter = 0;
		int coveredCounter = 0;
		while(currentPosition < routeLength) {
			Vector3 routePoint = LevelDesriptor.Instance.GetRoutePoint(r, currentPosition);
			if (IsPointInsideTowerRadius(routePoint)) {
				coveredCounter++;
			}

			totalCounter++;
			currentPosition += step;
		}

		return coveredCounter / (float)totalCounter;
	}

	private bool IsPointInsideTowerRadius(Vector3 position) {
		foreach (var tower in towers) {
			if ((position - tower.transform.position).sqrMagnitude < tower.Radius * tower.Radius) {
				return true;
			}
		}
		return false;
	}

	public List<BaseTower> GetAllTowers() {
		return towers;
	}

	public int MaxTowersCount => Mathf.Min(buildPointsCount, LevelUpgradesManager.GetTowerLimit());

	public bool CanBuildAnotherTower() {
		return towers.Count < MaxTowersCount;
	}

	public float GetTowersCount() {
		return towers.Count;
	}
}
