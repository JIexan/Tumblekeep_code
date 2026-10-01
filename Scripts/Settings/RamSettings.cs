using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class RamSettings : ScriptableObject
{
	public List<RamGroup> units;
}

[System.Serializable]
public struct RamGroup {
	public UnitType type;
	public int unitCount;
}
