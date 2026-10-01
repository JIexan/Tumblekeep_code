using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class UnitPriceSettings : ScriptableObject
{
    public List<ItemUnitPrice> ItemsUnitPrice;

    public ItemUnitPrice GetEntry(UnitType type) {
        return ItemsUnitPrice.FirstOrDefault(x => x.unitType == type);
    }
}

[Serializable]
public class ItemUnitPrice
{
    public UnitType unitType;
    public int price;
    public int exp = 1;
    public int crystals = 1;
    [Range(0f, 1f)] public float crystalChance = 0.02f;
}
