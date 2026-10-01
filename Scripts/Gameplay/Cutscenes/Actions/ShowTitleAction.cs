using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Cutscenes.Actions {
	// Показ заголовка игры (Image в CutsceneOverlay): проявление по альфе,
	// удержание, растворение. Пока — временный спрайт по центру экрана.
	[System.Serializable]
	public class ShowTitleAction : BaseCutsceneAction {
		[SerializeField] float fadeInTime = 0.8f;
		[SerializeField] float holdTime = 1.8f;
		[SerializeField] float fadeOutTime = 0.6f;

		private static Image Title =>
			CutsceneController.Instance != null ? CutsceneController.Instance.TitleImage : null;

		public override void Apply() {
			var title = Title;
			if (title == null)
				return;
			SetAlpha(title, 0f);
			title.gameObject.SetActive(false);
		}

		public override void Play(Action onComplete) {
			var title = Title;
			if (title == null) {
				onComplete?.Invoke();
				return;
			}

			SetAlpha(title, 0f);
			title.gameObject.SetActive(true);
			DOTween.Sequence()
				.SetUpdate(true)
				.SetId(TweenId)
				.Append(title.DOFade(1f, fadeInTime))
				.AppendInterval(holdTime)
				.Append(title.DOFade(0f, fadeOutTime))
				.OnComplete(() => {
					title.gameObject.SetActive(false);
					onComplete?.Invoke();
				});
		}

		private static void SetAlpha(Image image, float alpha) {
			var color = image.color;
			color.a = alpha;
			image.color = color;
		}

#if UNITY_EDITOR
		public void EditorInit(float fadeInTime, float holdTime, float fadeOutTime) {
			this.fadeInTime = fadeInTime;
			this.holdTime = holdTime;
			this.fadeOutTime = fadeOutTime;
		}
#endif
	}
}
