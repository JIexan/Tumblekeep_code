using System;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.Cutscenes.Actions {
	// Фейд полноэкранной чёрной шторки (ScreenFader в CutsceneOverlay).
	// toBlack = true — затемнение, false — проявление сцены из черноты.
	[System.Serializable]
	public class ScreenFadeAction : BaseCutsceneAction {
		[SerializeField] bool toBlack;
		[SerializeField] float duration = 1f;

		private static UI.Dialogue.ScreenFader Fader =>
			CutsceneController.Instance != null ? CutsceneController.Instance.Fader : null;

		public override void Apply() {
			var fader = Fader;
			if (fader == null)
				return;
			if (toBlack)
				fader.ShowInstant();
			else
				fader.HideInstant();
		}

		public override void Play(Action onComplete) {
			var fader = Fader;
			if (fader == null) {
				onComplete?.Invoke();
				return;
			}
			fader.FadeTo(toBlack ? 1f : 0f, duration, onComplete).SetId(TweenId);
		}

#if UNITY_EDITOR
		public void EditorInit(bool toBlack, float duration) {
			this.toBlack = toBlack;
			this.duration = duration;
		}
#endif
	}
}
