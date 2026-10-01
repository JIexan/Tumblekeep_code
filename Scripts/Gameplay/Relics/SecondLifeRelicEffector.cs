using UnityEngine;

namespace Gameplay.Relics {
	// «Перо феникса»: раз за катку смертельный удар по замку гасится, замок восстанавливается
	// до restorePercent от максимума. Пассивный маркер — потребляется
	// RelicsManager.TryConsumeSecondLife из Castle.Damage.
	[System.Serializable]
	public class SecondLifeRelicEffector : BaseRelicEffector {
		[SerializeField] [Range(0f, 1f)] float restorePercent = 0.3f;

		public float RestorePercent => restorePercent;

#if UNITY_EDITOR
		public void EditorInit(float restorePercent) {
			this.restorePercent = restorePercent;
		}
#endif
	}
}
