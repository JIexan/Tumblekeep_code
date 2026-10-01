using UnityEngine;

namespace Gameplay.RestorationSystem.Actions {
	// Unlock another region by id, switching its visual from destroyed to in-progress.
	[System.Serializable]
	public class UnlockRegionAction : BaseProgressAction, IUnlockAction {
		[SerializeField] RegionId regionId;

		public override void Apply() {
			// Unlocking only makes sense at runtime; skip in edit-mode (e.g. inspector preview).
			if (!Application.isPlaying) return;
			PlayerProgressController.Instance?.UnlockRegion(regionId);
		}
	}
}
