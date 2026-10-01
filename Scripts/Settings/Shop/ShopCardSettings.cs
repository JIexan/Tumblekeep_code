using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShopCardSettings
{
	public string id;
	public string name;
	public string description;

	public List<ItemPackSettings> packs;
}
