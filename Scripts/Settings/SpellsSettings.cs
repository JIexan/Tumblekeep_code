using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName ="SpellsSettings", menuName = "Settings/SpellsSettings")]
public class SpellsSettings : ScriptableObject
{
	public List<SpellSetting> settings;

	public SpellSetting GetSpellSettings(ItemType type) {
		return settings.FirstOrDefault(s => s.type == type);

	}
}

[System.Serializable]
public class SpellSetting {
	public ItemType type;
	public int price;
	public float value1;
	public float value2;
}
