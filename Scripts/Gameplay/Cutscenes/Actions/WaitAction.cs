using System;
using DG.Tweening;

namespace Gameplay.Cutscenes.Actions {
	// Пауза в цепочке (дать поклону доиграть, подержать кадр и т.п.).
	[System.Serializable]
	public class WaitAction : BaseCutsceneAction {
		[UnityEngine.SerializeField] float duration = 1f;

		public override void Apply() {
			// Нечего применять
		}

		public override void Play(Action onComplete) {
			DOVirtual.DelayedCall(duration, () => onComplete?.Invoke(), false).SetId(TweenId);
		}

#if UNITY_EDITOR
		public void EditorInit(float duration) {
			this.duration = duration;
		}
#endif
	}
}
