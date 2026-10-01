using UnityEngine;

public class ItemIconsManager : Singleton<ItemIconsManager>
{
	private ItemSettings itemSettings => GameConfigs.Instance.ItemSettings;

	public Sprite GetIcon(ItemType item) {
		var data = itemSettings.GetItemData(item);
		if (data == null || data.icon == null)
			return null;

		return data.icon;
	}
}
