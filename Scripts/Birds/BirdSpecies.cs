using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Вид птицы — пока только окрас по двум зонам (см. BirdRecolor.hlsl: G — пузико, B — тело, A — оттенок).
/// Ассеты лежат в Assets/Settings/Birds/. Списки видов с весами: по умолчанию — BirdsSettings.species,
/// у конкретной зоны — BirdPerchZone.Species (стартовое население и прилетающие туда стайки).
/// </summary>
[CreateAssetMenu(fileName = "BirdSpecies", menuName = "Configs/BirdSpecies")]
public class BirdSpecies : ScriptableObject
{
	[Tooltip("Спина, крылья, голова")]
	public Color body = new Color(0.792f, 0.82f, 0.863f);
	[Tooltip("Пузико")]
	public Color belly = new Color(0.525f, 0.604f, 0.651f);
}

/// <summary>Вид с весом для случайного выбора.</summary>
[System.Serializable]
public class BirdSpeciesWeight
{
	public BirdSpecies species;
	[Min(0f)] public float weight = 1f;

	/// <summary>Случайный вид по весам; null — список пуст или все веса нулевые (материал префаба как есть).</summary>
	public static BirdSpecies Pick(List<BirdSpeciesWeight> list)
	{
		if (list == null || list.Count == 0)
			return null;

		float sum = 0f;
		for (int i = 0; i < list.Count; i++)
			if (list[i].species != null)
				sum += Mathf.Max(0f, list[i].weight);
		if (sum <= 0f)
			return null;

		float r = Random.value * sum;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].species == null)
				continue;
			r -= Mathf.Max(0f, list[i].weight);
			if (r < 0f)
				return list[i].species;
		}
		return null;
	}
}
