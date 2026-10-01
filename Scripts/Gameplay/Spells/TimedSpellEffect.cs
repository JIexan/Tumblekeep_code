using UnityEngine;

/// <summary>
/// Базовый класс спелла с длительностью (Money, SpeedUp...). Ведёт таймер и даёт
/// SpellTimerPanel единый способ показать оставшееся время
/// </summary>
public abstract class TimedSpellEffect : MonoBehaviour
{
	private bool isPlaying = false;
	private float timer;
	private float duration;

	public bool IsPlaying => isPlaying;
	public float RemainingTime => timer;
	public float Duration => duration;

	protected void StartTimer(float effectDuration) {
		duration = effectDuration;
		timer = effectDuration;
		isPlaying = true;
	}

	protected virtual void Update() {
		if (!isPlaying)
			return;

		timer -= Time.deltaTime;
		if (timer <= 0f) {
			isPlaying = false;
			StopEffect();
		}
	}

	/// <summary>Вызывается по истечении таймера — снять бонус и погасить эффект</summary>
	protected abstract void StopEffect();
}
