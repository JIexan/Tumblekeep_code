#if AUTOPLAY
using Gameplay.Relics;
using UnityEngine;

namespace Autoplay {
	/// <summary>
	/// DPS живой башни «как если бы» одна характеристика имела другое значение. Зеркалит формулы
	/// DPS оружий (ArcherWeapon, DoubleArcherWeapon, LaserWeapon, MortarWeapon, AltElectricWeapon,
	/// ElectricWeapon) так же, как BookTowerStats — но поверх текущих значений с реликвиями и уровнем.
	/// </summary>
	public static class TowerDpsPredictor {
		public static float Current(BaseTower tower) => Predict(tower, null, 0f);

		/// <summary>DPS после применения апгрейда характеристики (та же математика, что Characteristic.AddBonus/AddPercent)</summary>
		public static float AfterUpgrade(BaseTower tower, TowerCharacteristicUpgradeData data) {
			var characteristic = tower.Characteristics != null ? tower.Characteristics.Get(data.characteristicType) : null;
			var settings = GameConfigs.Instance.CharacteristicSettings.Get(data.characteristicType);
			if (characteristic == null || settings == null)
				return Current(tower);

			float current = characteristic.Get(); // клампнутое, без реликвий
			float sign = settings.positiveIncrease ? 1f : -1f;
			float next;
			if (Mathf.Abs(data.additionalValue) > 0.0001f) {
				next = current + sign * data.additionalValue;
			} else {
				float factor = characteristic.GetPercent();
				float nextFactor = factor + sign * data.persentValue / 100f;
				next = factor > 0.0001f ? current * nextFactor / factor : current;
			}
			next = Mathf.Clamp(next, settings.MinValue, settings.MaxValue);
			return Predict(tower, data.characteristicType, next);
		}

		// Значение характеристики как его видит оружие (BaseTower.GetCharacteristicValue),
		// с подменой одной характеристики на сырое значение
		private static float Value(BaseTower tower, CharacteristicType type, CharacteristicType? overrideType, float overrideRaw) {
			if (overrideType.HasValue && overrideType.Value == type) {
				float v = overrideRaw * RelicsManager.GetTowerCharacteristicFactor(type);
				if (type == CharacteristicType.Speed)
					v *= SpellsManager.TowerSpeedFactor;
				return v;
			}
			if (tower.Characteristics == null || !tower.Characteristics.Contains(type))
				return 0f;
			return tower.GetCharacteristicValue(type);
		}

		public static float Predict(BaseTower tower, CharacteristicType? overrideType, float overrideRaw) {
			if (tower == null)
				return 0f;
			var weapon = tower.GetComponentInChildren<BaseTowerWeapon>(false);
			if (weapon == null)
				return 0f;

			float V(CharacteristicType type) => Value(tower, type, overrideType, overrideRaw);

			float relicFactor = Mathf.Max(0.01f, RelicsManager.GetFactor(RelicValue.TowerAttackSpeed));
			float reload = weapon.GetBaseReloadTime(V(CharacteristicType.Speed)) / relicFactor;
			float radius = V(CharacteristicType.Radius);
			float projectileSpeed = Mathf.Max(V(CharacteristicType.ProjectileSpeed), 0.01f);
			float projectileDamage = V(CharacteristicType.ProjectileDamage);
			float flight = radius / projectileSpeed;

			switch (weapon) {
				case DoubleArcherWeapon _:
					return projectileDamage * 2f / (reload + flight);
				case ArcherWeapon _:
					return projectileDamage / (reload + flight);
				case LaserWeapon _:
					return V(CharacteristicType.Damage) / Mathf.Max(V(CharacteristicType.FireTime) + reload, 0.01f);
				case MortarWeapon mortar:
					return projectileDamage / (reload + radius * 2f / projectileSpeed) * mortar.AoeEfficiency;
				case AltElectricWeapon electric:
					return projectileDamage * electric.MaxTargets / (reload + flight);
				case ElectricWeapon _:
					return projectileDamage / (reload + flight);
				default:
					return projectileDamage > 0f
						? projectileDamage / (reload + flight)
						: V(CharacteristicType.Damage) / Mathf.Max(reload, 0.01f);
			}
		}
	}
}
#endif
