using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemPackSettings
{
	public int id;
	public string googleId;
	public string appleId;
	public string name;
	public string description;
	public float price;

	public List<Reward> rewards;
}
