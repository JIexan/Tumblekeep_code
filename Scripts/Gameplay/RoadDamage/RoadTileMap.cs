using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.RoadDamage {
	/// <summary>
	/// База плиток дороги уровня + пространственная сетка + правила повреждения.
	/// Кладётся в сцену уровня; плитки — все активные MeshFilter под roadRoots (минус excludeRoots).
	/// Снаряды сообщают об ударе о землю через статический ReportImpact; без карты в сцене — no-op.
	/// Без физики: поиск по клеткам XZ размера settings.cellSize.
	/// </summary>
	public class RoadTileMap : MonoBehaviour {
		[Tooltip("Корни, чьи активные дети с MeshFilter считаются плитками дороги.")]
		[SerializeField] List<GameObject> roadRoots = new List<GameObject>();
		[Tooltip("Исключения: площадки башен из плиток, скалы и прочее, что ломать нельзя.")]
		[SerializeField] List<GameObject> excludeRoots = new List<GameObject>();

		private class Tile {
			public Transform transform;
			public MeshRenderer renderer;
			public Vector3 position;
			public bool damaged;
		}

		private class Pebble {
			public Vector3 position;
		}

		private RoadDamageSettings settings;
		private float cellSize = 4f;
		private readonly Dictionary<Vector2Int, List<Tile>> tileCells = new Dictionary<Vector2Int, List<Tile>>();
		private readonly Dictionary<Vector2Int, List<Pebble>> pebbleCells = new Dictionary<Vector2Int, List<Pebble>>();
		private readonly List<Tile> allTiles = new List<Tile>();
		private Transform pebblesRoot;

		public static RoadTileMap Current { get; private set; }

		public int TilesCount => allTiles.Count;

		// Сброс статиков на случай отключённого domain reload в редакторе
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics() {
			Current = null;
		}

		private void Awake() {
			settings = GameConfigs.Instance != null ? GameConfigs.Instance.RoadDamageSettings : null;
			if (settings == null) {
				Debug.LogWarning("[RoadTileMap] RoadDamageSettings не подключены в GameConfigs — повреждение дороги выключено.", this);
				enabled = false;
				return;
			}
			cellSize = Mathf.Max(0.5f, settings.cellSize);
			Rebuild();
		}

		private void OnEnable() {
			if (Current == null)
				Current = this;
			else if (Current != this)
				Debug.LogWarning("[RoadTileMap] В сцене больше одной карты дороги.", this);
		}

		private void OnDisable() {
			if (Current == this)
				Current = null;
		}

		private void OnDestroy() {
			foreach (var tile in allTiles) {
				if (tile.transform != null)
					DOTween.Kill(tile.transform);
			}
		}

		// ---------- Сбор плиток ----------

		private void Rebuild() {
			tileCells.Clear();
			pebbleCells.Clear();
			allTiles.Clear();

			foreach (var filter in CollectTileFilters()) {
				var tile = new Tile {
					transform = filter.transform,
					renderer = filter.GetComponent<MeshRenderer>(),
					position = filter.transform.position,
				};
				allTiles.Add(tile);
				GetOrCreate(tileCells, CellOf(tile.position)).Add(tile);
			}
		}

		// Общая часть для рантайма и гизмо: активные MeshFilter под корнями, кроме исключений.
		private List<MeshFilter> CollectTileFilters() {
			var result = new List<MeshFilter>();
			var seen = new HashSet<MeshFilter>();
			foreach (var root in roadRoots) {
				if (root == null)
					continue;
				foreach (var filter in root.GetComponentsInChildren<MeshFilter>(false)) {
					if (filter.sharedMesh == null || filter.GetComponent<MeshRenderer>() == null)
						continue;
					if (IsExcluded(filter.transform))
						continue;
					if (seen.Add(filter))
						result.Add(filter);
				}
			}
			return result;
		}

		private bool IsExcluded(Transform t) {
			foreach (var ex in excludeRoots) {
				if (ex != null && t.IsChildOf(ex.transform))
					return true;
			}
			return false;
		}

		// ---------- Сетка ----------

		private Vector2Int CellOf(Vector3 p) {
			return new Vector2Int(Mathf.FloorToInt(p.x / cellSize), Mathf.FloorToInt(p.z / cellSize));
		}

		private static List<T> GetOrCreate<T>(Dictionary<Vector2Int, List<T>> cells, Vector2Int key) {
			if (!cells.TryGetValue(key, out var list)) {
				list = new List<T>();
				cells[key] = list;
			}
			return list;
		}

		private static float SqrDistXZ(Vector3 a, Vector3 b) {
			float dx = a.x - b.x;
			float dz = a.z - b.z;
			return dx * dx + dz * dz;
		}

		// Перебирает клетки, покрывающие круг radius вокруг center.
		private IEnumerable<List<T>> CellsInRadius<T>(Dictionary<Vector2Int, List<T>> cells, Vector3 center, float radius) {
			Vector2Int min = CellOf(new Vector3(center.x - radius, 0f, center.z - radius));
			Vector2Int max = CellOf(new Vector3(center.x + radius, 0f, center.z + radius));
			for (int x = min.x; x <= max.x; x++) {
				for (int z = min.y; z <= max.y; z++) {
					if (cells.TryGetValue(new Vector2Int(x, z), out var list))
						yield return list;
				}
			}
		}

		private Tile FindNearestTile(Vector3 position, float radius) {
			Tile best = null;
			float bestSqr = radius * radius;
			foreach (var list in CellsInRadius(tileCells, position, radius)) {
				foreach (var tile in list) {
					if (tile.transform == null)
						continue;
					float sqr = SqrDistXZ(tile.position, position);
					if (sqr <= bestSqr) {
						bestSqr = sqr;
						best = tile;
					}
				}
			}
			return best;
		}

		private int CountDamagedInRadius(Vector3 position, float radius) {
			int count = 0;
			float sqrRadius = radius * radius;
			foreach (var list in CellsInRadius(tileCells, position, radius)) {
				foreach (var tile in list) {
					if (tile.damaged && SqrDistXZ(tile.position, position) <= sqrRadius)
						count++;
				}
			}
			return count;
		}

		private int CountPebblesInRadius(Vector3 position, float radius) {
			int count = 0;
			float sqrRadius = radius * radius;
			foreach (var list in CellsInRadius(pebbleCells, position, radius)) {
				foreach (var pebble in list) {
					if (SqrDistXZ(pebble.position, position) <= sqrRadius)
						count++;
				}
			}
			return count;
		}

		/// <summary>Случайная плитка в радиусе от точки (для демо-режима). null — плиток нет.</summary>
		public bool TryGetRandomTilePosition(Vector3 center, float radius, out Vector3 position) {
			position = default;
			float sqrRadius = radius * radius;
			int found = 0;
			// Резервуарная выборка — без аллокации списка кандидатов
			foreach (var list in CellsInRadius(tileCells, center, radius)) {
				foreach (var tile in list) {
					if (tile.transform == null || SqrDistXZ(tile.position, center) > sqrRadius)
						continue;
					found++;
					if (Random.Range(0, found) == 0)
						position = tile.position;
				}
			}
			return found > 0;
		}

		// ---------- Удар ----------

		/// <summary>Снаряд ударил о землю в position. Без карты в сцене — ничего не происходит.</summary>
		public static void ReportImpact(Vector3 position, RoadImpactProfile profile) {
			if (Current == null || profile == null || !profile.CanAffectRoad)
				return;
			Current.HandleImpact(position, profile);
		}

		private void HandleImpact(Vector3 position, RoadImpactProfile profile) {
			Tile tile = FindNearestTile(position, profile.tileSearchRadius);
			if (tile == null)
				return; // мимо дороги

			if (!tile.damaged && Random.value < profile.damageChance
				&& CountDamagedInRadius(tile.position, settings.damagedCheckRadius) < settings.maxDamagedInRadius) {
				DamageTile(tile);
			}

			if (Random.value < profile.pebbleChance
				&& settings.pebblePrefabs != null && settings.pebblePrefabs.Length > 0
				&& CountPebblesInRadius(position, settings.pebbleCheckRadius) < settings.maxPebblesInRadius) {
				SpawnPebble(position);
			}
		}

		private void DamageTile(Tile tile) {
			tile.damaged = true;

			if (settings.damagedMaterial != null && tile.renderer != null) {
				// Присваиваем ассет через sharedMaterials — инстансы материалов не плодятся (SRP Batcher)
				Material[] materials = tile.renderer.sharedMaterials;
				for (int i = 0; i < materials.Length; i++)
					materials[i] = settings.damagedMaterial;
				tile.renderer.sharedMaterials = materials;
			}

			if (settings.sinkDepth > 0f) {
				Transform t = tile.transform;
				DOTween.Kill(t);
				t.DOLocalMoveY(t.localPosition.y - settings.sinkDepth, settings.sinkDuration).SetEase(settings.sinkEase);
			}
		}

		private void SpawnPebble(Vector3 impactPosition) {
			if (pebblesRoot == null) {
				pebblesRoot = new GameObject("Pebbles").transform;
				pebblesRoot.SetParent(transform, false);
			}

			Vector2 offset = Random.insideUnitCircle * settings.pebbleScatterRadius;
			Vector3 position = new Vector3(impactPosition.x + offset.x, settings.pebbleHeight, impactPosition.z + offset.y);
			GameObject prefab = settings.pebblePrefabs[Random.Range(0, settings.pebblePrefabs.Length)];
			if (prefab == null)
				return;

			GameObject pebble = Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), pebblesRoot);
			float scale = Random.Range(settings.pebbleScaleRange.x, settings.pebbleScaleRange.y);
			Vector3 targetScale = prefab.transform.localScale * scale;
			if (settings.pebblePopDuration > 0f) {
				pebble.transform.localScale = Vector3.zero;
				pebble.transform.DOScale(targetScale, settings.pebblePopDuration).SetEase(settings.pebblePopEase);
			} else {
				pebble.transform.localScale = targetScale;
			}

			GetOrCreate(pebbleCells, CellOf(position)).Add(new Pebble { position = position });
		}

#if UNITY_EDITOR
		// Подсветка собранных плиток — проверить списки корней/исключений, не входя в плей-мод
		private void OnDrawGizmosSelected() {
			Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.6f);
			foreach (var filter in CollectTileFilters()) {
				Bounds b = filter.GetComponent<MeshRenderer>().bounds;
				Gizmos.DrawWireCube(b.center, b.size);
			}
		}
#endif
	}
}
