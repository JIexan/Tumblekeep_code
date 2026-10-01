using System.Collections.Generic;

// Опыт островов (игровых уровней), ключ — Level.Id. Хранится только суммарный XP:
// левел вычисляется из кривой IslandExperienceSettings, ребаланс кривой применяется
// ретроактивно без миграций.
[System.Serializable]
public class SaveDataIslandExperience : SaveDataBase {
	public Dictionary<string, int> xpByLevel = new Dictionary<string, int>();

	public override void CheckValues() {
		base.CheckValues();
		if (xpByLevel == null)
			xpByLevel = new Dictionary<string, int>();
	}

	public int GetTotalXP(string levelId) {
		if (string.IsNullOrEmpty(levelId))
			return 0;
		return xpByLevel.TryGetValue(levelId, out int xp) ? xp : 0;
	}

	public void AddXP(string levelId, int amount) {
		if (string.IsNullOrEmpty(levelId) || amount <= 0)
			return;
		xpByLevel[levelId] = GetTotalXP(levelId) + amount;
	}
}
