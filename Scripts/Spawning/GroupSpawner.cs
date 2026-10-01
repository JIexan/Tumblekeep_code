using System;
using System.Collections.Generic;
using UnityEngine;

public class GroupSpawner
{
	private int id;
	private List<int> waitingList;

	private SpawnGroup group;
	private int count;

	public enum SpawnerState {
		Waiting, //ожидание пока другие группы закончат
		Busy, //startDelay или кулдаун после спавна юнита
		ReadyToSpawn, //ожидает команды на спавн
		Finished, //закончил работу
	}

	public SpawnerState state;

	// Остаток startDelay / кулдауна. Тикает из WaveSpawner.Update, отложенных вызовов нет:
	// спавнер не может пережить свой корабль и не зависит от порядка колбэков параллельных групп
	private float timer;
	private bool disposed;

	// Габариты юнита группы (по префабу) — WaveSpawner ждёт, пока под него освободится точка спавна
	public Unit.SpacingBounds Bounds { get; private set; }
	// Зазор и боковая позиция (полоса группы ± разброс) ближайшего спавна; бросаются один раз на юнита,
	// иначе условие дистанции дрожало бы каждый кадр
	public float GapFactor { get; private set; }
	public float Lateral { get; private set; }
	public int RouteIdx => group.routeIdx;

	private float laneJitter;

	public GroupSpawner(SpawnGroup group, int id, float laneJitter) {
		this.group = group;
		this.id = id;
		this.laneJitter = laneJitter;
		state = SpawnerState.Waiting;
		waitingList = new List<int>(group.waitingList);
		count = 0;

		// Префаба нет — Spawn залогирует ошибку, а габариты остаются условными, чтобы волна не вставала
		if (GameController.Instance.unitsToSpawn.unitsToSpawn.TryGetValue(group.type, out var prefab) && prefab != null)
			Bounds = prefab.GetSpacingBounds();
		else
			Bounds = new Unit.SpacingBounds { front = 0.5f, back = 0.5f, bottom = 0f, top = 1f };
		RollNext();

		InGameEventManager.Subscribe(InGameEvents.OnSpawnerFinished, OnSpawnerFinished);
		OnSpawnerFinished(id);
	}

	public void OnSpawnerFinished(object id) {
		if (disposed)
			return;
		if(waitingList != null && waitingList.Contains((int)id)) {
			waitingList.Remove((int)id);
		}

		if(waitingList.Count == 0) {
			//старт этого спавнера
			InGameEventManager.Unsubscribe(InGameEvents.OnSpawnerFinished, OnSpawnerFinished);
			state = SpawnerState.Busy;
			timer = group.startDelay;
		}
	}

	public void Update(float deltaTime) {
		if (state != SpawnerState.Busy)
			return;
		timer -= deltaTime;
		if (timer > 0f)
			return;

		if (group.unitsCount <= 0) {
			// Пустая группа (кривая авторская волна) — сразу финиш, иначе ждущие её группы и корабль зависнут
			state = SpawnerState.Finished;
			InGameEventManager.SendEvent(InGameEvents.OnSpawnerFinished, id);
		} else {
			state = SpawnerState.ReadyToSpawn;
		}
	}

	/// <summary>
	/// Заспавнить очередного юнита группы. Возвращает null, если спавн упал — счётчик группы всё равно
	/// двигается: сломанный префаб не должен вешать волну, корабль обязан доспавнить группу и уплыть
	/// </summary>
	public Unit Spawn(int waveId, Vector3 position, Quaternion rotation) {
		if (disposed || state != SpawnerState.ReadyToSpawn)
			return null;

		Unit unit = null;
		try {
			unit = GameController.Instance.SpawnUnitExactPosition(waveId, position, rotation, group.type, group.routeIdx, null, group.unitsHPMultiplier, group.isBoss, group.rewardMultiplier);
		} catch (Exception e) {
			Debug.LogException(e);
		}

		count++;
		if(count >= group.unitsCount) {
			state = SpawnerState.Finished;
			InGameEventManager.SendEvent(InGameEvents.OnSpawnerFinished, id);
		} else {
			state = SpawnerState.Busy;
			timer = UnityEngine.Random.Range(group.minSpawnDelay, group.maxSpawnDelay);
			RollNext();
		}
		return unit;
	}

	private void RollNext() {
		GapFactor = Mathf.Max(UnityEngine.Random.Range(group.minGapFactor, group.maxGapFactor), 0f);
		Lateral = group.laneOffset + UnityEngine.Random.Range(-laneJitter, laneJitter);
	}

	/// <summary>
	/// Остановить спавнер: отписка от OnSpawnerFinished. Без этого спавнеры уничтоженного корабля
	/// оставались на статической шине и после перезагрузки сцены.
	/// </summary>
	public void Dispose() {
		if (disposed)
			return;
		disposed = true;
		state = SpawnerState.Finished;
		InGameEventManager.Unsubscribe(InGameEvents.OnSpawnerFinished, OnSpawnerFinished);
	}
}
