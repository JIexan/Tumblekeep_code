using System.Collections.Generic;
using UnityEngine;

public static class CastleUpgradeGenerator
{
    public static List<UpgradeData> GenerateUpgradeOptions() {
        var upgrades = new List<UpgradeData>(3);
        var candidates = Castle.Instance.GetAvailableUpgradeTypes();
        while (upgrades.Count < 3 && candidates.Count > 0) {
            int index = Random.Range(0, candidates.Count);
            var type = candidates[index];
            candidates.RemoveAt(index);
            bool weaponStat = type == CharacteristicType.Speed || type == CharacteristicType.ProjectileDamage;
            var settings = GameConfigs.Instance.CharacteristicSettings.Get(type);
            bool additive = weaponStat || Random.value > 0.5f;
            upgrades.Add(new CastleCharacteristicUpgradeData {
                characteristicType = type,
                additionalValue = additive ? (weaponStat ? Random.Range(settings.baseIncreaseMin, settings.baseIncreaseMax) : Random.Range(0.1f, 1f)) : 0f,
                persentValue = additive ? 0 : Random.Range(1, 5)
            });
        }
        return upgrades;
    }
}
