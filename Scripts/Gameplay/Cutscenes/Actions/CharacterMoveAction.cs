using System;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.Cutscenes.Actions {
	// Персонаж идёт к точке по прямой (NavMesh в проекте нет), опционально через
	// промежуточные вейпоинты. Скорость постоянная, на каждом отрезке персонаж
	// мгновенно поворачивается к следующей точке; walk-анимация на время движения.
	[System.Serializable]
	public class CharacterMoveAction : BaseCutsceneAction {
		[SerializeField] CutsceneCharacter character;
		[SerializeField] System.Collections.Generic.List<Transform> waypoints = new();
		[SerializeField] Transform target;
		[SerializeField] float speed = 2f;

		public override void Apply() {
			if (character == null || target == null)
				return;
			character.SetWalking(false);
			character.FaceInstant(target.position);
			character.transform.position = target.position;
		}

		public override void Play(Action onComplete) {
			if (character == null || target == null) {
				onComplete?.Invoke();
				return;
			}

			var points = new System.Collections.Generic.List<Vector3>();
			foreach (var w in waypoints) {
				if (w != null)
					points.Add(w.position);
			}
			points.Add(target.position);

			character.SetWalking(true);
			var sequence = DOTween.Sequence().SetId(TweenId);
			Vector3 from = character.transform.position;
			foreach (var p in points) {
				Vector3 point = p;
				float duration = Vector3.Distance(from, point) / Mathf.Max(0.01f, speed);
				sequence.AppendCallback(() => character.FaceInstant(point));
				sequence.Append(character.transform.DOMove(point, duration).SetEase(Ease.Linear));
				from = point;
			}
			sequence.OnComplete(() => {
				character.SetWalking(false);
				onComplete?.Invoke();
			});
		}

#if UNITY_EDITOR
		public void EditorInit(CutsceneCharacter character, Transform target, float speed) {
			this.character = character;
			this.target = target;
			this.speed = speed;
		}
#endif
	}
}
