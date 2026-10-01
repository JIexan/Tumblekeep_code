using DG.Tweening;
using UnityEngine;

namespace UI.Dialogue {
	// Киношные чёрные полосы сверху/снизу экрана — маркер катсцены.
	// Отдельный компонент (не часть DialoguePanel): туториальные реплики
	// используют диалог без полос. Твины на unscaled time — катсцена может идти на паузе.
	public class LetterboxController : MonoBehaviour {
		[SerializeField] RectTransform topBar;
		[SerializeField] RectTransform bottomBar;
		[Tooltip("Высота полосы в пикселях при референсном разрешении 1920x1080.")]
		[SerializeField] float barHeight = 120f;

		public bool IsShown { get; private set; }

		public void Show(float duration) {
			IsShown = true;
			gameObject.SetActive(true);
			Animate(barHeight, duration, null);
		}

		public void Hide(float duration) {
			IsShown = false;
			Animate(0f, duration, () => gameObject.SetActive(false));
		}

		public void HideInstant() {
			IsShown = false;
			KillTweens();
			SetHeight(0f);
			gameObject.SetActive(false);
		}

		private void Animate(float height, float duration, TweenCallback onComplete) {
			KillTweens();
			if (duration <= 0f) {
				SetHeight(height);
				onComplete?.Invoke();
				return;
			}
			topBar.DOSizeDelta(new Vector2(topBar.sizeDelta.x, height), duration).SetUpdate(true).SetTarget(this);
			bottomBar.DOSizeDelta(new Vector2(bottomBar.sizeDelta.x, height), duration).SetUpdate(true).SetTarget(this)
				.OnComplete(onComplete);
		}

		private void SetHeight(float height) {
			topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, height);
			bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, height);
		}

		private void KillTweens() {
			DOTween.Kill(this);
		}
	}
}
