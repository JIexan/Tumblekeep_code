using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using UnityEngine;

// Конвертер, который позволяет сохранять в файл текущую дату вместо значения переменной
class SaveDateTimeConverter : IsoDateTimeConverter {
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
		base.WriteJson(writer, DateTime.UtcNow, serializer);
	}
}

public enum ItemType {
	Crystal = 1,
	Star = 2,

	//спеллы начинаются с 50
	SpellFreeze = 50,
	SpellFireball,
	SpellLightning,
	SpellProtect,
	SpellHeal,
	SpellMoney,
	SpellExplode,
	SpellSpeedUp,
	SpellPoison,
	SpellPoisonCloud
}

[System.Serializable]
public class SaveDataProfile : SaveDataBase {
	// Текущая версия сейва. Профиль без версии или с версией меньше текущей при загрузке сбрасывается
	// (SavesManager.LoadProfile) — поднимать, когда тестеры демо должны начать с нуля и пройти весь туториал
	public const int CURRENT_VERSION = 2;

	// Populate: если поля в файле нет, десериализатор ставит 0, а не оставляет значение инициализатора
	[System.ComponentModel.DefaultValue(0)]
	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
	public int version = CURRENT_VERSION;

	[JsonConverter(typeof(SaveDateTimeConverter))]
	public DateTime saveDate = DateTime.UtcNow; // Время создания сейва

	public string playerName = "-";

	// Валюты и прочие постоянные предметы. Свитки (спеллы) сюда НЕ попадают — см. sessionSpells
	public Dictionary<ItemType, ObscuredInt> items;

	// Свитки живут только внутри катки: не сериализуются, теряются при смене сцены / загрузке профиля.
	// В профиле остаются только анлоки уровня (типы башен, трейты), а не расходники
	[JsonIgnore] private Dictionary<ItemType, int> sessionSpells = new Dictionary<ItemType, int>();

	// Порядок получения свитков — в нём HUD показывает спеллы сверху вниз (SpellsPanelController).
	// Спелл попадает в список, когда его количество становится > 0, и уходит, когда падает до 0
	[JsonIgnore] public List<ItemType> spellOrder = new List<ItemType>();

	public SaveDataAchievements achievements = new SaveDataAchievements();
	public SaveDataAnalitics analitics = new SaveDataAnalitics();
    public SaveDataDailyReward dailyReward = new SaveDataDailyReward();
	public SaveDataBookViewed saveDataBookViewed = new SaveDataBookViewed();
	public SaveDataTechnologyTree technologyTree = new SaveDataTechnologyTree();
	public SaveDataPlayerLocation playerLocation = new SaveDataPlayerLocation();
	public SaveDataLevelUpgrades levelUpgrades = new SaveDataLevelUpgrades();
	public SaveDataIslandExperience islandExperience = new SaveDataIslandExperience();
	public SaveDataTutorial tutorial = new SaveDataTutorial();
	// Мета-анлоки контента через хаб (реликвии/свитки/фракции/ярусы) — см. ContentUnlocksManager
	public SaveDataUnlocks unlocks = new SaveDataUnlocks();

	public SaveSettings settings = new SaveSettings();
	public Dictionary<string, SaveDataLevel> levels = null;


    public override void CheckValues() {
		base.CheckValues();

		if (analitics == null) {
			analitics = new SaveDataAnalitics();
		}

		if(levels == null) {
			levels = new Dictionary<string, SaveDataLevel>();
		}
		if(items == null) {
			items = new Dictionary<ItemType, ObscuredInt>();
		}
		if(spellOrder == null) {
			spellOrder = new List<ItemType>();
		}
		if(playerLocation == null) {
			playerLocation = new SaveDataPlayerLocation();
		}
		if(levelUpgrades == null) {
			levelUpgrades = new SaveDataLevelUpgrades();
		}
		levelUpgrades.CheckValues();

		if(islandExperience == null) {
			islandExperience = new SaveDataIslandExperience();
		}
		islandExperience.CheckValues();

		if(tutorial == null) {
			tutorial = new SaveDataTutorial();
		}
		tutorial.CheckValues();

		if(unlocks == null) {
			unlocks = new SaveDataUnlocks();
		}
		unlocks.CheckValues();

		sessionSpells ??= new Dictionary<ItemType, int>();
		var values = Enum.GetValues(typeof(ItemType));
		foreach(ItemType value in values) {
			if (IsSpell(value)) {
				// Старые сейвы хранили свитки в items — расходники между катками не переносятся
				items.Remove(value);
			} else if(!items.ContainsKey(value)) {
				items.Add(value, 0);
			}
		}
		spellOrder.RemoveAll(t => !IsSpell(t) || GetItem(t) <= 0);
	}

	// Спеллы в ItemType начинаются с 50
	public static bool IsSpell(ItemType type) => (int)type >= 50;

	/// <summary>Свитки, которые есть у игрока, в порядке получения</summary>
	public List<ItemType> GetOwnedSpellsInOrder() {
		var res = new List<ItemType>();
		foreach (var type in spellOrder) {
			if (GetItem(type) > 0)
				res.Add(type);
		}
		return res;
	}

	private void UpdateSpellOrder(ItemType type) {
		if (!IsSpell(type))
			return;
		bool owned = GetItem(type) > 0;
		if (owned && !spellOrder.Contains(type))
			spellOrder.Add(type);
		else if (!owned)
			spellOrder.Remove(type);
	}

	public ObscuredInt GetItem(ItemType type) {
		if (IsSpell(type)) {
			return new ObscuredInt(sessionSpells.TryGetValue(type, out int count) ? count : 0);
		}
		if (!items.ContainsKey(type)) {
			return new ObscuredInt(0);
		}

		return items[type];
	}

	public void AddItem(Reward r) {
		AddItem(r.item, r.count);
	}

	public void AddItem(ItemType type, int amount) {
		if (IsSpell(type)) {
			sessionSpells.TryGetValue(type, out int count);
			sessionSpells[type] = count + amount;
		} else {
			if(!items.ContainsKey(type)) {
				items.Add(type, 0);
			}
			items[type] += amount;
		}
		UpdateSpellOrder(type);
		InGameEventManager.SendEvent(InGameEvents.OnItemAdded, type);
		InGameEventManager.SendEvent(InGameEvents.OnItemCountChanged, type);
	}

	public bool SpendItem(ItemType type, int amount) {
		if (!HasItem(type, amount))
			return false;

		if (IsSpell(type))
			sessionSpells[type] -= amount;
		else
			items[type] -= amount;
		UpdateSpellOrder(type);
		InGameEventManager.SendEvent(InGameEvents.OnItemUsed, type);
		InGameEventManager.SendEvent(InGameEvents.OnItemCountChanged, type);
		return true;
	}

	public bool HasItem(ItemType type, int amount) {
		return GetItem(type) >= amount;
	}

}
