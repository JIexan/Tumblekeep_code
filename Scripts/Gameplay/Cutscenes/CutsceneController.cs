using System;
using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Cutscenes.Actions;
using UnityEngine;

namespace Gameplay.Cutscenes {
	// Плеер катсцен. Живёт на CutsceneOverlay в UI-канвасе (собирается DialogueUiBuilder) —
	// оттуда же берёт letterbox и кнопку скипа. Владеет «конвертом» бесшовного перехода:
	// лок ввода игрока + сброс инерции камеры, скрытие HUD (только хаб), letterbox,
	// запоминание/возврат позиции камеры. RevertCamera() НЕ используется намеренно —
	// он завершает active=false и его orig-поля принадлежат боевому старту.
	// Экшены играют группами: экшен + следующие за ним с runWithPrevious; следующая
	// группа стартует, когда вся текущая отчиталась onComplete.
	public class CutsceneController : Singleton<CutsceneController> {
		[SerializeField] UI.Dialogue.LetterboxController letterbox;
		[SerializeField] GameObject skipButton;
		[SerializeField] UI.Dialogue.ScreenFader screenFader;
		[SerializeField] UnityEngine.UI.Image titleImage;
		[SerializeField] float letterboxTime = 0.4f;
		[SerializeField] float cameraReturnTime = 1f;

		public static bool IsPlaying { get; private set; }

		// Доступ для экшенов (ScreenFadeAction/ShowTitleAction): в бою оверлей живёт
		// в префабе UICanvas — сериализованная ссылка из сценной катсцены невозможна
		public UI.Dialogue.ScreenFader Fader => screenFader;
		public UnityEngine.UI.Image TitleImage => titleImage;

		private Cutscene current;
		private Action onFinished;
		private Vector3 savedCameraPos;
		private bool skipping;

		protected override void Awake() {
			base.Awake();
			if (skipButton != null) {
				skipButton.SetActive(false);
				var button = skipButton.GetComponent<UnityEngine.UI.Button>();
				if (button != null)
					button.onClick.AddListener(Skip);
			}
		}

		public void Play(Cutscene cutscene, Action onComplete = null) {
			if (cutscene == null) {
				onComplete?.Invoke();
				return;
			}
			if (IsPlaying) {
				Debug.LogWarning($"[CutsceneController] Катсцена '{cutscene.Id}' проигнорирована — уже играет '{current?.Id}'.");
				onComplete?.Invoke();
				return;
			}

			IsPlaying = true;
			skipping = false;
			current = cutscene;
			onFinished = onComplete;

			foreach (var action in cutscene.Actions)
				action?.Init(cutscene);

			// Вход: лочим ввод и прячем HUD до полёта камеры — бесшовность
			var camera = CameraController.Instance;
			camera.active = false;
			camera.ResetInertia();
			savedCameraPos = camera.cameraContainer.position;

			SetHubHudHidden(true);
			// «С чёрного экрана»: шторка могла встать ещё в CutsceneTrigger.Start — идемпотентно
			if (cutscene.StartFromBlack && screenFader != null)
				screenFader.ShowInstant();
			letterbox.Show(letterboxTime);
			if (skipButton != null)
				skipButton.SetActive(true);

			PlayGroup(0);
		}

		public void Skip() {
			if (!IsPlaying || skipping)
				return;
			skipping = true;

			// Убить твины БЕЗ complete: OnComplete/отложенные Play не должны сработать
			DOTween.Kill(BaseCutsceneAction.TweenId, false);
			UI.Dialogue.DialoguePanel.Instance?.CompleteImmediately();

			// Конечное состояние всех экшенов по порядку — тот же контракт,
			// что у восстановления из сейва
			current.ApplyAll();

			ExitEnvelope(0.25f, 0.5f);
		}

		private void PlayGroup(int startIndex) {
			if (skipping)
				return;
			if (current == null || startIndex >= current.Actions.Count) {
				ExitEnvelope(letterboxTime, cameraReturnTime);
				return;
			}

			var actions = current.Actions;
			// Группа: экшен + следующие с runWithPrevious
			int endIndex = startIndex + 1;
			while (endIndex < actions.Count && actions[endIndex] != null && actions[endIndex].RunWithPrevious)
				endIndex++;

			int remaining = endIndex - startIndex;
			int nextIndex = endIndex;

			void OnOneDone() {
				remaining--;
				if (remaining == 0 && !skipping)
					PlayGroup(nextIndex);
			}

			for (int i = startIndex; i < endIndex; i++) {
				var action = actions[i];
				if (action == null) {
					OnOneDone();
					continue;
				}

				if (action.StartDelay > 0f)
					DOVirtual.DelayedCall(action.StartDelay, () => action.Play(OnOneDone), false)
						.SetId(BaseCutsceneAction.TweenId);
				else
					action.Play(OnOneDone);
			}
		}

		// Выход: полосы прячутся, камера возвращается в точку, откуда игрок ушёл,
		// и только потом отдаём управление
		private void ExitEnvelope(float lbTime, float camTime) {
			if (skipButton != null)
				skipButton.SetActive(false);
			letterbox.Hide(lbTime);

			var camera = CameraController.Instance;
			camera.cameraContainer.DOMove(savedCameraPos, camTime).OnComplete(FinishCutscene);
		}

		private void FinishCutscene() {
			CameraController.Instance.active = true;
			SetHubHudHidden(false);
			// Страховка: катсцена не обязана сама убирать шторку и заголовок
			if (screenFader != null)
				screenFader.HideInstant();
			if (titleImage != null)
				titleImage.gameObject.SetActive(false);

			var finished = current;
			var callback = onFinished;
			current = null;
			onFinished = null;
			IsPlaying = false;
			skipping = false;

			if (finished != null && SavesManager.Profile != null) {
				SavesManager.Profile.tutorial.MarkCutsceneWatched(finished.Guid);
				SavesManager.Instance.SaveProfile();
			}

			callback?.Invoke();
		}

		// HUD прячем только в хабе (реюз оконного ref-count'а — катсцена и окно
		// не дерутся за HUD). В бою фасад другой и HUD на время катсцены не трогаем.
		private void SetHubHudHidden(bool hidden) {
			var hub = FindObjectOfType<PlayerLocationUIController>();
			if (hub == null)
				return;
			if (hidden)
				hub.HideHudForCutscene();
			else
				hub.RestoreHudAfterCutscene();
		}
	}
}
