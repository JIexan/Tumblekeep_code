using System;
using UnityEngine;

[CreateAssetMenu]
public class BookUnitDescription : ScriptableObject
{
    public UnitDictionary unitsToSpawn;
}

[Serializable]
public class UnitDictionary : SerializableDictionary<UnitType, Unit> { }
