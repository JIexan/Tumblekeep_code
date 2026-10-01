using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Shore settings")]
public class ShoreSettings : ScriptableObject
{
	[System.Serializable]
	public class ShoreData {
		public GameObject[] go;
		public float placeChance;
		public Vector2 centerOffset;
		public int[,] data;
	}

	public List<ShoreData> shores;
}
