using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public abstract class TraitCondition {
	public abstract bool Evaluate(BaseTower tower);
}

[System.Serializable]
public class BasicTraitCondition : TraitCondition {
	public enum ConditionType { HasTrait, NotHasTrait }
	[SerializeField] protected TowerTraitType traitId;
	[SerializeField] protected ConditionType conditionType;
	public override bool Evaluate(BaseTower tower) {
		if (tower == null) return false;
		bool hasTrait = tower.HasTrait(traitId);
		return conditionType == ConditionType.HasTrait ? hasTrait : !hasTrait;
	}
}

[System.Serializable]
public class CountCondition : TraitCondition {
	public enum ConditionType { Less, LEqual, Equal, GEqual, Greater }
	[SerializeField] protected int count;
	[SerializeField] protected ConditionType conditionType;
	public override bool Evaluate(BaseTower tower) {
		if (tower == null) return false;
		int currentCount = tower.TraitsCount;
		switch (conditionType) {
			case ConditionType.Less:
				return count < currentCount;
			case ConditionType.LEqual:
				return count <= currentCount;
			case ConditionType.Equal:
				return count == currentCount;
			case ConditionType.GEqual:
				return count >= currentCount;
			case ConditionType.Greater:
				return count > currentCount;
		}
		return false;
	}
}

[System.Serializable]
public class AndCondition : TraitCondition {
	[SerializeReference] public List<TraitCondition> conditions = new();

	public override bool Evaluate(BaseTower tower) {
		if (conditions.Count == 0) return false;
		foreach (var condition in conditions)
			if (!condition.Evaluate(tower))
				return false;
		return true;
	}
}

[System.Serializable]
public class OrCondition : TraitCondition {
	[SerializeReference] public List<TraitCondition> conditions = new();

	public override bool Evaluate(BaseTower tower) {
		if (conditions.Count == 0) return false;
		foreach (var condition in conditions)
			if (condition.Evaluate(tower))
				return true;
		return false;
	}
}

[System.Serializable]
public class NotCondition : TraitCondition {
	[SerializeReference] public TraitCondition condition;

	public override bool Evaluate(BaseTower tower)
		=> condition != null ? !condition.Evaluate(tower) : false;
}

[System.Serializable]
public abstract class TraitAction {
	[SerializeField] protected GameObject gameObject;

	public abstract void Apply(BaseTower tower);
	public abstract void Revert(BaseTower tower);
}

[System.Serializable]
public class SetGameObjectActiveAction : TraitAction {
	[SerializeField] private bool activeState;

	private Dictionary<GameObject, bool> originalStates = new();

	public override void Apply(BaseTower tower) {
		var target = gameObject;
		if (target != null) {
			if (!originalStates.ContainsKey(gameObject))
				originalStates[gameObject] = target.activeSelf;

			target.SetActive(activeState);
		}
	}

	public override void Revert(BaseTower tower) {
		var target = gameObject;
		if (target != null && originalStates.TryGetValue(target, out bool originalState)) {
			target.SetActive(originalState);
			originalStates.Remove(target);
		}
	}
}

[System.Serializable]
public class ScaleGameObjectAction : TraitAction {
	[SerializeField] private Vector3 scaleMultiplier = Vector3.one;

	private Dictionary<Transform, Vector3> originalScales = new();

	public override void Apply(BaseTower tower) {
		var target = gameObject.transform;
		if (target != null) {
			if (!originalScales.ContainsKey(target))
				originalScales[target] = target.localScale;

			target.localScale = Vector3.Scale(originalScales[target], scaleMultiplier);
		}
	}

	public override void Revert(BaseTower tower) {
		var target = gameObject.transform;
		if (target != null && originalScales.TryGetValue(target, out Vector3 originalScale)) {
			target.localScale = originalScale;
			originalScales.Remove(target);
		}
	}
}

[System.Serializable]
public class CompositeAction : TraitAction {
	[SerializeReference] public List<TraitAction> actions = new();

	public override void Apply(BaseTower tower) {
		foreach (var action in actions)
			action?.Apply(tower);
	}

	public override void Revert(BaseTower tower) {
		foreach (var action in actions)
			action?.Revert(tower);
	}
}