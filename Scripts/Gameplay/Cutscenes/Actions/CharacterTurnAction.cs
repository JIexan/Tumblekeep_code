using System;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.Cutscenes.Actions {
	// Персонаж поворачивается лицом к цели (только по Y).
	[System.Serializable]
	public class CharacterTurnAction : BaseCutsceneAction {
		[SerializeField] CutsceneCharacter character;
		[SerializeField] Transform faceTarget;
		[SerializeField] float time = 0.4f;

		public override void Apply() {
			if (character != null && faceTarget != null)
				character.FaceInstant(faceTarget.position);
		}

		public override void Play(Action onComplete) {
			if (character == null || faceTarget == null) {
				onComplete?.Invoke();
				return;
			}

			Vector3 direction = faceTarget.position - character.transform.position;
			direction.y = 0f;
			if (direction.sqrMagnitude < 0.0001f) {
				onComplete?.Invoke();
				return;
			}

			var targetRotation = Quaternion.LookRotation(direction).eulerAngles;
			character.transform.DORotate(targetRotation, time).SetId(TweenId)
				.OnComplete(() => onComplete?.Invoke());
		}

#if UNITY_EDITOR
		public void EditorInit(CutsceneCharacter character, Transform faceTarget, float time) {
			this.character = character;
			this.faceTarget = faceTarget;
			this.time = time;
		}
#endif
	}
}
