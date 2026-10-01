using System;
using UnityEngine;

namespace Gameplay.Cutscenes.Actions {
	// Базовый экшен катсцены. В отличие от restoration-экшенов (staggered-цепочка
	// по задержкам) катсцена играет ПОСЛЕДОВАТЕЛЬНО: следующий экшен ждёт onComplete
	// предыдущего — длительность диалога определяет игрок, заранее её не знаем.
	// runWithPrevious = true запускает экшен параллельно предыдущему, не блокируя
	// цепочку («камера летит, пока персонаж идёт»).
	// Apply() обязателен: мгновенно конечное состояние — скип катсцены и
	// восстановление сейва (ShowCutsceneProgressAction.Apply) прыгают в конец.
	[System.Serializable]
	public abstract class BaseCutsceneAction {
		// Все твины катсцены помечаются этим id — Skip убивает их разом.
		public const string TweenId = "CutsceneTween";

		[Tooltip("Пауза перед запуском экшена, сек.")]
		[SerializeField] protected float startDelay;
		[Tooltip("Запустить одновременно с предыдущим экшеном, не блокируя цепочку.")]
		[SerializeField] protected bool runWithPrevious;

		public float StartDelay => startDelay;
		public bool RunWithPrevious => runWithPrevious;

		protected Cutscene owner;

		// Вызывается плеером перед запуском катсцены — экшенам нужен доступ
		// к катсцене (conversationId для диалога) без сериализованной обратной ссылки.
		public virtual void Init(Cutscene owner) {
			this.owner = owner;
		}

		// Мгновенно установить конечное состояние (скип, восстановление сейва).
		public abstract void Apply();

		// Проиграть с анимацией; по завершении вызвать onComplete.
		public virtual void Play(Action onComplete) {
			Apply();
			onComplete?.Invoke();
		}

#if UNITY_EDITOR
		public void EditorSetTiming(float startDelay, bool runWithPrevious) {
			this.startDelay = startDelay;
			this.runWithPrevious = runWithPrevious;
		}
#endif
	}
}
