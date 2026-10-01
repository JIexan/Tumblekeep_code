using UnityEngine;

[CreateAssetMenu]
public class LocalizationSettings : ScriptableObject
{
	public string TableId;
	public string[] Sheets;
	public Object SaveFolder;
}
