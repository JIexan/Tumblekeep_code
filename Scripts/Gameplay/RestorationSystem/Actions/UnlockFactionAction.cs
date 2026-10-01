using Gameplay.Unlocks;
using UnityEngine;

namespace Gameplay.RestorationSystem.Actions {
	// Мета-анлок: фракция врагов попадает в мешок WavesGenerator (infinite-волны) во всех катках.
	// Идемпотентно — Apply() гоняется при каждом входе в хаб для завершённых шагов.
	[System.Serializable]
	public class UnlockFactionAction : BaseProgressAction, IUnlockAction {
		[SerializeField] FactionId factionId;

		public FactionId FactionId => factionId;

		public override void Apply() {
			if (!Application.isPlaying) return;
			ContentUnlocksManager.UnlockFaction(factionId);
		}

#if UNITY_EDITOR
		public void EditorConfigure(FactionId id, float chainDelay = 0f) {
			factionId = id;
			startDelay = chainDelay;
		}
#endif
	}
}
