using System.Collections.Generic;

// Мета-анлоки контента, полученные через восстановление хаба (Unlock*Action шагов).
// Хранятся ТОЛЬКО добавочные открытия: стартовый набор задаёт ContentUnlocksSettings,
// итог = дефолты ∪ сейв (см. ContentUnlocksManager). Enum'ы пишутся именами (конвенция
// сейва, как RegionId) — переименовывать значения ItemType/FactionId/UnitTier нельзя.
[System.Serializable]
public class SaveDataUnlocks : SaveDataBase {
	public List<string> relicIds = new List<string>();
	public List<string> spellIds = new List<string>();
	public List<string> factionIds = new List<string>();
	// Открытые острова (Level.Id)
	public List<string> levelIds = new List<string>();
	// Максимальный открытый ярус юнитов как int (UnitTier), 0 = T1; итог = max(default, saved)
	public int maxTier = 0;
	// Чит «открыть всё» — не трогает списки, чтобы сброс чита возвращал честное состояние
	public bool unlockAll;

	public override void CheckValues() {
		base.CheckValues();
		relicIds ??= new List<string>();
		spellIds ??= new List<string>();
		factionIds ??= new List<string>();
		levelIds ??= new List<string>();
		if (maxTier < 0)
			maxTier = 0;
	}

	public bool HasRelic(string id) => !string.IsNullOrEmpty(id) && relicIds.Contains(id);
	public bool HasSpell(ItemType type) => spellIds.Contains(type.ToString());
	public bool HasFaction(FactionId id) => factionIds.Contains(id.ToString());
	public bool HasLevel(string levelId) => !string.IsNullOrEmpty(levelId) && levelIds.Contains(levelId);

	// Все Add* идемпотентны: Apply() шагов гоняется при каждом входе в хаб. true — реально изменилось
	public bool AddRelic(string id) => AddUnique(relicIds, id);
	public bool AddSpell(ItemType type) => AddUnique(spellIds, type.ToString());
	public bool AddFaction(FactionId id) => AddUnique(factionIds, id.ToString());
	public bool AddLevel(string levelId) => AddUnique(levelIds, levelId);

	public bool RaiseTier(UnitTier tier) {
		if ((int)tier <= maxTier)
			return false;
		maxTier = (int)tier;
		return true;
	}

	public void Clear() {
		relicIds.Clear();
		spellIds.Clear();
		factionIds.Clear();
		levelIds.Clear();
		maxTier = 0;
		unlockAll = false;
	}

	private static bool AddUnique(List<string> list, string value) {
		if (string.IsNullOrEmpty(value) || list.Contains(value))
			return false;
		list.Add(value);
		return true;
	}
}
