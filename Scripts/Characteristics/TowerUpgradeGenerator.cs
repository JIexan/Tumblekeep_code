using System.Collections.Generic;
using UnityEngine;

public static class TowerUpgradeGenerator
{
	private static float traitChance = 0.3f;
	public static List<UpgradeData> GenerateUpgradeOptions(
		BaseTower tower, 
		TowerUpgradeRules rules, 
		RarityConfig rarityConfig,
		CharacteristicSettings chracteristicSettings
		) {
		//������� �������� ���� ������� �����������
		List<UpgradeData> upgradeData = new List<UpgradeData>(3);

		List<CharacteristicType> usedCharacteristics = new List<CharacteristicType>(3);
		List<TowerTraitType> usedTraits = new List<TowerTraitType>();


		for (int i = 0; i < 3; i++) {
			if (Random.value < traitChance) {
				if (TryToGetTraitUpgrade(rules, tower, usedTraits, out var trait)) {
					upgradeData.Add(trait);
				} else {
					if(TryToGetCharacteristicUpgrade(rules, rarityConfig, chracteristicSettings, tower, usedCharacteristics, out var characteristic)) {
						upgradeData.Add(characteristic);
					}
				}
			} else {
				if (TryToGetCharacteristicUpgrade(rules, rarityConfig, chracteristicSettings, tower, usedCharacteristics, out var characteristic)) {
					upgradeData.Add(characteristic);
				}
			}
		}


		return upgradeData;
	}

	private static bool TryToGetCharacteristicUpgrade(
		TowerUpgradeRules rules, 
		RarityConfig rarityConfig, 
		CharacteristicSettings characteristicSettings,
		BaseTower tower, 
		List<CharacteristicType> usedCharacteristics, 
		out TowerCharacteristicUpgradeData characteristic) {
		characteristic = null;
		var characteristicRule = rules.GetRule(UpgradeRuleType.Characteristic);
		if (characteristicRule != null && characteristicRule.MeetsCondition(tower.Level)) {
			Characteristic characteristicData = tower.GetRandomNotMaxedCharacteristic(usedCharacteristics);
			if (characteristicData != null) {
				var settings = characteristicData.Settings;
				bool additive = Random.value > 0.5f;
				var rarity = rarityConfig.SelectRandomRarityWithLuck(Castle.Instance.GetCharacteristicValue(CharacteristicType.Luck), characteristicSettings.Get(CharacteristicType.Luck).MaxValue);
				float value = Random.Range(settings.baseIncreaseMin, settings.baseIncreaseMax);
				Debug.Log($"Characteristic upgrade {characteristicData.type} value={value} multiplier={rarity.multiplier}");
				characteristic = new TowerCharacteristicUpgradeData() {
					rarity = rarity.type,
					tower = tower,
					characteristicType = characteristicData.type,
					additionalValue = (additive ? value * rarity.multiplier : 0f),
					persentValue = (additive ? 0 : Mathf.Max(1, Mathf.RoundToInt((value / settings.MaxValue) * 100f * rarity.multiplier)))
				};
			}
			return true;
		}

		return false;
	}

	private static bool TryToGetTraitUpgrade(TowerUpgradeRules rules, BaseTower tower, List<TowerTraitType> usedTraits, out TowerTraitUpgradeData trait) {
		// ��������� ��������� trait'�
		List<TowerTraitType> availableTraits = new List<TowerTraitType>();

		// �������� ��� ��������� trait'� ��� �����
		var possibleTraits = tower.GetPossibleTraits();

		foreach (var possibleTrait in possibleTraits) {
			// ��������� ��� ��������� � ���� ������ ���������
			if (!usedTraits.Contains(possibleTrait)) {
				availableTraits.Add(possibleTrait);
			}
		}

		if (availableTraits.Count == 0) {
			trait = null;
			return false;
		}

		// Явный кап слотов: без него башня с полными трейтами проваливалась в правило
		// FirstTrait и могла получить лишний трейт. Лимит поднимают реликвии (MaxTraits).
		if (tower.TraitsCount >= tower.MaxTraits) {
			trait = null;
			return false;
		}

		// �������� ��������� ��������� trait
		TowerTraitType selectedTrait = availableTraits[Random.Range(0, availableTraits.Count)];

		// ��������� ������� ��� ���������� trait
		UpgradeRuleType requiredRuleType = UpgradeRuleType.FirstTrait;

		if (tower.TraitsCount == 0) {
			requiredRuleType = UpgradeRuleType.FirstTrait;
		} else if (tower.TraitsCount == 1) {
			requiredRuleType = UpgradeRuleType.SecondTrait;
		} else {
			// 3-й и последующие слоты (4-й от «Шлема зодчего») идут по правилу ThirdTrait
			requiredRuleType = UpgradeRuleType.ThirdTrait;
		}

		var traitRule = rules.GetRule(requiredRuleType);
		if (traitRule != null && traitRule.MeetsCondition(tower.Level)) {
			trait = new TowerTraitUpgradeData() {
				tower = tower,
				trait = selectedTrait
			};
			return true;
		}

		trait = null;
		return false;

	}
}
