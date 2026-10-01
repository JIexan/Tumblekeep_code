using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TraitRule {
	[SerializeReference] public TraitCondition condition;
	[SerializeReference] public TraitAction action;

	public bool CheckAndApply(BaseTower tower) {
		if (condition == null || action == null) return false;

		if (condition.Evaluate(tower)) {
			action.Apply(tower);
			return true;
		} else {
			action.Revert(tower);
			return false;
		}
	}
}

public class TowerVisualManager : MonoBehaviour {
	[SerializeField] private BaseTower tower;
	[SerializeField] private List<TraitRule> rules = new();

	private void Start() {
		if (tower == null)
			tower = GetComponent<BaseTower>();

		if (tower != null)
			tower.OnUpdateState += OnTowerStateUpdated;

		ApplyRules();
	}

	private void OnDestroy() {
		if (tower != null)
			tower.OnUpdateState -= OnTowerStateUpdated;
	}

	private void OnTowerStateUpdated() {
		ApplyRules();
	}

	private void ApplyRules() {
		foreach (var rule in rules)
			rule?.CheckAndApply(tower);
	}
}