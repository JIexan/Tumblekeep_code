using DG.Tweening;
using UnityEngine;

namespace Gameplay.Cutscenes {
	// Автозапуск катсцены при входе в сцену (после загрузки профиля).
	// Просмотренные (для once-катсцен) пропускаются; флаг пишет CutsceneController.
	public class CutsceneTrigger : MonoBehaviour {
		[SerializeField] Cutscene cutscene;
		[Tooltip("Задержка после загрузки профиля, сек — дать сцене достроиться.")]
		[SerializeField] float delay = 0.5f;

		private bool triggered;

		private void Start() {
			SavesManager.Subscribe(OnProfileLoaded);
		}

		private void OnDestroy() {
			SavesManager.Unsubscribe(OnProfileLoaded);
		}

		private void OnProfileLoaded(object _) {
			if (triggered || cutscene == null)
				return;
			if (cutscene.Once && cutscene.WasWatched)
				return;

			triggered = true;

			// «С чёрного экрана»: шторка встаёт синхронно (Start/replay идут до
			// первого отрисованного кадра) — игрок не увидит хаб до катсцены
			if (cutscene.StartFromBlack) {
				var controller = CutsceneController.Instance;
				if (controller != null && controller.Fader != null)
					controller.Fader.ShowInstant();
			}

			DOVirtual.DelayedCall(delay, () => {
				if (this != null && cutscene != null)
					cutscene.Play();
			}, false);
		}
	}
}
