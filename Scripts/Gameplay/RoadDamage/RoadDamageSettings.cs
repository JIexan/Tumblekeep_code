using DG.Tweening;
using UnityEngine;

namespace Gameplay.RoadDamage {
	/// <summary>
	/// Глобальные правила повреждения дороги. Ассет лежит в Assets/Settings/RoadDamageSettings.asset
	/// и подключён в GameConfigs. Шансы конкретного снаряда — в RoadImpactProfile на его префабе.
	/// </summary>
	[CreateAssetMenu(fileName = "RoadDamageSettings", menuName = "Configs/RoadDamageSettings")]
	public class RoadDamageSettings : ScriptableObject {
		[Header("Сетка")]
		[Tooltip("Размер клетки пространственной сетки, м. Плитка дороги ≈ 3.3 м.")]
		public float cellSize = 4f;

		[Header("Повреждение плитки")]
		[Tooltip("Материал повреждённой плитки (Road_DarkBrown из новых дорог).")]
		public Material damagedMaterial;
		[Tooltip("На сколько плитка опускается вниз, м (толщина плитки 0.1).")]
		public float sinkDepth = 0.04f;
		public float sinkDuration = 0.25f;
		public Ease sinkEase = Ease.OutCubic;
		[Tooltip("Не больше стольких повреждённых плиток в радиусе damagedCheckRadius от кандидата.")]
		public int maxDamagedInRadius = 3;
		public float damagedCheckRadius = 6f;

		[Header("Камушки")]
		public GameObject[] pebblePrefabs;
		[Tooltip("Не больше стольких камушков в радиусе pebbleCheckRadius от точки спавна.")]
		public int maxPebblesInRadius = 3;
		public float pebbleCheckRadius = 3f;
		[Tooltip("Случайный сдвиг камушка от точки удара по XZ, м.")]
		public float pebbleScatterRadius = 0.8f;
		public Vector2 pebbleScaleRange = new Vector2(0.45f, 1f);
		[Tooltip("Высота камушка над землёй (в новых дорогах ≈ 0.15).")]
		public float pebbleHeight = 0.15f;
		public float pebblePopDuration = 0.2f;
		public Ease pebblePopEase = Ease.OutBack;
	}
}
