using System;
using System.Collections.Generic;
using UnityEngine;
using static SaveDataTechnologyTree;

[CreateAssetMenu]
public class TechnologyTree : ScriptableObject
{
    public List<ItemTechnologyTree> itemsTechnologyTree;
}

[Serializable]
public class ItemTechnologyTree
{
    public idTechTree id;
    public string value; // значение
    public string description; // описание
    public Sprite imageTechTree; // картинка
    public hierarchyTechTree hierarchy; // тип объекта
    public typeItemTech type; // тип улучшения
    public bool openItem; // получен
    public int cost; // цена
}

[Serializable]
public enum hierarchyTechTree
{
    item,
    category,
}

[Serializable]
public enum typeItemTech
{
    archer,
    mortar,
    electric,
    magic,
    castle,
}