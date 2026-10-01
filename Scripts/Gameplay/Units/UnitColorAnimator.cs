using UnityEngine;
using DG.Tweening;

public class UnitColorAnimator : MonoBehaviour {
	[Header("Color Settings")]
	[SerializeField] private Color unitColor = Color.white;
	[SerializeField] private AnimationPreset damageAnimation;

	private Renderer[] renderers;
	private MaterialPropertyBlock propertyBlock;

	// Твин для текущей анимации
	private Tweener currentColorTween;
	private Tweener currentIntensityTween;

	void Start() {
		renderers = GetComponentsInChildren<Renderer>();
		propertyBlock = new MaterialPropertyBlock();
	}

	// Основной метод обновления цвета
	public void UpdateColor() {
		if (renderers == null || renderers.Length == 0 || propertyBlock == null) return;

		for (int i = 0; i < renderers.Length; i++) {
			// Получаем текущие свойства
			renderers[i].GetPropertyBlock(propertyBlock);

			// Устанавливаем новые значения
			propertyBlock.SetColor("_InstanceColor", unitColor);

			// Применяем изменения
			renderers[i].SetPropertyBlock(propertyBlock);
		}

	}

	public void SetColor(Color newColor) {
		unitColor = newColor;
		UpdateColor();
	}

	#region DOTween Animations

	public void StartBlinkAnimation(AnimationPreset preset) {
		// Отменяем предыдущие анимации
		KillCurrentTweens();

		// Сохраняем начальные значения
		Color originalColor = unitColor;

		// Начинаем новую анимацию
		currentColorTween = DOVirtual.Color(originalColor, preset.color, preset.duration, SetColor)
			.SetEase(preset.ease)
			.SetLoops(preset.loops, preset.loopType)
			.OnComplete(() => {
				// Возвращаем оригинальный цвет после завершения
				SetColor(originalColor);
				currentColorTween = null;
			})
			.OnKill(() => {
				// При принудительной отмене также возвращаем оригинальный цвет
				if (currentColorTween != null) {
					SetColor(originalColor);
					currentColorTween = null;
				}
			});
	}

	// Остановка текущей анимации
	public void StopAnimation() {
		KillCurrentTweens();
	}

	// Отмена всех текущих твинов
	private void KillCurrentTweens() {
		if (currentColorTween != null && currentColorTween.IsActive()) {
			currentColorTween.Kill();
			currentColorTween = null;
		}

		if (currentIntensityTween != null && currentIntensityTween.IsActive()) {
			currentIntensityTween.Kill();
			currentIntensityTween = null;
		}
	}

	// Проверка, идет ли анимация
	public bool IsAnimating() {
		return (currentColorTween != null && currentColorTween.IsActive()) ||
			   (currentIntensityTween != null && currentIntensityTween.IsActive());
	}

	#endregion

	public void PlayDamageAnimation() {
		StartBlinkAnimation(damageAnimation);
	}

	void OnDestroy() {
		// Важно отменять твины при уничтожении объекта
		KillCurrentTweens();
	}

	// Дополнительный вспомогательный класс для управления анимациями
	[System.Serializable]
	public class AnimationPreset {
		public string name;
		public Color color;
		public float intensity = 1f;
		public float duration = 0.5f;
		public int loops = 1;
		public LoopType loopType;
		public Ease ease = Ease.InOutSine;
	}
}