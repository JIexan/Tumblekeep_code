using Gameplay.IslandExperience;
using UnityEngine;

namespace Gameplay.LevelUpgrades {
	// Требование левела СВОЕГО острова: проверяется остров, которому принадлежит
	// конфиг апгрейдов (levelId приходит контекстом из LevelUpgradesManager).
	[System.Serializable]
	public class IslandLevelUpgradeRequirement : BaseUpgradeRequirement {
		[SerializeField] int minLevel = 2;

		public int MinLevel => minLevel;

		public override bool IsMet(string levelId) {
			return IslandExperienceManager.GetLevel(levelId) >= minLevel;
		}

		public override (int current, int required) GetProgress(string levelId) {
			return (IslandExperienceManager.GetLevel(levelId), minLevel);
		}

#if UNITY_EDITOR
		public void EditorInit(int minLevel) {
			this.minLevel = minLevel;
		}
#endif
	}
}
