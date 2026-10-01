using System;
using UnityEngine;

namespace Gameplay.RestorationSystem.Actions {
	// Катсцена как экшен шага восстановления: играет после оплаты шага.
	// Apply (восстановление сейва) мгновенно применяет конечное состояние
	// экшенов катсцены — тот же контракт, что у остальных progress-экшенов.
	[System.Serializable]
	public class ShowCutsceneProgressAction : BaseProgressAction {
		[SerializeField] Gameplay.Cutscenes.Cutscene cutscene;

		public override void Apply() {
			if (cutscene != null)
				cutscene.ApplyAll();
		}

		public override void Play(Action onComplete) {
			if (cutscene == null) {
				onComplete?.Invoke();
				return;
			}
			Gameplay.Cutscenes.CutsceneController.Instance.Play(cutscene, onComplete);
		}
	}
}
