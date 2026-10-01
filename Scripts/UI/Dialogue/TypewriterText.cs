using System;
using TMPro;
using UnityEngine;

namespace UI.Dialogue {
	// Побуквенное появление текста через TMP maxVisibleCharacters.
	// Unscaled time обязателен: окна/катсцены могут останавливать timeScale,
	// а диалог должен печататься всегда.
	public class TypewriterText : MonoBehaviour {
		[SerializeField] TMP_Text text;
		[Tooltip("Скорость печати, символов в секунду.")]
		[SerializeField] float charsPerSecond = 40f;

		private float visibleCount;
		private int totalCount;
		private bool revealing;
		private Action onFullyShown;

		public bool IsRevealing => revealing;

		public void Play(string line, Action onFullyShown = null) {
			this.onFullyShown = onFullyShown;
			text.text = line;
			text.ForceMeshUpdate();
			totalCount = text.textInfo.characterCount;
			visibleCount = 0f;
			text.maxVisibleCharacters = 0;
			revealing = totalCount > 0;
			if (!revealing)
				Finish();
		}

		public void RevealAll() {
			if (!revealing)
				return;
			text.maxVisibleCharacters = totalCount;
			Finish();
		}

		public void Clear() {
			revealing = false;
			onFullyShown = null;
			text.text = string.Empty;
		}

		private void Update() {
			if (!revealing)
				return;

			visibleCount += charsPerSecond * Time.unscaledDeltaTime;
			int visible = Mathf.Min(totalCount, Mathf.FloorToInt(visibleCount));
			text.maxVisibleCharacters = visible;
			if (visible >= totalCount)
				Finish();
		}

		private void Finish() {
			revealing = false;
			var callback = onFullyShown;
			onFullyShown = null;
			callback?.Invoke();
		}
	}
}
