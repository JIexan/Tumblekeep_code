using System;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.Cutscenes.Actions {
	// Прибытие корабля: гасит «течение» конвейера (плавно — корабль как бы набирает
	// ход), переносит корабль и камеру на подходную точку и ведёт к причалу.
	// Телепорт невидим: вектор переноса квантуется по решётке тайлов воды —
	// идентичные тайлы встают ровно в позиции соседей и фаза воды в кадре
	// сохраняется точно; world-space пена переносится вместе с кораблём (TeleportTo);
	// approachStart подобран так, чтобы остров был вне кадра. Камера всю дорогу
	// держит корабль (то же мировое смещение, что и до телепорта).
	[System.Serializable]
	public class ShipArriveAction : BaseCutsceneAction {
		[SerializeField] CutsceneShip ship;
		[Tooltip("Точка в море, откуда начинается реальный подход к острову (фактический старт — ближайшая решёточно-кратная позиция).")]
		[SerializeField] Transform approachStart;
		[Tooltip("Точка швартовки у маяка; forward = курс подхода.")]
		[SerializeField] Transform dockPoint;
		[SerializeField] float speed = 25f;
		[Tooltip("За сколько секунд затухает течение воды после старта подхода.")]
		[SerializeField] float driftFadeTime = 1.5f;

		public override void Apply() {
			if (ship == null || dockPoint == null)
				return;
			ship.StopSailLoop();
			ship.SetFoam(false);
			ship.transform.SetPositionAndRotation(dockPoint.position, dockPoint.rotation);
		}

		public override void Play(Action onComplete) {
			if (ship == null || approachStart == null || dockPoint == null) {
				Apply();
				onComplete?.Invoke();
				return;
			}

			// Течение затухает параллельно набору хода — вода «останавливается»,
			// когда корабль начинает двигаться по-настоящему
			ship.StopSailLoop(driftFadeTime);

			var camera = CameraController.Instance != null ? CameraController.Instance.cameraContainer : null;

			// Квантование вектора телепорта по решётке воды: фаза тайлов в кадре не меняется
			Vector3 delta = approachStart.position - ship.transform.position;
			delta.y = 0f;
			float step = ship.WaterTileStep;
			if (step > 0.01f) {
				delta.x = Mathf.Round(delta.x / step) * step;
				delta.z = Mathf.Round(delta.z / step) * step;
			}
			Vector3 startPos = ship.transform.position + delta;

			Vector3 heading = dockPoint.position - startPos;
			heading.y = 0f;
			var rotation = heading.sqrMagnitude > 0.001f ? Quaternion.LookRotation(heading) : dockPoint.rotation;
			ship.TeleportTo(startPos, rotation);
			if (camera != null)
				camera.position += delta;

			Vector3 camOffset = camera != null ? camera.position - ship.transform.position : Vector3.zero;
			float duration = Mathf.Max(0.1f, heading.magnitude / Mathf.Max(0.1f, speed));
			ship.SetFoam(true);

			ship.transform.DOMove(dockPoint.position, duration)
				.SetEase(Ease.OutSine)
				.SetId(TweenId)
				.OnUpdate(() => {
					if (camera != null)
						camera.position = ship.transform.position + camOffset;
				})
				.OnComplete(() => {
					ship.SetFoam(false);
					onComplete?.Invoke();
				});
		}

#if UNITY_EDITOR
		public void EditorInit(CutsceneShip ship, Transform approachStart, Transform dockPoint, float speed) {
			this.ship = ship;
			this.approachStart = approachStart;
			this.dockPoint = dockPoint;
			this.speed = speed;
		}
#endif
	}
}
