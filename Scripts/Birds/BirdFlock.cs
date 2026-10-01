using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Стая: группа летящих птиц + аттрактор — точка, к которой все стремятся.
/// Аттрактор движется по вейпоинтам чуть медленнее птиц (они завихряются вокруг — это и есть «стайность»).
/// Логика фаз — в BirdsManager.UpdateFlock.
/// </summary>
public class BirdFlock
{
	public enum Phase
	{
		Ascend,   // набор высоты от точки испуга
		Cruise,   // перелёт по дуге к целевой зоне
		Descend,  // снижение и рассадка
		Loiter,   // кружение (нет безопасной зоны)
		Leave     // улёт с карты
	}

	public Phase phase;
	public readonly List<BirdAgent> members = new List<BirdAgent>();

	public Vector3 attractor;
	public readonly List<Vector3> waypoints = new List<Vector3>();
	public int wpIndex;

	public BirdPerchZone targetZone;
	public Vector3 threatPos;
	public float baseY;               // опорная высота земли (Y зоны взлёта)
	public float cruiseAltitude;      // случайная на стаю — заодно рассинхрон фазы взмаха в шейдере

	public float ignoreScaresUntil;

	// Loiter
	public float loiterStartTime;
	public Vector3 loiterCenter;
	public float loiterAngle;

	// Leave
	public Vector3 leaveOrigin;

	public Vector3 Centroid
	{
		get
		{
			if (members.Count == 0)
				return attractor;
			Vector3 sum = Vector3.zero;
			for (int i = 0; i < members.Count; i++)
				sum += members[i].pos;
			return sum / members.Count;
		}
	}
}
