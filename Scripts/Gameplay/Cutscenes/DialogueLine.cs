using UnityEngine;

namespace Gameplay.Cutscenes {
	// Одна реплика диалога: кто говорит, с какой эмоцией и какой текст.
	// Текст в сейве/ассете не хранится — только стабильный lineId, из которого
	// вычисляется ключ локализации Dialogue_{conversationId}_{lineId}. lineId —
	// guid-фрагмент, а не индекс: вставка/перестановка реплик не ломает переводы.
	// draftRu — черновик текста для автора и TSV-экспорта переводчикам, в рантайме не читается.
	[System.Serializable]
	public class DialogueLine {
		[SerializeField] CharacterId character = CharacterId.King;
		[SerializeField] EmotionId emotion = EmotionId.Neutral;
		[SerializeField] string lineId;
		[SerializeField, TextArea(1, 3)] string draftRu;

		public CharacterId Character => character;
		public EmotionId Emotion => emotion;
		public string LineId => lineId;
		public string DraftRu => draftRu;

		public string LocalizationKey(string conversationId) => $"Dialogue_{conversationId}_{lineId}";

		// Вызывается из OnValidate владельца (Cutscene/TutorialManager) — сам класс не MonoBehaviour.
		public void EnsureLineId() {
			if (string.IsNullOrEmpty(lineId))
				lineId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
		}

		public void RegenerateLineId() {
			lineId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
		}

#if UNITY_EDITOR
		public void EditorInit(CharacterId character, EmotionId emotion, string lineId, string draftRu) {
			this.character = character;
			this.emotion = emotion;
			this.lineId = lineId;
			this.draftRu = draftRu;
		}
#endif
	}
}
