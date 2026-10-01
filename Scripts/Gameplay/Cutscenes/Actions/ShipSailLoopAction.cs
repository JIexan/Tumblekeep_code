using System;
using UnityEngine;

namespace Gameplay.Cutscenes.Actions {
	// Запускает «вечное плаванье» (см. CutsceneShip.StartSailLoop: корабль стоит,
	// течение изображают вода и пена) и сразу отдаёт onComplete — не блокирует
	// цепочку: диалог играет, пока корабль «плывёт». Останавливает конвейер
	// ShipArriveAction (или Skip убивает твины по TweenId + Apply).
	[System.Serializable]
	public class ShipSailLoopAction : BaseCutsceneAction {
		[SerializeField] CutsceneShip ship;
		[Tooltip("Стартовая точка и курс корабля (телепорт перед запуском; пусто — плыть откуда стоит).")]
		[SerializeField] Transform startPoint;
		[SerializeField] float speed = 6f;

		public override void Apply() {
			// Конечное состояние «плаванья в никуда» — конвейер остановлен; позицию
			// корабля задают последующие экшены (ShipArriveAction.Apply)
			if (ship != null)
				ship.StopSailLoop();
		}

		public override void Play(Action onComplete) {
			if (ship == null) {
				onComplete?.Invoke();
				return;
			}
			if (startPoint != null)
				ship.transform.SetPositionAndRotation(startPoint.position, startPoint.rotation);
			ship.StartSailLoop(speed);
			onComplete?.Invoke();
		}

#if UNITY_EDITOR
		public void EditorInit(CutsceneShip ship, Transform startPoint, float speed) {
			this.ship = ship;
			this.startPoint = startPoint;
			this.speed = speed;
		}
#endif
	}
}
