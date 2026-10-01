using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;


[System.Serializable]
public class Reward {
	public ItemType item;
	public ObscuredInt count;
}

[System.Serializable]
public class Route {
	public List<Waypoint> waypoints;
	// Точка высадки, с которой начинается маршрут (индекс в LevelDesriptor.spawnPoints). Несколько маршрутов могут
	// начинаться с одной высадки (развилка дороги); «торговая» запись spawnPoints маршрутам не принадлежит
	public int spawnPointIdx;
}

[System.Serializable]
public class Waypoint {
	public WaypointMarker marker;
	public Waypoint next;
	public Waypoint previous;
	public Vector3 position;
}

[System.Serializable]
public class Wave {
	public int id;
	public List<SpawnGroup> groups;
	public bool disabled;
	public float spawnTime;
	public RamSettings ramSettings;
	// Высадка по кругу: отспавнив все группы, корабль начинает волну заново, пока флаг не снимут (чит-парад).
	// В авторских данных не хранится
	[System.NonSerialized] public bool loop;
}

[System.Serializable]
public class SpawnGroup {
	public int[] waitingList;
	public float startDelay;
	public int routeIdx;
	public int spawnPointIdx;
	public float minSpawnDelay;
	public float maxSpawnDelay;
	// Зазор до предыдущих юнитов на точке спавна — в долях суммы габаритов тел (0 — вплотную, 1 — просвет длиной в сами тела).
	// Спавн ждёт и кулдаун выше, и дистанцию; генератор волн кулдаун не задаёт и работает только от дистанции
	public float minGapFactor;
	public float maxGapFactor;
	// Полоса группы: боковой сдвиг от оси дороги (вправо по ходу), юниты мира. Юниты держат её до самого замка (Unit.SetLane),
	// а дистанция спавна до юнитов соседних полос считается по диагонали — параллельные группы идут плотнее, не клипаясь
	public float laneOffset;
	public UnitType type;
	public int unitsCount;
	public float unitsHPMultiplier = 1f;
	// Множитель наград за килл; -1 — как unitsHPMultiplier. Боссу генератор ставит фракционный, а не масштаб под пул
	public float rewardMultiplier = -1f;
	public bool isBoss = false;
}

[System.Serializable]
public class SpawnPoint {
	public WaypointMarker point;
	public float radius;
	public int arrivingShipRoute;
	public int departingShipRoute;
	public WaypointMarker shipTargetPoint;
}

public class LevelDesriptor : Singleton<LevelDesriptor>
{
	public int levelID;
	public int startCoinsAmount;
	public int castleHP;
	public int cointForWaveSpeedUp;
	public List<Route> routs;
	public List<Route> shipRouts;
	[HideInInspector] public List<Wave> waves;
	public List<SpawnPoint> spawnPoints;
	public List<Reward> rewards;


#if UNITY_EDITOR
	[HideInInspector] public bool pricesFoldOut;

	[HideInInspector] public bool routsFoldOut;
	[HideInInspector] public List<bool> routsIndividualFouldOut;

	[HideInInspector] public bool wavesFoldOut;
	[HideInInspector] public List<bool> wavesIndividualFouldOut;
#endif

	// Start is called before the first frame update
	protected override void Awake()
    {
		base.Awake();
		if (routs.Count == 0) {
			Debug.LogError("We need more waypoints");
			return;
		}

		FillRouteData(routs);
		FillRouteData(shipRouts);
    }

	private void FillRouteData(List<Route> r) {
		for (int i = 0; i < r.Count; i++) {

			if (r[i].waypoints.Count < 2) {
				Debug.LogError("We need more waypoints");
				return;
			}

			//waypoints[0][0].isStart = true;
			for (int j = 0; j < r[i].waypoints.Count; j++) {
				r[i].waypoints[j].position = r[i].waypoints[j].marker.transform.position;
				if (j != r[i].waypoints.Count - 1)
					r[i].waypoints[j].next = r[i].waypoints[j + 1];
				if(j > 0)
					r[i].waypoints[j].previous = r[i].waypoints[j - 1];
			}
		}
	}

	public Waypoint GetRoute(int i) {
		return routs[i].waypoints[0];
	}

	public Waypoint GetShipRoute(int i) {
		return shipRouts[i].waypoints[0];
	}

	public ItemUnitPrice GetUnitReward(UnitType type)
	{
		var value = GameController.Instance.unitPriceSettings.GetEntry(type);
		if (value == null)
		{
			Debug.LogError("Please add price for " + type + " unit type");
		}
		return value;
	}

	// Начало маршрута — его точка высадки (Route.spawnPointIdx), а не spawnPoints[r]: маршрутов бывает больше, чем высадок
	public Vector3 GetRouteSpawnPosition(int r) {
		return spawnPoints[routs[r].spawnPointIdx].point.transform.position;
	}

	public float GetRouteLength(int r) {
		float res = 0;
		Vector3 prevPosition = GetRouteSpawnPosition(r);
		for(int i = 0; i < routs[r].waypoints.Count; i++) {
			res += Vector3.Distance(prevPosition, routs[r].waypoints[i].position);
			prevPosition = routs[r].waypoints[i].position;
		}

		return res;
	}

	private float GetSegmentLength(int r, int i) {
		if(i == 0) {
			return Vector3.Distance(GetRouteSpawnPosition(r), routs[r].waypoints[i].position);
		} else {
			return Vector3.Distance(routs[r].waypoints[i - 1].position, routs[r].waypoints[i].position);
		}
	}

	private Vector3 GetSegmentStartPoint(int r, int i) {
		if (i == 0) {
			return GetRouteSpawnPosition(r);
		} else {
			return routs[r].waypoints[i - 1].position;
		}
	}

	private Vector3 GetSegmentEndPoint(int r, int i) {
		return routs[r].waypoints[i].position;
	}

	public Vector3 GetRoutePoint(int r, float length) {
		int segments = routs[r].waypoints.Count;

		float totalLength = 0;

		for (int i = 0; i < segments; i++) {
			float newSegmentLength = GetSegmentLength(r, i);
			if(totalLength + newSegmentLength > length) {
				//мы нашли нужный нам сегмент
				Vector3 startPoint = GetSegmentStartPoint(r, i);
				Vector3 endPoint = GetSegmentEndPoint(r, i);

				return startPoint + (endPoint - startPoint).normalized * (length - totalLength);
			}
			totalLength += newSegmentLength;
		}

		return Vector3.zero;
	}
}
