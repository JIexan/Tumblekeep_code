using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeRuleType {
	Characteristic,
	FirstTrait,
	SecondTrait,
	ThirdTrait
}

[System.Serializable]
public class TowerUpgradeRule {
	[HorizontalGroup(Width = 100)]
	[HideLabel]
	public UpgradeRuleType ruleType;
	[HorizontalGroup]
	[MinMaxSlider(0, 100, true)]
	[LabelWidth(35)]
	public Vector2Int level;

	public float baseChance;

	public int MinLevel => level.x;
	public int MaxLevel => level.y;

	public bool MeetsCondition(int level) {
		return level >= MinLevel && level <= MaxLevel;
	}

	public bool TryToSelect() {
		return Random.value <= baseChance;
	}

}

[CreateAssetMenu]
public class TowerUpgradeRules : ScriptableObject
{
	public List<TowerUpgradeRule> upgradeRules;

	public TowerUpgradeRule GetRule(UpgradeRuleType type) {
		for (int i = 0; i < upgradeRules.Count; i++) {
			if(upgradeRules[i].ruleType == type) 
				return upgradeRules[i];
		}
		return null;
	}
}
