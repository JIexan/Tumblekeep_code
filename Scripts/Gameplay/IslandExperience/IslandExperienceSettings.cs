using UnityEngine;

namespace Gameplay.IslandExperience {
	// Кривая опыта островов: сколько XP нужно на каждый левелап (XPFormulaConfig —
	// общий с опытом башен) и потолок левела.
	[CreateAssetMenu(fileName = "IslandExperienceSettings", menuName = "Settings/IslandExperienceSettings")]
	public class IslandExperienceSettings : ScriptableObject {
		[SerializeField] XPFormulaConfig xpFormula = new XPFormulaConfig();
		[SerializeField] int maxLevel = 10;

		public int MaxLevel => maxLevel;

		// Стоимость перехода НА targetLevel (уровень 1 бесплатен).
		public int GetXPForLevel(int targetLevel) {
			return xpFormula.CalculateXPRequirement(targetLevel);
		}

		// Суммарный XP, необходимый чтобы ИМЕТЬ указанный левел.
		public int GetTotalXPForLevel(int level) {
			int total = 0;
			int capped = Mathf.Min(level, maxLevel);
			for (int lvl = 2; lvl <= capped; lvl++) {
				total += GetXPForLevel(lvl);
			}
			return total;
		}

		// Всё O(maxLevel) без кэша: потолок мал, потребители — читы/редкие проверки.
		// Появится per-frame UI — добавить [NonSerialized] кумулятивный кэш.
		public int GetLevelFromTotalXP(int totalXP) {
			int level = 1;
			int cumulative = 0;
			while (level < maxLevel) {
				cumulative += GetXPForLevel(level + 1);
				if (totalXP < cumulative)
					break;
				level++;
			}
			return level;
		}

		public float GetProgressToNextLevel(int totalXP) {
			int level = GetLevelFromTotalXP(totalXP);
			if (level >= maxLevel)
				return 1f;

			int nextLevelCost = GetXPForLevel(level + 1);
			if (nextLevelCost <= 0)
				return 1f;

			int intoCurrentLevel = totalXP - GetTotalXPForLevel(level);
			return Mathf.Clamp01(intoCurrentLevel / (float)nextLevelCost);
		}

#if UNITY_EDITOR
		private void OnValidate() {
			if (maxLevel < 1)
				maxLevel = 1;
		}
#endif
	}
}
