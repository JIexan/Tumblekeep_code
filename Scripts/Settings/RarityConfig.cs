using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RarityType {
	Common,
	Uncommon,
	Rare,
	Epic,
	Legendary
}

[System.Serializable]
public class RaritySettings {
	public RarityType type;
	public Color c;
	public int baseWeight;
	public float luckCoefficient;
	public float multiplier;
}

[CreateAssetMenu]
public class RarityConfig : ScriptableObject
{
	[SerializeField] private List<RaritySettings> rarities = new List<RaritySettings>();

	/// <summary>
	/// Выбрать случайную редкость с учетом удачи
	/// </summary>
	public RaritySettings SelectRandomRarityWithLuck(float luck, float maxLuck) {

		if (rarities.Count == 0) {
			Debug.LogError("Система лута не настроена!");
			return null;
		}

		// Фильтруем редкости, которые могут выпадать
		var availableRarities = rarities;

		if (availableRarities.Count == 0)
			return null;

		// Рассчитываем веса с учетом удачи
		List<float> weights = new List<float>();
		float totalWeight = 0;

		foreach (var rarity in availableRarities) {
			float weight = CalculateWeightWithLuck(rarity, luck, maxLuck);
			weights.Add(weight);
			totalWeight += weight;
		}

		if (totalWeight <= 0)
			return availableRarities[0];

		// Выбор случайной редкости
		float randomValue = Random.Range(0f, totalWeight);
		float currentSum = 0;

		for (int i = 0; i < availableRarities.Count; i++) {
			currentSum += weights[i];
			if (randomValue <= currentSum) {
				var selectedRarity = availableRarities[i];

				// Проверка на "удачное улучшение"
				/*if (config.EnableLuckSystem && config.LuckyUpgradeChance > 0) {
					selectedRarity = TryLuckyUpgrade(selectedRarity, playerLuck);
				}*/

				// Логирование для отладки
				Debug.Log($"Выбрана редкость: {selectedRarity.type} " +
							$"(Исходный вес: {availableRarities[i].baseWeight}, " +
							$"С учетом удачи: {weights[i]}, " +
							$"Шанс: {(weights[i] / totalWeight) * 100:F2}%, " +
							$"Удача: {luck})");
				

				return selectedRarity;
			}
		}

		return availableRarities[availableRarities.Count - 1];
	}

	public RaritySettings Get(RarityType rarity) {
		for (int i = 0; i < rarities.Count; i++) {
			if (rarities[i].type == rarity) {
				return rarities[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Рассчитать вес редкости с учетом удачи (формула вариант 1 - мультипликативный)
	/// </summary>
	private float CalculateWeightWithLuck(RaritySettings rarity, float luck, float maxLuck) {
		if (luck <= 0)
			return rarity.baseWeight;

		// Формула: Базовый_Вес * (1 + (Luck / MaxLuck) * Коэффициент_Редкости)
		float normalizedLuck = Mathf.Clamp01((float)luck / maxLuck);
		float luckMultiplier = 1 + (normalizedLuck * rarity.luckCoefficient);

		return rarity.baseWeight * luckMultiplier;
	}
}
