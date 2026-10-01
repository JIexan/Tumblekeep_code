// Ключи локализации названий/описаний предметов. Спеллы описаны в книге-справочнике
// (Book_{ItemType}_Name/_Desc), остальные предметы — по конвенции ItemName_/ItemDesc_
// (см. UpgradeRequirementPresenter). Общая точка для окна сундука и торговца.
public static class ItemLocalizationKeys {
	public static string GetNameKey(ItemType item) {
		return IsSpell(item) ? "Book_" + item + "_Name" : "ItemName_" + item;
	}

	public static string GetDescKey(ItemType item) {
		return IsSpell(item) ? "Book_" + item + "_Desc" : "ItemDesc_" + item;
	}

	private static bool IsSpell(ItemType item) {
		var settings = GameConfigs.Instance != null ? GameConfigs.Instance.ItemSettings : null;
		var data = settings != null ? settings.GetItemData(item) : null;
		return data != null && data.category == ItemCategory.Spell;
	}
}
