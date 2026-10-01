using UnityEngine;

/// <summary>
/// «Защита»: на время даёт замку плоский бонус к Resistance
/// (читается в Castle.GetCharacteristicValue через SpellsManager.CastleResistanceBonus).
/// Визуала пока нет — при желании подключить ParticleSystem в поле vfx
/// </summary>
public class ProtectSpellEffect : TimedSpellEffect
{
	[Tooltip("Опциональный эффект щита на замке; играет пока действует спелл")]
	[SerializeField] private ParticleSystem vfx;

	public void StartEffect(float resistanceBonus, float effectDuration) {
		StartTimer(effectDuration);
		SpellsManager.CastleResistanceBonus = resistanceBonus;

		if (vfx != null) {
			vfx.gameObject.SetActive(true);
			vfx.Play(true);
		}
	}

	protected override void StopEffect() {
		SpellsManager.CastleResistanceBonus = 0f;
		if (vfx != null)
			vfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
	}

	private void OnDestroy() {
		// Уровень мог закончиться посреди эффекта — бонус не должен утечь в следующую катку
		if (IsPlaying)
			SpellsManager.CastleResistanceBonus = 0f;
	}
}
