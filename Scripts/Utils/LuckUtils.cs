using UnityEngine;

// Удача замка в виде нормализованного коэффициента 0..1 и множитель веса по рарности
// (формула RarityConfig.CalculateWeightWithLuck: 1 + luck01 * luckCoefficient).
// Общая точка для сундуков (вес тира, вес реликвии); null-safe — без замка/в редакторе удача = 0.
public static class LuckUtils {
	public static float GetLuck01() {
		if (!Application.isPlaying)
			return 0f;
		var castle = Castle.Instance;
		if (castle == null)
			return 0f;
		float luck = castle.GetCharacteristicValue(CharacteristicType.Luck);
		if (luck <= 0f)
			return 0f;

		float maxLuck = 1000f;
		var configs = GameConfigs.Instance;
		var luckData = configs != null && configs.CharacteristicSettings != null
			? configs.CharacteristicSettings.Get(CharacteristicType.Luck)
			: null;
		if (luckData != null && luckData.MaxValue > 0f)
			maxLuck = luckData.MaxValue;
		return Mathf.Clamp01(luck / maxLuck);
	}

	public static float GetRarityLuckMultiplier(RarityType rarity) {
		return GetRarityLuckMultiplier(rarity, GetLuck01());
	}

	public static float GetRarityLuckMultiplier(RarityType rarity, float luck01) {
		if (luck01 <= 0f)
			return 1f;
		var configs = GameConfigs.Instance;
		var settings = configs != null && configs.RarityConfig != null ? configs.RarityConfig.Get(rarity) : null;
		if (settings == null)
			return 1f;
		return 1f + luck01 * settings.luckCoefficient;
	}
}
