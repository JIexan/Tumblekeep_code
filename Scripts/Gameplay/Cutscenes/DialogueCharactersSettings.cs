using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Cutscenes {
	// Справочник сюжетных персонажей: сторона диалоговой панели и портреты по эмоциям.
	// Сторона глобальная (King слева, Jester справа во всех разговорах) — консистентность
	// важнее гибкости; при необходимости можно добавить оверрайд на конкретной катсцене.
	// Фолбэк портрета: нужная эмоция -> Neutral -> первый непустой спрайт.
	[CreateAssetMenu(fileName = "DialogueCharactersSettings", menuName = "Settings/DialogueCharactersSettings")]
	public class DialogueCharactersSettings : ScriptableObject {
		public enum PanelSide {
			Left = 0,
			Right = 1,
		}

		[System.Serializable]
		public class EmotionSprite {
			public EmotionId emotion;
			public Sprite sprite;
		}

		[System.Serializable]
		public class CharacterEntry {
			public CharacterId id;
			public PanelSide side;
			public List<EmotionSprite> emotions = new();
		}

		[SerializeField] List<CharacterEntry> characters = new();

		public List<CharacterEntry> Characters => characters;

		public string NameKey(CharacterId id) => "CharacterName_" + id;

		public PanelSide GetSide(CharacterId id) {
			var entry = GetEntry(id);
			return entry != null ? entry.side : PanelSide.Left;
		}

		public Sprite GetPortrait(CharacterId id, EmotionId emotion) {
			var entry = GetEntry(id);
			if (entry == null || entry.emotions == null || entry.emotions.Count == 0)
				return null;

			Sprite neutral = null;
			Sprite firstAny = null;
			foreach (var e in entry.emotions) {
				if (e == null || e.sprite == null)
					continue;
				if (e.emotion == emotion)
					return e.sprite;
				if (e.emotion == EmotionId.Neutral && neutral == null)
					neutral = e.sprite;
				if (firstAny == null)
					firstAny = e.sprite;
			}
			return neutral != null ? neutral : firstAny;
		}

		private CharacterEntry GetEntry(CharacterId id) {
			foreach (var c in characters) {
				if (c != null && c.id == id)
					return c;
			}
			return null;
		}

#if UNITY_EDITOR
		public void EditorSetCharacters(List<CharacterEntry> characters) {
			this.characters = characters ?? new List<CharacterEntry>();
		}

		private void OnValidate() {
			var seen = new HashSet<CharacterId>();
			foreach (var c in characters) {
				if (c == null)
					continue;
				if (c.id == CharacterId.None)
					Debug.LogWarning($"[DialogueCharactersSettings] '{name}': персонаж с id = None.", this);
				else if (!seen.Add(c.id))
					Debug.LogWarning($"[DialogueCharactersSettings] '{name}': дублирующийся персонаж '{c.id}'.", this);
			}
		}
#endif
	}
}
