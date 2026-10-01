using System;
using UnityEngine;

namespace Gameplay.Cutscenes.Actions {
	// Дёрнуть произвольный триггер аниматора персонажа (поклон, смех и т.п.).
	// Завершается сразу — длительность клипа цепочку не блокирует
	// (нужна пауза — ставь следом WaitAction).
	[System.Serializable]
	public class CharacterAnimAction : BaseCutsceneAction {
		[SerializeField] CutsceneCharacter character;
		[SerializeField] string trigger;

		public override void Apply() {
			// Разовая анимация конечное состояние не меняет
		}

		public override void Play(Action onComplete) {
			if (character != null && !string.IsNullOrEmpty(trigger))
				character.SetTrigger(trigger);
			onComplete?.Invoke();
		}
	}
}
