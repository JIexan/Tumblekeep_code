using Gameplay.Unlocks;
using UnityEngine;

namespace Gameplay.RestorationSystem.Actions {
	// Мета-анлок: остров (Level.Id из LevelSettings) становится доступен на карте хаба.
	// Идемпотентно — Apply() гоняется при каждом входе в хаб для завершённых шагов.
	[System.Serializable]
	public class UnlockLevelAction : BaseProgressAction, IUnlockAction {
		[SerializeField] string levelId;

		public string LevelId => levelId;

		public override void Apply() {
			if (!Application.isPlaying) return;
			ContentUnlocksManager.UnlockLevel(levelId);
		}

#if UNITY_EDITOR
		public void EditorConfigure(string id, float chainDelay = 0f) {
			levelId = id;
			startDelay = chainDelay;
		}
#endif
	}
}
