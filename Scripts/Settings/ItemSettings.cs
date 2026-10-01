using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSettings", menuName = "Settings/ItemSettings")]
public class ItemSettings : ScriptableObject
{
	[Header("Items")]
	[ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "ItemName")]
	public List<ItemData> items = new List<ItemData>();

	[Header("Merchant Settings")]
	[Tooltip("Экспонента цены за каждую покупку")]
	public float priceExponent = 1.3f;

	[Tooltip("Цена товара округляется до кратной этому шагу (1 — без шага)")]
	[Min(1)]
	public int priceRoundStep = 5;

	[Tooltip("Количество товаров для показа")]
	[Range(1, 5)]
	public int merchantItemsToShow = 3;

	/// <summary>
	/// Получить настройки конкретного айтема
	/// </summary>
	public ItemData GetItemData(ItemType type) {
		return items.FirstOrDefault(i => i.itemType == type);
	}

	/// <summary>
	/// Получить все айтемы определённой категории
	/// </summary>
	public List<ItemData> GetItemsByCategory(ItemCategory category) {
		return items.Where(i => i.category == category).ToList();
	}

	/// <summary>
	/// Получить настройки спелла
	/// </summary>
	public SpellData GetSpellData(ItemType type) {
		var item = GetItemData(type);
		return item?.spellData;
	}

	/// <summary>
	/// Получить случайные товары для торговца (по весу).
	/// avoid — товары с прилавка до реролла: сначала выбираем из остальных, прежними добираем, только если пул мал
	/// </summary>
	public List<ItemData> SelectRandomMerchantItems(int count, ICollection<ItemType> avoid = null) {
		var merchantItems = GetMerchantPool();
		List<ItemData> result = new List<ItemData>();

		if (avoid == null || avoid.Count == 0) {
			PickWeighted(merchantItems, count, result);
			return result;
		}

		PickWeighted(merchantItems.Where(i => !avoid.Contains(i.itemType)).ToList(), count, result);
		if (result.Count < count)
			PickWeighted(merchantItems.Where(i => avoid.Contains(i.itemType)).ToList(), count - result.Count, result);

		return result;
	}

	// Взвешенный выбор без повторов; availableItems расходуется
	private void PickWeighted(List<ItemData> availableItems, int count, List<ItemData> result) {
		for (int i = 0; i < count && availableItems.Count > 0; i++) {
			ItemData selected = SelectWeightedRandom(availableItems);
			if (selected != null) {
				result.Add(selected);
				availableItems.Remove(selected);
			}
		}
	}

	// Свитки, не открытые метой (хаб), торговец не продаёт
	private List<ItemData> GetMerchantPool() {
		return items.Where(i => i.availableInShop && i.merchantData != null
			&& (i.category != ItemCategory.Spell || Gameplay.Unlocks.ContentUnlocksManager.IsSpellUnlocked(i.itemType))).ToList();
	}

	// Сколько разных товаров торговец вообще может предложить этому профилю (для гейта реролла)
	public int GetMerchantPoolCount() {
		return GetMerchantPool().Count;
	}

	private ItemData SelectWeightedRandom(List<ItemData> pool) {
		if (pool.Count == 0) return null;

		float totalWeight = 0f;
		foreach (var item in pool) {
			totalWeight += item.merchantData.weight;
		}

		float randomValue = UnityEngine.Random.Range(0f, totalWeight);
		float currentWeight = 0f;

		foreach (var item in pool) {
			currentWeight += item.merchantData.weight;
			if (randomValue <= currentWeight) {
				return item;
			}
		}

		return pool[pool.Count - 1];
	}

	/// <summary>
	/// Рассчитать цену товара с учётом количества покупок за катку (ShipsController.MerchantPurchaseCount).
	/// Округляется итоговая цена: прежнее усечение самого pow до int держало цену три покупки подряд,
	/// а потом разом удваивало (множитель 1, 1, 1, 2, 2, 3…). Math.Round с AwayFromZero —
	/// у Mathf.Round банковское округление.
	/// </summary>
	public int CalculateMerchantPrice(int basePrice, int purchaseCount) {
		int step = Mathf.Max(1, priceRoundStep);
		double raw = basePrice * Math.Pow(priceExponent, Mathf.Max(0, purchaseCount));
		return (int)Math.Round(raw / step, MidpointRounding.AwayFromZero) * step;
	}
}

public enum ItemCategory
{
	Currency,
	Spell
}

/// <summary>
/// Глобальные данные одного айтема
/// </summary>
[Serializable]
public class ItemData
{
	[Header("General")]
	public ItemType itemType;
	public ItemCategory category;
	public Sprite icon;
	public bool availableInShop = true;

	public string ItemName => itemType.ToString();

	[ShowIf("availableInShop", true)]
	public MerchantData merchantData;

	[ShowIf("category", ItemCategory.Spell)]
	public SpellData spellData;
}

/// <summary>
/// Данные для продажи у торговца
/// </summary>
[Serializable]
public class MerchantData
{
	[Tooltip("Базовая цена товара")]
	public int basePrice = 50;

	[Tooltip("Вес для случайного выбора товара (чем больше, тем чаще выпадает)")]
	[Range(0f, 100f)]
	public float weight = 1f;

	[Tooltip("Минимальное количество товара")]
	//[Min(1)]
	public int minCount = 1;

	[Tooltip("Максимальное количество товара")]
	//[Min(1)]
	public int maxCount = 5;
}

/// <summary>
/// Данные спелла
/// </summary>
[Serializable]
public class SpellData
{
	public int price;
	public float value1;
	public float value2;
	public float value3;
}
