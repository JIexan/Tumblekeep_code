using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialogue {
	// Полноэкранная чёрная шторка катсцен: старт «с чёрного экрана» и фейды.
	// Пока видна — перехватывает клики (raycastTarget у Image); при alpha 0
	// объект выключается, чтобы не жечь филлрейт и не глотать ввод.
	public class ScreenFader : MonoBehaviour {
		[SerializeField] Image image;

		public bool IsShown => gameObject.activeSelf;

		public void ShowInstant() {
			KillTween();
			SetAlpha(1f);
			gameObject.SetActive(true);
		}

		public void HideInstant() {
			KillTween();
			SetAlpha(0f);
			gameObject.SetActive(false);
		}

		// Твин на unscaled time (катсцена может идти при паузе). onComplete внутри:
		// внешний OnComplete затёр бы автодеактивацию при уходе в прозрачность.
		public Tween FadeTo(float alpha, float duration, Action onComplete = null) {
			KillTween();
			gameObject.SetActive(true);
			return image.DOFade(alpha, duration)
				.SetUpdate(true)
				.SetTarget(this)
				.OnComplete(() => {
					if (alpha <= 0f)
						gameObject.SetActive(false);
					onComplete?.Invoke();
				});
		}

		private void SetAlpha(float alpha) {
			var color = image.color;
			color.a = alpha;
			image.color = color;
		}

		private void KillTween() {
			DOTween.Kill(this);
		}
	}
}
