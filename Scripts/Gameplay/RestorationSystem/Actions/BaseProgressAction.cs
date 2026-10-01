using System;
using UnityEngine;

namespace Gameplay.RestorationSystem.Actions {
	[System.Serializable]
	public abstract class BaseProgressAction {
		[Tooltip("Задержка перед запуском этого экшена в цепочке шага, сек (сдвиг относительно предыдущего).")]
		[SerializeField] protected float startDelay;

		// Stagger before this action fires within the step's chain (Play only; Apply is instant).
		public float StartDelay => startDelay;

		// Instantly set the final state (restoring saved progress, no animation).
		public abstract void Apply();

		// Play animated; invoke onComplete when finished.
		// Default = Apply + immediate onComplete (correct for instant actions).
		public virtual void Play(Action onComplete) {
			Apply();
			onComplete?.Invoke();
		}
	}
}
