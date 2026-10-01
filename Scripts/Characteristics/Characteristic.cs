using System;
using UnityEngine;

[System.Serializable]
public class Characteristic : DynamicFloat 
{
	[HideInInspector] public CharacteristicType type;

	// Кэш настроек. В редакторе у объектов префаба-ассета сюда попадает «пустой» CharacteristicData
	// (тип Radius, границы 0..0) — поэтому кэш валидируем по типу и диапазону, а не только по null.
	[System.NonSerialized] private CharacteristicData settings;

	public CharacteristicData Settings { 
		get {
			if (settings == null || settings.type != type || (settings.MinValue == 0f && settings.MaxValue == 0f)) {
				settings = GameConfigs.Instance.CharacteristicSettings.Get(type);
			}

			return settings;
		}
	}

	public bool IsMaxed => Get() < Settings.MaxValue;

	public Characteristic() : base() {
	}

	public Characteristic(float value) : base(value) {
	}

	public override void AddBonus(float bonus) {
		if (Settings.positiveIncrease)
			base.AddBonus(bonus);
		else
			base.AddBonus(-bonus);
	}

	public override void AddPercent(int percent) {
		if(settings.positiveIncrease) {
			base.AddPercent(percent);
		} else {
			base.SubPercent(percent);
		}
	}

	public static implicit operator float(Characteristic value) {
		return value.Get();
	}

	public override float Get() {
		return Mathf.Clamp(base.Get(), Settings.MinValue, Settings.MaxValue);
	}

	// Preview the same additive bonus/factor changes as Upgrade, without mutating the live stat.
	public float GetUpgradePreview(float additionalValue, int percentValue) {
		var config = Settings;
		float direction = config.positiveIncrease ? 1f : -1f;
		float upgradedBonus = bonus + additionalValue * direction;
		float upgradedFactor = factor + percentValue / 100f * direction;
		return Mathf.Clamp((value + upgradedBonus) * upgradedFactor, config.MinValue, config.MaxValue);
	}

	internal void SetBaseValueToMin() {
		value = Settings.MinValue;
		UpdateValue();
	}

	internal void ModifyByPersent(float multiplier) {
		factor *= multiplier;
		UpdateValue();
	}
}
