using System.Collections.Generic;
using UnityEngine;

// Спавн волны с корабля. Юниты выходят не по таймеру, а по дистанции: следующий появляется, когда все
// недавно вышедшие реально отошли от линии спавна достаточно далеко. Дистанция меряется фактически пройденным
// путём (Unit.DistanceTravelled), а не считается из скорости — фриз, замедления, реликвии скорости и стоячие
// состояния (SpawningUnit, разворот тарана) учитываются сами собой.
// Параллельные группы идут по разным полосам дороги (SpawnGroup.laneOffset, юниты держат полосу до замка),
// и дистанция между ними считается по диагонали: боковой разнос полос вычитается из требуемого отхода вдоль
// дороги. Соседние полосы выходят почти плечом к плечу в шахматном порядке — волна не растягивается в одну
// колонну, но и тела друг в друга не появляются.
public class WaveSpawner : MonoBehaviour
{
	[Tooltip("Случайный разброс юнита поперёк своей полосы. Вдоль хода разброса нет: все стартуют с одной линии, иначе пройденный путь перестаёт быть дистанцией до юнита.")]
	[SerializeField] private float laneJitter = 0.35f;
	[Tooltip("Страховка: мешающие юниты стоят на месте дольше этого (фриз, стоячее состояние прямо на точке спавна) — спавним всё равно, корабль обязан доспавнить волну и уплыть. Пока они хоть как-то отходят, спавнер ждёт сколько нужно.")]
	[SerializeField] private float maxStalledTime = 4f;

	// Юнит, ещё не отошедший от точки спавна
	private struct ZoneUnit {
		public Unit unit;
		public Unit.SpacingBounds bounds;
		public float lateral; // боковая позиция от оси маршрута
		public int serial;    // порядковый номер выхода
	}

	private bool spawning = false;
	private Wave wave;
	private int currentSpawner = 0;
	private List<GroupSpawner> groupSpawners = new List<GroupSpawner>();
	private readonly List<ZoneUnit> spawnZone = new List<ZoneUnit>();
	private int nextSerial;
	// Голова очереди и номер выхода, с которого она ждёт: вышедшие позже — «обошедшие» её юниты соседних полос
	private GroupSpawner head;
	private int headSerial;
	// Сколько мешающие юниты не двигаются и сколько им оставалось пройти кадр назад (детектор простоя для страховки)
	private float stalledTime;
	private float lastBlockingDistance;
	// Максимумы по группам волны: юнит, отошедший дальше любого возможного требования, из зоны вычёркивается
	private float maxFront;
	private float maxGapFactor;
	private int spawnPointID = 0;
	public int WaveId { get; private set; }

	public void Init(Wave currentWave, int spawnPointID) {
		wave = currentWave;
		this.spawnPointID = spawnPointID;
		WaveId = currentWave.id;
		BuildSpawners();
	}

	/// <summary>
	/// Досрочно закончить высадку (чит-парад): группы гасятся, корабль штатно уплывает на следующем кадре
	/// </summary>
	public void FinishNow() {
		foreach (var spawner in groupSpawners)
			spawner.Dispose();
	}

	private void BuildSpawners() {
		DisposeSpawners();
		currentSpawner = 0;
		head = null;
		ResetStall();
		maxFront = 0f;
		maxGapFactor = 0f;

		int i = 0;
		foreach (var group in wave.groups) {
			if (group.spawnPointIdx == spawnPointID) {
				var spawner = new GroupSpawner(group, i, laneJitter);
				groupSpawners.Add(spawner);
				maxFront = Mathf.Max(maxFront, spawner.Bounds.front);
				maxGapFactor = Mathf.Max(maxGapFactor, group.minGapFactor, group.maxGapFactor);
			}
			i++;
		}
		spawning = true;
	}

	// Корабль уничтожен (уплыл, рестарт, смена сцены) — гасим спавнеры, иначе они останутся на статической шине событий
	private void OnDestroy() {
		DisposeSpawners();
	}

	private void DisposeSpawners() {
		spawning = false;
		foreach (var spawner in groupSpawners)
			spawner.Dispose();
		groupSpawners.Clear();
	}

