using System;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.Cutscenes.Actions {
	// Камера подъезжает к точке с заданным зумом. Углы не меняются —
	// используется штатный dolly-риг CameraController (движение вдоль forward).
	[System.Serializable]
	public class CameraMoveAction : BaseCutsceneAction {
		[Tooltip("Точка на земле, на которую смотрим.")]
		[SerializeField] Transform point;
		[Tooltip("Зум: 0 — максимально близко, 1 — максимально далеко (высота в границах borderWorld).")]
		[SerializeField, Range(0f, 1f)] float zoomLevel = 0.3f;
		[SerializeField] float time = 1.5f;
		[Tooltip("Не ограничивать точку границами borderWorld (полёты над открытым морем).")]
		[SerializeField] bool ignoreBounds;

		public override void Apply() {
			if (point != null)
				CameraController.Instance.JumpToPosition(point.position, zoomLevel, !ignoreBounds);
		}

		public override void Play(Action onComplete) {
			if (point == null) {
				onComplete?.Invoke();
				return;
			}
			// time <= 0 — мгновенный прыжок (постановка кадра под чёрным экраном)
			if (time <= 0f) {
				CameraController.Instance.JumpToPosition(point.position, zoomLevel, !ignoreBounds);
				onComplete?.Invoke();
				return;
			}
			CameraController.Instance.AnimateToPosition(point.position, zoomLevel, time, onComplete, !ignoreBounds).SetId(TweenId);
		}

#if UNITY_EDITOR
		public void EditorInit(Transform point, float zoomLevel, float time, bool ignoreBounds = false) {
			this.point = point;
			this.zoomLevel = zoomLevel;
			this.time = time;
			this.ignoreBounds = ignoreBounds;
		}
#endif
	}
}
