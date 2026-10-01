using System;
using System.Collections.Generic;
using Gameplay.Cutscenes;
using UnityEngine;

namespace Gameplay.Cutscenes.Actions {
	// Запускает диалог в DialoguePanel и блокирует цепочку, пока игрок его не дочитает.
	// Говорящий персонаж в мире подсвечивается talk-анимацией: по OnLineStarted
	// ищем CutsceneCharacter с нужным CharacterId среди участников катсцены.
	[System.Serializable]
	public class DialogueAction : BaseCutsceneAction {
		[SerializeField] List<DialogueLine> lines = new();
		[Tooltip("Персонажи в мире для talk-анимаций (опционально).")]
		[SerializeField] List<CutsceneCharacter> characters = new();

		public List<DialogueLine> Lines => lines;

		public override void Apply() {
			// Конечного состояния в мире нет; открытую панель закрывает CutsceneController при скипе
		}

		public override void Play(Action onComplete) {
			var panel = UI.Dialogue.DialoguePanel.Instance;
			if (panel == null || lines.Count == 0) {
				onComplete?.Invoke();
				return;
			}

			panel.OnLineStarted += OnLineStarted;
			panel.Play(owner != null ? owner.Id : "<?>", lines, () => {
				panel.OnLineStarted -= OnLineStarted;
				SetAllTalking(false);
				onComplete?.Invoke();
			});
		}

		private void OnLineStarted(DialogueLine line) {
			foreach (var c in characters) {
				if (c != null)
					c.SetTalking(c.Id == line.Character);
			}
		}

		private void SetAllTalking(bool value) {
			foreach (var c in characters) {
				if (c != null)
					c.SetTalking(value);
			}
		}

#if UNITY_EDITOR
		public void EditorInit(List<DialogueLine> lines, List<CutsceneCharacter> characters) {
			this.lines = lines ?? new List<DialogueLine>();
			this.characters = characters ?? new List<CutsceneCharacter>();
		}
#endif
	}
}
