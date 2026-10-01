using UnityEngine;

namespace Gameplay.LevelUpgrades {
	// Открывает виды башен в панели постройки; TowerType — битовые флаги,
	// один эффект может открыть сразу несколько видов.
	[System.Serializable]
	public class UnlockTowerTypeEffect : BaseUpgradeEffect {
		[SerializeField] TowerType towerTypes;

		public TowerType TowerTypes => towerTypes;

		public bool Unlocks(TowerType type) {
			return (towerTypes & type) == type;
		}

#if UNITY_EDITOR
		public void EditorInit(TowerType towerTypes) {
			this.towerTypes = towerTypes;
		}
#endif
	}
}
