#if AUTOPLAY
using System.Collections.Generic;
using UnityEngine;

namespace Autoplay {
	/// <summary>
	/// Выбор варианта апгрейда башни: максимальный прирост DPS по предиктору + небольшие бонусы
	/// за полезные не-DPS статы; трейты — по эвристической таблице.
	/// </summary>
	public static class UpgradeScorer {
		// SuperDamage/SuperSlow не реализованы в TraitsFactory (бросают) — их не берём никогда
		private const float ForbiddenScore = -100f;

		public static UpgradeData Best(BaseTower tower, List<UpgradeData> options, System.Random rng, out string description) {
			description = "";
			if (tower == null || options == null || options.Count == 0)
				return null;

			float now = TowerDpsPredictor.Current(tower);
			UpgradeData best = null;
			float bestScore = float.NegativeInfinity;

			foreach (var option in options) {
				if (option == null)
					continue;
				float score;
				string why;
				switch (option) {
					case TowerCharacteristicUpgradeData c: {
						float after = TowerDpsPredictor.AfterUpgrade(tower, c);
						float gain = (after - now) / Mathf.Max(now, 0.01f);
						score = gain + SideBonus(c.characteristicType);
						string delta = Mathf.Abs(c.additionalValue) > 0.0001f ? "+" + c.additionalValue.ToString("0.##") : "+" + c.persentValue + "%";
						why = $"{c.characteristicType} {delta} (dps {gain * 100f:+0.#;-0.#}%)";
						break;
					}
					case TowerTraitUpgradeData t:
						score = TraitScore(tower, t.trait);
						why = $"trait {t.trait}";
						break;
					default:
						score = 0f;
						why = option.GetType().Name;
						break;
				}
				if (score <= ForbiddenScore)
					continue;
				score += (float)rng.NextDouble() * 0.001f; // тай-брейк
				if (score > bestScore) {
					bestScore = score;
					best = option;
					description = why;
				}
			}
			return best;
		}

		// Ценность статов, не входящих в формулу DPS напрямую (в долях DPS-прироста)
		private static float SideBonus(CharacteristicType type) {
			switch (type) {
				case CharacteristicType.Radius: return 0.15f;
				case CharacteristicType.ProjectileAOERadius: return 0.15f;
				case CharacteristicType.Slowdown: return 0.10f;
				case CharacteristicType.FireDamage:
				case CharacteristicType.FireDuration:
				case CharacteristicType.PoisonDamage:
				case CharacteristicType.PoisonDuration: return 0.10f;
				case CharacteristicType.DischargeTime: return 0.05f;
				case CharacteristicType.ExpGain: return 0.05f;
				case CharacteristicType.RotationSpeed: return 0.03f;
				default: return 0f;
			}
		}

		private static float TraitScore(BaseTower tower, TowerTraitType trait) {
			switch (trait) {
				case TowerTraitType.Double: return tower.HasDistinctSecondWeapon ? 1.0f : 0.3f;
				case TowerTraitType.Fire: return 0.25f;
				case TowerTraitType.Poison: return 0.25f;
				case TowerTraitType.Heavy: return 0.2f;
				case TowerTraitType.SuperDamage:
				case TowerTraitType.SuperSlow: return ForbiddenScore;
				default: return 0.1f;
			}
		}
	}
}
#endif
