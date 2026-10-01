using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class CharacteristicData {

	[HideLabel]
	[PreviewField(Alignment = ObjectFieldAlignment.Left)]
	[HorizontalGroup(GroupID = "Line", Width = 50)]
	public Sprite icon;

	[VerticalGroup(GroupID = "Line/Column")]
	public CharacteristicType type;

	[HorizontalGroup(GroupID = "Line/Column/MinMax")]
	[LabelWidth(65)]
	public float MinValue;

	[HorizontalGroup(GroupID = "Line/Column/MinMax")]
	[LabelWidth(65)]
	public float MaxValue;

	public bool positiveIncrease;
	public string unitsLocale;

	[HorizontalGroup(GroupID = "Line2")]
	[LabelWidth(120)]
	public float baseIncreaseMin;
	[HorizontalGroup(GroupID = "Line2")]
	[LabelWidth(120)]
	public float baseIncreaseMax;
}

[CreateAssetMenu]
public class CharacteristicSettings : ScriptableObject
{
	[ListDrawerSettings(ShowFoldout = true, ShowPaging =true, NumberOfItemsPerPage = 5)]
	[SerializeField] private List<CharacteristicData> data;

	public CharacteristicData Get(CharacteristicType type) {
		for (int i = 0; i < data.Count; i++) {
			if (data[i].type == type)
				return data[i];
		}
		return null;
	}
}
