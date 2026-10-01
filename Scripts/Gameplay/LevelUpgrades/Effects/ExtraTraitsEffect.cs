using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.LevelUpgrades {
	// Добавляет трейты в рандомный пул апгрейдов башен, чей тип попадает в маску.
	[System.Serializable]
	public class ExtraTraitsEffect : BaseUpgradeEffect {
		[SerializeField] TowerType towerTypes;
		[SerializeField] List<TowerTraitType> traits = new();

		public TowerType TowerTypes => towerTypes;
		public List<TowerTraitType> Traits => traits;

		public bool AppliesTo(TowerType type) {
			return (towerTypes & type) == type;
		}

#if UNITY_EDITOR
		public void EditorInit(TowerType towerTypes, List<TowerTraitType> traits) {
			this.towerTypes = towerTypes;
			this.traits = traits ?? new List<TowerTraitType>();
		}
#endif
	}
}
