using UnityEngine;
using DG.Tweening;

/// <summary>
/// «Боевой горн»: на время поднимает характеристику Speed всех башен на процент
/// (множитель читается в BaseTower.GetCharacteristicValue через SpellsManager.TowerSpeedFactor).
/// Визуал — виньетка VFX_Vignette_Speed_Anime_Example на камере, фейд через свойство материала
/// </summary>
public class SpeedUpSpellEffect : TimedSpellEffect
{
	[Tooltip("Корень виньетки на камере (VFX_Vignette_Speed_Anime_Example)")]
	[SerializeField] private ParticleSystem vignette;
	[Tooltip("Рендерер бесконечной частицы виньетки — у него фейдится материал")]
	[SerializeField] private ParticleSystemRenderer fadeRenderer;
	[SerializeField] private string materialPropertyName = "_OpacityMultiply";
	[SerializeField] private float fadeInDuration = 0.5f;
	[SerializeField] private float fadeOutDuration = 0.5f;

	// Инстанс материала, чтобы не портить ассет (у MoneySpellEffect правится сам ассет)
	private Material fadeMaterial;
	private Tween fadeTween;

	private Material FadeMaterial {
		get {
			if (fadeMaterial == null && fadeRenderer != null)
				fadeMaterial = fadeRenderer.material;
			return fadeMaterial;
		}
	}

	public void StartEffect(float speedPercent, float effectDuration) {
		StartTimer(effectDuration);
		SpellsManager.TowerSpeedFactor = 1f + speedPercent / 100f;

		if (vignette != null) {
			vignette.gameObject.SetActive(true);
			vignette.Play(true);
		}
		Fade(0f, 1f, fadeInDuration);
	}

	protected override void StopEffect() {
		SpellsManager.TowerSpeedFactor = 1f;
		float from = FadeMaterial != null ? FadeMaterial.GetFloat(materialPropertyName) : 1f;
		Fade(from, 0f, fadeOutDuration, () => {
			if (vignette != null) {
				vignette.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
				vignette.gameObject.SetActive(false);
			}
		});
	}

	private void Fade(float from, float to, float duration, TweenCallback onComplete = null) {
		var mat = FadeMaterial;
		if (mat == null) {
			onComplete?.Invoke();
			return;
		}
		fadeTween?.Kill();
		mat.SetFloat(materialPropertyName, from);
		fadeTween = DOTween.To(() => mat.GetFloat(materialPropertyName),
			x => mat.SetFloat(materialPropertyName, x), to, duration);
		if (onComplete != null)
			fadeTween.OnComplete(onComplete);
	}

	private void OnDestroy() {
		fadeTween?.Kill();
		// Уровень мог закончиться посреди эффекта — бонус не должен утечь в следующую катку
		if (IsPlaying)
			SpellsManager.TowerSpeedFactor = 1f;
		if (fadeMaterial != null)
			Destroy(fadeMaterial);
	}
}
