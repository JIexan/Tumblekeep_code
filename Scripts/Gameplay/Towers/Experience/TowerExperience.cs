using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class XPFormulaConfig {
	public enum GrowthType {
		Linear,
		Exponential,
		Quadratic,
		Custom
	}

	public GrowthType growthType = GrowthType.Exponential;
	public int baseXP = 100;
	[ShowIf("growthType", GrowthType.Exponential)]
	public float multiplier = 1.5f;
	[ShowIf("growthType", GrowthType.Linear)]
	public int linearIncrement = 50;
	[ShowIf("growthType", GrowthType.Quadratic)]
	public int quadraticFactor = 50;
	[ShowIf("growthType", GrowthType.Custom)]
	public float customExponent = 1.8f;
	[ShowIf("growthType", GrowthType.Custom)]
	public int customFactor = 75;

	// Сколько XP стоит переход НА targetLevel. Чистая функция от конфига —
	// переиспользуется опытом башен и опытом островов.
	public int CalculateXPRequirement(int targetLevel) {
		// Уровень 1 не требует опыта
		if (targetLevel <= 1) return 0;

		switch (growthType) {
			case GrowthType.Linear:
				return baseXP + (targetLevel - 2) * linearIncrement;

			case GrowthType.Exponential:
				return Mathf.RoundToInt(baseXP * Mathf.Pow(multiplier, targetLevel - 2));

			case GrowthType.Quadratic:
				return baseXP + Mathf.RoundToInt(Mathf.Pow(targetLevel - 1, 2) * quadraticFactor);

			case GrowthType.Custom:
				return baseXP + Mathf.RoundToInt(
					Mathf.Pow(targetLevel - 1, customExponent) * customFactor);

			default:
				return baseXP;
		}
	}
}

[System.Serializable]
public class TowerExperince
{
	[SerializeField] GameObject levelUpIndicator;

	[SerializeField] private XPFormulaConfig xpConfig;

	[SerializeField] private int currentLevel = 1;
	[SerializeField] private int currentXP = 0;
	[SerializeField] private int totalXP = 0;

	private Dictionary<int, int> requirementsCache = new Dictionary<int, int>();

	public int CurrentLevel => currentLevel;
	public int CurrentXP => currentXP;
	public int TotalXP => totalXP;

	public event System.Action<int> OnLevelUp; // int - новый уровень
	public event System.Action<int> OnXPChanged; // int - новое количество XP

	public event System.Action OnUpgradeReady;

	// Метод для добавления опыта за убийство моба
	public void AddXP(int xpAmount) {
		if (xpAmount <= 0) return;

		bool wasReady = CanLevelUp();
		currentXP += xpAmount;
		totalXP += xpAmount;
		OnXPChanged?.Invoke(currentXP);
		if (!wasReady && CanLevelUp()) OnUpgradeReady?.Invoke();

		if (levelUpIndicator != null) {
			levelUpIndicator.SetActive(CanLevelUp());
		}
	}

	public bool CanLevelUp() {
		int requiredXP = GetXPRequirement(currentLevel + 1);
		return currentXP >= requiredXP;
	}

	public void LevelUp() {
		currentLevel++;
		int requiredXP = GetXPRequirement(currentLevel);
		currentXP -= requiredXP;

		if (levelUpIndicator != null) {
			levelUpIndicator.SetActive(CanLevelUp());
		}

		OnLevelUp?.Invoke(currentLevel);

		// Здесь можно добавить логику улучшения башни:
		// - увеличение урона
		// - уменьшение перезарядки
		// - новые способности
		// - изменение визуала
	}

	public int GetXPRequirement(int targetLevel) {
		// Уровень 1 не требует опыта
		if (targetLevel <= 1) return 0;

		// Используем кэш для оптимизации
		if (requirementsCache.TryGetValue(targetLevel, out int cachedValue))
			return cachedValue;

		int requiredXP = xpConfig.CalculateXPRequirement(targetLevel);
		requirementsCache[targetLevel] = requiredXP;

		return requiredXP;
	}

	public int GetTotalXPForLevel(int level) {
		int total = 0;
		for (int lvl = 2; lvl <= level; lvl++) {
			total += GetXPRequirement(lvl);
		}
		return total;
	}

	public float GetProgressToNextLevel() {
		//if (currentLevel <= 1) return 0f;

		int currentLevelReq = GetXPRequirement(currentLevel);
		int nextLevelReq = GetXPRequirement(currentLevel + 1);
		//int xpForCurrentLevel = currentXP + currentLevelReq;

		if (nextLevelReq <= 0) return 1f;

		return Mathf.Clamp01((float)currentXP / nextLevelReq);
	}

	public void ResetToLevel(int level) {
		if (level < 1) level = 1;

		currentLevel = 1;
		currentXP = 0;
		totalXP = 0;

		// Добавляем опыт для достижения нужного уровня
		for (int lvl = 2; lvl <= level; lvl++) {
			currentXP += GetXPRequirement(lvl);
			totalXP += GetXPRequirement(lvl);
			currentLevel = lvl;
		}
	}
}
