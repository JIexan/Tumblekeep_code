using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Gameplay.Unlocks {
	// Очередь анонсов «контент открыт»: Unlock*Action при реальной записи в профиль (Play шага) кладут
	// анлок сюда, а ProgressStep после завершения всех экшенов (включая катсцену) показывает окна
	// ContentUnlockWindow по одному. Вне хаба (нет PlayerLocationUIController) очередь просто очищается.
	public static class ContentUnlockAnnouncer {
		private static readonly List<ContentUnlock> pending = new();
		private static bool showing;

		public static int PendingCount => pending.Count;

		public static void Enqueue(ContentUnlock unlock) {
			if (!Application.isPlaying)
				return;
			foreach (var p in pending) {
				if (p.Kind == unlock.Kind && p.Key == unlock.Key)
					return;
			}
			pending.Add(unlock);
		}

		public static void Clear() {
			pending.Clear();
		}

		// Показать всё накопленное, окна по очереди; onDone — когда закрыто последнее (или показывать нечего)
		public static void ShowPending(Action onDone = null) {
			if (showing || pending.Count == 0) {
				onDone?.Invoke();
				return;
			}
			var hub = UnityEngine.Object.FindObjectOfType<PlayerLocationUIController>();
			if (hub == null) {
				pending.Clear();
				onDone?.Invoke();
				return;
			}

			showing = true;
			ShowNext(hub, onDone);
		}

		private static void ShowNext(PlayerLocationUIController hub, Action onDone) {
			if (pending.Count == 0 || hub == null) {
				showing = false;
				onDone?.Invoke();
				return;
			}
			ShowNextAsync(hub, onDone).Forget();
		}

		// Окно одно на все анонсы: onAfterHide приходит в начале анимации закрытия, а её событие
		// DeactivateWindow гасит объект. Если открыть следующий анонс сразу, он погаснет вместе с ним
		// без onAfterHide — ref-count HUD в PlayerLocationUIController зависает и HUD не возвращается.
		// Поэтому ждём, пока инстанс окна реально выключится.
		private static async UniTaskVoid ShowNextAsync(PlayerLocationUIController hub, Action onDone) {
			var manager = UnityEngine.Object.FindObjectOfType<WindowsManager>();
			var window = manager != null ? manager.GetWindow(WindowType.ContentUnlockWindow) : null;
			if (window != null && window.gameObject.activeSelf)
				await UniTask.WaitUntil(() => window == null || !window.gameObject.activeSelf);
			if (hub == null || pending.Count == 0) {
				showing = false;
				onDone?.Invoke();
				return;
			}
			var unlock = pending[0];
			pending.RemoveAt(0);
			hub.ShowWindow(WindowType.ContentUnlockWindow, unlock, null, () => ShowNext(hub, onDone));
		}
	}
}
