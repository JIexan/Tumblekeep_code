using System.Collections.Generic;
using Gameplay.Cutscenes.Actions;
using UnityEngine;

namespace Gameplay.Cutscenes {
	// Катсцена: последовательность экшенов на сценном объекте (точки камеры,
	// персонажи и цели движения — дети этого объекта или ссылки на сцену).
	// id — человекочитаемый, идёт в ключи локализации реплик (Dialogue_{id}_{lineId}).
	// guid — save-идентичность (флаг «просмотрено»), переименование id её не трогает.
	public class Cutscene : MonoBehaviour, IDialogueLineOwner {
		[SerializeField] string id;
		[SerializeReference] List<BaseCutsceneAction> actions = new();
		[Tooltip("Проигрывать не более одного раза (флаг в сейве).")]
		[SerializeField] bool once = true;
		[Tooltip("Начинать с чёрного экрана: шторка встаёт мгновенно при запуске (для интро — ещё в CutsceneTrigger.Start, до первого кадра).")]
		[SerializeField] bool startFromBlack;

		[SerializeField, HideInInspector] string guid;

		public string Id => id;
		public string Guid => guid;
		public bool Once => once;
		public bool StartFromBlack => startFromBlack;
		public List<BaseCutsceneAction> Actions => actions;

		public bool WasWatched => SavesManager.Profile != null && SavesManager.Profile.tutorial.IsCutsceneWatched(guid);

		public string GetConversationId(string linePropertyPath) => id;

		public void Play(System.Action onComplete = null) {
			CutsceneController.Instance.Play(this, onComplete);
		}

		// Мгновенно применить конечное состояние всех экшенов (восстановление сейва).
		public void ApplyAll() {
			foreach (var action in actions) {
				if (action == null)
					continue;
				action.Init(this);
				action.Apply();
			}
		}

#if UNITY_EDITOR
		private void OnValidate() {
			if (string.IsNullOrEmpty(guid))
				guid = System.Guid.NewGuid().ToString("N");

			// Автозаполнение стабильных id реплик во всех DialogueAction
			foreach (var action in actions) {
				if (action is DialogueAction dialogue) {
					foreach (var line in dialogue.Lines)
						line?.EnsureLineId();
				}
			}
		}

		public void EditorInit(string id, List<BaseCutsceneAction> actions) {
			this.id = id;
			this.actions = actions ?? new List<BaseCutsceneAction>();
			if (string.IsNullOrEmpty(guid))
				guid = System.Guid.NewGuid().ToString("N");
		}

		public void EditorSetStartFromBlack(bool value) {
			startFromBlack = value;
		}
#endif
	}
}
