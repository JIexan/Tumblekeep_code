using UnityEngine;

/// <summary>
/// Данные о конкретном предложении товара в окне торговца
/// </summary>
public class MerchantOffer
{
	public ItemType ItemType { get; private set; }
	public int Count { get; private set; }
	public int Price { get; private set; }
	public ItemData SourceData { get; private set; }

	public MerchantOffer(ItemData data, int purchaseCount, ItemSettings settings) {
		SourceData = data;
		ItemType = data.itemType;
		Count = Random.Range(data.merchantData.minCount, data.merchantData.maxCount + 1);
		Price = settings.CalculateMerchantPrice(data.merchantData.basePrice, purchaseCount);
	}
}