	private void Update() {
		if (!spawning)
			return;

		// Пауза (timeScale = 0): таймеры стоят, юниты стоят — и спавн стоит
		float deltaTime = Time.deltaTime;
		if (deltaTime <= 0f)
			return;

		bool allSpawnersFinished = true;
		foreach (var spawner in groupSpawners) {
			spawner.Update(deltaTime);
			if (spawner.state != GroupSpawner.SpawnerState.Finished)
				allSpawnersFinished = false;
		}

		if (allSpawnersFinished) {
			// Зацикленная волна начинается заново; недавно вышедшие остаются в spawnZone — дистанция держится и на стыке кругов
			if (wave.loop && groupSpawners.Count > 0) {
				BuildSpawners();
				return;
			}
			spawning = false;
			// Используем ShipsController через GameController
			GameController.Instance.Ships.DepartSpawningShip(spawnPointID);
			return;
		}

		PruneSpawnZone();

		// Голова очереди — первая готовая группа по кругу
		int headIndex = -1;
		for (int i = 0; i < groupSpawners.Count; i++) {
			int realI = (currentSpawner + i) % groupSpawners.Count;
			if (groupSpawners[realI].state == GroupSpawner.SpawnerState.ReadyToSpawn) {
				headIndex = realI;
				break;
			}
		}
		if (headIndex < 0) {
			head = null;
			ResetStall();
			return;
		}
		if (groupSpawners[headIndex] != head) {
			head = groupSpawners[headIndex];
			headSerial = nextSerial;
			ResetStall();
		}
		// Указатель круга встаёт на голову: группа, ставшая готовой позже, не влезет перед ней
		currentSpawner = headIndex;

		float blockingDistance = GetBlockingDistance(head, int.MaxValue);
		if (blockingDistance > 0f) {
			// Место освобождается — ждём сколько нужно (медленный дирижабль отходит долго, и это нормально).
			// Простой копится, только пока мешающие не сдвинулись ни на сколько
			if (blockingDistance < lastBlockingDistance)
				stalledTime = 0f;
			else
				stalledTime += deltaTime;
			lastBlockingDistance = blockingDistance;

			if (stalledTime < maxStalledTime) {
				TrySpawnAroundHead(headIndex);
				return;
			}
			Debug.LogWarning($"[WaveSpawner] Точка спавна {spawnPointID}: юниты стоят на ней дольше {maxStalledTime:F0}с — спавню без дистанции");
		}

		Spawn(head);
		currentSpawner = (headIndex + 1) % groupSpawners.Count;
		head = null;
		ResetStall();
	}

	// Пока голова ждёт своё место, остальные готовые группы занимают свободные полосы — иначе медленный таран
	// в одной полосе растягивал бы всю волну под свой темп. Но только пока голову держат юниты, вышедшие ДО того,
	// как она встала в очередь: как только мешают одни «обошедшие», обход закрыт, и голова выходит следующей.
	// Без этого частая мелочь соседних полос не дала бы крупному юниту выйти до конца своих групп
	private void TrySpawnAroundHead(int headIndex) {
		if (GetBlockingDistance(head, headSerial) <= 0f)
			return;

		for (int i = 1; i < groupSpawners.Count; i++) {
			var spawner = groupSpawners[(headIndex + i) % groupSpawners.Count];
			if (spawner.state == GroupSpawner.SpawnerState.ReadyToSpawn && GetBlockingDistance(spawner, int.MaxValue) <= 0f) {
				Spawn(spawner);
				return;
			}
		}
	}

	private void ResetStall() {
		stalledTime = 0f;
		lastBlockingDistance = float.MaxValue;
	}

	private void PruneSpawnZone() {
		for (int i = spawnZone.Count - 1; i >= 0; i--) {
			var z = spawnZone[i];
			if (z.unit == null || z.unit.isDead || z.unit.DistanceTravelled >= (z.bounds.back + maxFront) * (1f + maxGapFactor))
				spawnZone.RemoveAt(i);
		}
	}

	// Сколько ещё должны пройти недавно вышедшие (с номером выхода меньше serialLimit), чтобы под юнита группы
	// освободилось место (<= 0 — свободно). Между телами нужен просвет: сумма габаритов (зад вышедшего + перед нового)
	// с зазором группы. Он считается по диагонали — боковой разнос полос идёт в зачёт, вдоль дороги остаётся
	// отойти на sqrt(просвет² − разнос²). Тела на разной высоте (воздух над пехотой) друг друга не ждут
	private float GetBlockingDistance(GroupSpawner next, int serialLimit) {
		var bounds = next.Bounds;
		float blockingDistance = 0f;
		foreach (var z in spawnZone) {
			if (z.serial >= serialLimit || !z.bounds.OverlapsVertically(bounds))
				continue;
			float clearance = (z.bounds.back + bounds.front) * (1f + next.GapFactor);
			float lateral = Mathf.Abs(z.lateral - next.Lateral);
			if (lateral >= clearance)
				continue;
			float requiredAlong = Mathf.Sqrt(clearance * clearance - lateral * lateral);
			blockingDistance = Mathf.Max(blockingDistance, requiredAlong - z.unit.DistanceTravelled);
		}
		return blockingDistance;
	}

	private void Spawn(GroupSpawner spawner) {
		Transform spawnPoint = LevelDesriptor.Instance.spawnPoints[spawnPointID].point.transform;

		// «Право» — поперёк хода к первому вейпоинту: та же ось, по которой юнит дальше держит полосу
		Vector3 direction = LevelDesriptor.Instance.GetRoute(spawner.RouteIdx).position - spawnPoint.position;
		direction.y = 0f;
		if (direction.sqrMagnitude < 0.0001f)
			direction = spawnPoint.forward;
		Vector3 right = Vector3.Cross(Vector3.up, direction.normalized);

		// Габариты и боковую позицию берём до Spawn: он сразу бросает значения для следующего юнита группы
		var bounds = spawner.Bounds;
		float lateral = spawner.Lateral;
		Unit unit = spawner.Spawn(WaveId, spawnPoint.position + right * lateral, spawnPoint.rotation);
		if (unit == null)
			return;

		unit.SetLane(lateral, spawnPoint.position);
		spawnZone.Add(new ZoneUnit { unit = unit, bounds = bounds, lateral = lateral, serial = nextSerial++ });
	}
}
