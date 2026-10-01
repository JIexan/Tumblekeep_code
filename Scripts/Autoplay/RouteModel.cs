#if AUTOPLAY
using System.Collections.Generic;
using UnityEngine;

namespace Autoplay {
	/// <summary>
	/// Дуговая модель маршрутов LevelDesriptor: положение юнита вдоль маршрута (у самих юнитов
	/// прогресса нет, только текущий вейпоинт) и последняя точка маршрута, покрытая радиусом башен.
	/// Юнит правее покрытия = «прошёл все башни», по нему бот кастует спеллы.
	/// </summary>
	public class RouteModel {
		public struct UnitProgress {
			public Unit unit;
			public int route;
			public float s;          // длина дуги от точки спавна
			public float remaining;  // до конца маршрута (замка)
		}

		private class RouteData {
			public float[] cumLen;         // длина дуги от спавна до вейпоинта i
			public float length;
			public float lastCoveredS = -1f; // -1: башни не покрывают маршрут вовсе
		}

		private const float SampleStep = 0.5f;
		private const float SafetyRebuildInterval = 5f;

		private readonly List<RouteData> routes = new List<RouteData>();
		private readonly Dictionary<Waypoint, (int route, int index)> index = new Dictionary<Waypoint, (int, int)>();
		private bool dirty = true;
		private float lastRebuildTime = -999f;

		public int RouteCount => routes.Count;

		public RouteModel() {
			var ld = LevelDesriptor.Instance;
			if (ld == null || ld.routs == null)
				return;
			for (int r = 0; r < ld.routs.Count; r++) {
				var wps = ld.routs[r].waypoints;
				var data = new RouteData { cumLen = new float[wps != null ? wps.Count : 0] };
				if (wps != null && wps.Count > 0) {
					Vector3 prev = SpawnStart(ld, r);
					float total = 0f;
					for (int i = 0; i < wps.Count; i++) {
						total += Vector3.Distance(prev, wps[i].position);
						data.cumLen[i] = total;
						prev = wps[i].position;
						index[wps[i]] = (r, i);
					}
					data.length = total;
				}
				routes.Add(data);
			}
		}

		// Та же точка отсчёта, что в LevelDesriptor.GetRouteLength: точка высадки маршрута (Route.spawnPointIdx)
		private static Vector3 SpawnStart(LevelDesriptor ld, int r) {
			var first = ld.routs[r].waypoints[0].position;
			if (ld.spawnPoints == null || ld.spawnPoints.Count == 0)
				return first;
			var sp = ld.spawnPoints[Mathf.Clamp(ld.routs[r].spawnPointIdx, 0, ld.spawnPoints.Count - 1)];
			return sp != null && sp.point != null ? sp.point.transform.position : first;
		}

		public float Length(int r) => r >= 0 && r < routes.Count ? routes[r].length : 0f;
		public float LastCoveredS(int r) => r >= 0 && r < routes.Count ? routes[r].lastCoveredS : -1f;

		public void MarkDirty() => dirty = true;

		/// <summary>Пересчёт покрытия (после постройки/апгрейда башен и раз в несколько секунд для страховки)</summary>
		public void EnsureCoverage(float gameTime, List<BaseTower> towers) {
			if (!dirty && gameTime - lastRebuildTime < SafetyRebuildInterval)
				return;
			dirty = false;
			lastRebuildTime = gameTime;
			var ld = LevelDesriptor.Instance;
			if (ld == null)
				return;
			for (int r = 0; r < routes.Count; r++) {
				var data = routes[r];
				data.lastCoveredS = -1f;
				if (data.length <= 0f)
					continue;
				for (float s = 0f; s < data.length; s += SampleStep) {
					if (IsCovered(ld.GetRoutePoint(r, s), towers))
						data.lastCoveredS = s;
				}
			}
		}

		private static bool IsCovered(Vector3 point, List<BaseTower> towers) {
			if (towers == null)
				return false;
			foreach (var tower in towers) {
				if (tower == null || tower.towerType == TowerType.Placeholder)
					continue;
				float radius = tower.Radius;
				if (radius <= 0f)
					continue;
				Vector3 d = tower.transform.position - point;
				d.y = 0f;
				if (d.sqrMagnitude < radius * radius)
					return true;
			}
			return false;
		}

		public bool TryGetProgress(Unit unit, out UnitProgress progress) {
			progress = default;
			if (unit == null)
				return false;
			var wp = unit.CurrentWaypoint;
			if (wp == null || !index.TryGetValue(wp, out var at))
				return false;
			var data = routes[at.route];
			Vector3 d = wp.position - unit.transform.position;
			d.y = 0f;
			float s = Mathf.Clamp(data.cumLen[at.index] - d.magnitude, 0f, data.length);
			progress = new UnitProgress { unit = unit, route = at.route, s = s, remaining = data.length - s };
			return true;
		}

		/// <summary>Живые юниты, прошедшие последнюю покрытую башнями точку своего маршрута</summary>
		public void CollectLeakers(List<Unit> enemies, List<UnitProgress> result, float margin = 0.5f) {
			result.Clear();
			foreach (var unit in enemies) {
				if (unit == null || unit.isDead)
					continue;
				if (!TryGetProgress(unit, out var p))
					continue;
				if (p.s > routes[p.route].lastCoveredS + margin)
					result.Add(p);
			}
		}

		/// <summary>Точка маршрута на lead метров впереди позиции s (для спеллов с подлётом)</summary>
		public Vector3 PointAhead(int r, float s, float lead) {
			var data = routes[r];
			float target = Mathf.Clamp(s + lead, 0f, Mathf.Max(0f, data.length - 0.05f));
			return LevelDesriptor.Instance.GetRoutePoint(r, target);
		}
	}
}
#endif
