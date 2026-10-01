using Gameplay.Unlocks;
using UnityEngine;

namespace Gameplay.RestorationSystem.Actions {
	// Мета-анлок: поднять максимальный ярус юнитов (WavesGenerator.IsTierUnlocked). Только вверх —
	// более низкий ярус ничего не меняет. Идемпотентно.
	[System.Serializable]
	public class UnlockUnitTierAction : BaseProgressAction, IUnlockAction {
		[SerializeField] UnitTier tier = UnitTier.T3;

		public UnitTier Tier => tier;

		public override void Apply() {
			if (!Application.isPlaying) return;
			ContentUnlocksManager.UnlockTier(tier);
		}

#if UNITY_EDITOR
		public void EditorConfigure(UnitTier newTier, float chainDelay = 0f) {
			tier = newTier;
			startDelay = chainDelay;
		}
#endif
	}
}
