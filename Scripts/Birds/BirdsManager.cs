using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Менеджер птиц-боидов: один Update на всех птиц (паттерн ShadowsManager).
/// Кладётся руками в сцены с птицами (уровни + хаб). Зоны сидения (BirdPerchZone)
/// регистрируются сами через статический реестр — сцена без менеджера ничего не спамит.
/// Источники испуга: выстрелы башен (BaseTowerWeapon.ShotFired), удары снарядов о землю
/// (BaseProjectileTrigger.GroundImpacted), юниты рядом (поллинг),
/// скрытие зоны (BirdPerchZone.ZoneDisabled — например, ToggleObjectAction в хабе).
/// Пулы моделей птиц паркуются под poolsParent (по умолчанию — под самим менеджером): в хабе
/// менеджер лежит под MainLocation, так что при уходе на глобальную карту птицы гаснут вместе
/// с островом, а не разлетаются. Выключение зоны обрабатывается с задержкой в кадр — если зона
/// погасла вместе с островом и вернулась с ним, птицы остаются сидеть.
/// </summary>
public class BirdsManager : Singleton<BirdsManager>
{
	private struct PendingReturn
	{
		public float dueTime;
		public int count;
	}

	[SerializeField, Tooltip("Родитель контейнеров пулов моделей птиц. Пусто — сам менеджер. Масштаб родителя должен быть 1.")]
	private Transform poolsParent;

	private BirdsSettings settings;
	private GameController gameController;   // null в хабе — НЕ через Singleton.Instance (спамит warning)

	private readonly List<BirdAgent> agents = new List<BirdAgent>();
	private readonly List<BirdFlock> flocks = new List<BirdFlock>();
	private readonly List<PendingReturn> pendingReturns = new List<PendingReturn>();
	// Зоны, выключившиеся с прошлого Update: решение «пугать или нет» принимается в Update,
	// чтобы отличить скрытие одной зоны от выключения всего острова вместе с менеджером
	private readonly List<BirdPerchZone> disabledZones = new List<BirdPerchZone>();

	private float nextUnitPollTime;
	private float startTime;
	private bool isShuttingDown;

	// Переиспользуемые буферы
	private readonly List<int> slotIndexBuffer = new List<int>();
	private readonly List<BirdAgent> agentBuffer = new List<BirdAgent>();

	// Окрас по видам: на каждую пару (префаб модели, вид) — свой материал (SRP Batcher батчит по шейдеру,
	// количество материалов не важно; MaterialPropertyBlock, наоборот, выбивал бы птиц из батчера).
	private static readonly int BodyColorId = Shader.PropertyToID("_BodyColor");
	private static readonly int BellyColorId = Shader.PropertyToID("_BellyColor");
	private readonly Dictionary<GameObject, Material> baseMaterials = new Dictionary<GameObject, Material>();
	private readonly Dictionary<(GameObject prefab, BirdSpecies species), Material> speciesMaterials =
		new Dictionary<(GameObject prefab, BirdSpecies species), Material>();
	private readonly Dictionary<Transform, Renderer> rendererCache = new Dictionary<Transform, Renderer>();
	private readonly List<Material> createdMaterials = new List<Material>();

	private void Start()
	{
		settings = GameConfigs.Instance != null ? GameConfigs.Instance.BirdsSettings : null;
		if (settings == null || settings.sittingPrefab == null || settings.flyingPrefab == null)
		{
			Debug.LogError("BirdsManager: BirdsSettings не настроен в GameConfigs");
			enabled = false;
			return;
		}

		gameController = FindObjectOfType<GameController>();
		startTime = Time.time;

		Transform poolsRoot = poolsParent != null ? poolsParent : transform;
		SimplePool.SetParent(settings.sittingPrefab, poolsRoot);
		SimplePool.SetParent(settings.flyingPrefab, poolsRoot);

		BaseTowerWeapon.ShotFired += OnShotFired;
		BaseProjectileTrigger.GroundImpacted += Scare;
		BirdPerchZone.ZoneEnabled += OnZoneEnabled;
		BirdPerchZone.ZoneDisabled += OnZoneDisabled;

		// Защита от «мёртвых» записей при выключенном domain reload
		BirdPerchZone.ActiveZones.RemoveAll(z => z == null);

		foreach (BirdPerchZone zone in BirdPerchZone.ActiveZones)
		{
			zone.RebuildSlots(settings.slotSpacing);
			PopulateZone(zone);
		}

		// Чтобы первый испуг не инстанцировал всю стаю в один кадр
		SimplePool.Preload(settings.flyingPrefab, Mathf.Min(agents.Count, 16));
	}

	private void OnDestroy()
	{
		isShuttingDown = true;
		BaseTowerWeapon.ShotFired -= OnShotFired;
		BaseProjectileTrigger.GroundImpacted -= Scare;
		BirdPerchZone.ZoneEnabled -= OnZoneEnabled;
		BirdPerchZone.ZoneDisabled -= OnZoneDisabled;

		foreach (Material m in createdMaterials)
			if (m != null)
				Destroy(m);
		createdMaterials.Clear();
		speciesMaterials.Clear();
		baseMaterials.Clear();
	}

	private void OnApplicationQuit()
	{
		isShuttingDown = true;
	}

	private void Update()
	{
		if (settings == null)
			return;

		float dt = Time.deltaTime;
		float now = Time.time;

		ProcessDisabledZones(now);
		DecayZoneHeat(dt);
		PollUnits(now);
		ProcessPendingReturns(now);

		for (int i = flocks.Count - 1; i >= 0; i--)
		{
			UpdateFlock(flocks[i], dt, now);
			if (flocks[i].members.Count == 0)
				flocks.RemoveAt(i);
		}

		for (int i = agents.Count - 1; i >= 0; i--)
		{
			BirdAgent a = agents[i];
			switch (a.state)
			{
				case BirdAgent.BirdState.Sitting:
					UpdateSitting(a, now);
					break;
				case BirdAgent.BirdState.TakingOff:
					if (now >= a.releaseAt)
						DoTakeoff(a, now);
					break;
				case BirdAgent.BirdState.Flying:
				case BirdAgent.BirdState.Leaving:
					UpdateFlying(a, dt, now);
					break;
				case BirdAgent.BirdState.Landing:
					UpdateLanding(a, dt, now);
					break;
				case BirdAgent.BirdState.Touchdown:
					break; // ведёт твин
			}
		}
	}

	// ================== Население ==================

	/// <summary>Стартовое население: по каждому прямоугольнику — его доля слотов (PerchRect.initialFill).</summary>
	private void PopulateZone(BirdPerchZone zone)
	{
		for (int r = 0; r < zone.Rects.Count; r++)
		{
			int target = Mathf.RoundToInt(zone.GetRectSlotCount(r) * zone.Rects[r].initialFill);
			SpawnSeatedBirds(zone, r, target);
		}
	}

	private int SpawnSeatedBirds(BirdPerchZone zone, int rectIndex, int count)
	{
		if (count <= 0 || zone.SlotCount == 0)
			return 0;

		slotIndexBuffer.Clear();
		for (int i = 0; i < zone.SlotCount; i++)
			if (zone.GetSlotRect(i) == rectIndex && zone.GetSlotState(i) == BirdPerchZone.SlotState.Free)
				slotIndexBuffer.Add(i);

		// Тасование Фишера-Йетса
		for (int i = slotIndexBuffer.Count - 1; i > 0; i--)
		{
			int j = Random.Range(0, i + 1);
			(slotIndexBuffer[i], slotIndexBuffer[j]) = (slotIndexBuffer[j], slotIndexBuffer[i]);
		}

		int spawned = 0;
		for (int i = 0; i < slotIndexBuffer.Count && spawned < count; i++, spawned++)
		{
			int slot = slotIndexBuffer[i];
			var agent = new BirdAgent
			{
				state = BirdAgent.BirdState.Sitting,
				zone = zone,
				slot = slot,
				wanderSeed = Random.Range(0f, 1000f),
				nextHopTime = Time.time + Random.Range(settings.hopIntervalMin, settings.hopIntervalMax)
			};
			zone.OccupySlot(slot, agent);
			agent.species = PickSpecies(zone);
			agent.sittingTf = SpawnModel(settings.sittingPrefab, zone.GetSlotWorldPos(slot), zone.GetSlotWorldRotation(slot), agent.species);
			agents.Add(agent);
		}
		return spawned;
	}

	// ================== Сидящие: прыжки ==================

	private void UpdateSitting(BirdAgent a, float now)
	{
		if (now < a.nextHopTime || a.sittingTf == null)
			return;
		if (a.activeTween != null && a.activeTween.IsActive())
			return;

		a.nextHopTime = now + Random.Range(settings.hopIntervalMin, settings.hopIntervalMax);

		Quaternion baseRot = a.zone.GetSlotWorldRotation(a.slot);
		Quaternion newRot = baseRot * Quaternion.Euler(0f, Random.Range(-90f, 90f), 0f);

		if (Random.value < settings.rotateOnlyChance)
		{
			// Только оглядеться
			a.activeTween = a.sittingTf.DORotateQuaternion(newRot, settings.hopDuration);
			return;
		}

		// Прыжок в пределах своей ячейки слота — соседи не клипаются, перебронь не нужна
		float maxOffset = Mathf.Max(0f, (a.zone.SlotSpacing - settings.birdBodyDiameter) * 0.5f);
		Vector2 rnd = Random.insideUnitCircle * maxOffset;
		// Смещение — в плоскости прямоугольника (на скате крыши прыжок идёт по скату); поворот птицы — только yaw
		Vector3 slotCenter = a.zone.GetSlotWorldPos(a.slot);
		Quaternion rectRot = a.zone.GetRectWorldRotation(a.zone.GetSlotRect(a.slot));
		Vector3 targetPos = slotCenter + rectRot * new Vector3(rnd.x, 0f, rnd.y);

		Sequence seq = DOTween.Sequence();
		seq.Append(a.sittingTf.DOJump(targetPos, settings.hopHeight, 1, settings.hopDuration));
		seq.Join(a.sittingTf.DORotateQuaternion(newRot, settings.hopDuration));
		a.activeTween = seq;
	}

	// ================== Испуг ==================

	public void Scare(Vector3 worldPos, float radius)
	{
		if (isShuttingDown)
			return;

		float now = Time.time;

		// Отметить жар на зонах в радиусе
		foreach (BirdPerchZone zone in BirdPerchZone.ActiveZones)
		{
			float dist = Vector3.Distance(zone.WorldCenter, worldPos);
			if (dist > radius + zone.BoundingRadius)
				continue;
			zone.lastScareTime = now;
			zone.scareHeat += 1f;
			if (zone.scareHeat >= settings.heatAbandonThreshold)
				zone.abandonedUntil = now + settings.abandonDuration;
		}

		// Собрать напуганных сидящих (и заходящих на посадку) птиц
		agentBuffer.Clear();
		foreach (BirdAgent a in agents)
		{
			Vector3 birdPos = GetAgentWorldPos(a);
			if (Vector3.Distance(birdPos, worldPos) > radius)
				continue;

			switch (a.state)
			{
				case BirdAgent.BirdState.Sitting:
					agentBuffer.Add(a);
					break;
				// TakingOff: уже улетает — не трогаем
				case BirdAgent.BirdState.Landing:
				case BirdAgent.BirdState.Touchdown:
					if (a.flock == null || now >= a.flock.ignoreScaresUntil)
						AbortLanding(a);
					break;
			}
		}

		if (agentBuffer.Count > 0)
			LaunchFlock(agentBuffer, worldPos, now);
	}

	private void LaunchFlock(List<BirdAgent> scared, Vector3 threatPos, float now)
	{
		var flock = new BirdFlock
		{
			phase = BirdFlock.Phase.Ascend,
			threatPos = threatPos,
			ignoreScaresUntil = now + settings.flockScareImmunity,
			cruiseAltitude = Random.Range(settings.cruiseAltitudeMin, settings.cruiseAltitudeMax)
		};

		Vector3 centroid = Vector3.zero;
		foreach (BirdAgent a in scared)
			centroid += GetAgentWorldPos(a);
		centroid /= scared.Count;
		flock.baseY = centroid.y;

		Vector3 away = centroid - threatPos;
		away.y = 0f;
		away = away.sqrMagnitude > 0.01f ? away.normalized : Random.insideUnitSphere.WithY(0f).normalized;

		flock.attractor = centroid;
		flock.waypoints.Add(centroid + Vector3.up * 2f + away * 3f);
		flock.waypoints.Add(centroid + away * 10f + Vector3.up * flock.cruiseAltitude);
		flock.wpIndex = 0;

		foreach (BirdAgent a in scared)
		{
			a.KillTween();
			a.flock = flock;
			flock.members.Add(a);
			a.state = BirdAgent.BirdState.TakingOff;
			a.releaseAt = now + Random.Range(0f, settings.takeoffStaggerMax);
		}

		flocks.Add(flock);
	}

	private void DoTakeoff(BirdAgent a, float now)
	{
		// Слот освобождаем сразу
		if (a.zone != null && a.slot >= 0)
			a.zone.ReleaseSlot(a.slot);

		Vector3 sitPos = a.sittingTf != null ? a.sittingTf.position : GetAgentWorldPos(a);
		GameAudio.Play(GameSound.BirdTakeoff, sitPos);
		a.pos = sitPos + Vector3.up * settings.flyingPivotYOffset;

		Vector3 away = a.flock != null ? (a.pos - a.flock.threatPos).WithY(0f) : Vector3.zero;
		away = away.sqrMagnitude > 0.01f ? away.normalized : Random.insideUnitCircle.ToXZ().normalized;
		a.vel = Vector3.up * settings.takeoffUpSpeed
			+ away * settings.takeoffAwaySpeed
			+ Random.insideUnitSphere * 0.5f;
		a.prevFlatVel = a.vel.WithY(0f);
		a.roll = 0f;
		a.scareBoostUntil = now + settings.scareBoostDuration;

		SwapToFlying(a);

		a.zone = null;
		a.slot = -1;
		a.state = BirdAgent.BirdState.Flying;
	}

	// ================== Стаи ==================

	private void UpdateFlock(BirdFlock flock, float dt, float now)
	{
		switch (flock.phase)
		{
			case BirdFlock.Phase.Ascend:
				MoveAttractor(flock, dt, settings.cruiseSpeed * settings.attractorSpeedFactor);
				if (flock.wpIndex >= flock.waypoints.Count)
				{
					if (!TryStartCruise(flock))
						StartLoiter(flock, now);
				}
				break;

			case BirdFlock.Phase.Cruise:
				MoveAttractor(flock, dt, settings.cruiseSpeed * settings.attractorSpeedFactor);
				if (flock.targetZone == null || !IsZoneLandable(flock.targetZone, now))
				{
					// Цель пропала/испугана по пути — перевыбрать
					if (!TryStartCruise(flock))
						StartLoiter(flock, now);
					break;
				}
				if (flock.wpIndex >= flock.waypoints.Count)
					StartDescend(flock);
				break;

			case BirdFlock.Phase.Descend:
				UpdateDescend(flock, dt, now);
				break;

			case BirdFlock.Phase.Loiter:
				UpdateLoiter(flock, dt, now);
				break;

			case BirdFlock.Phase.Leave:
				MoveAttractor(flock, dt, settings.cruiseSpeed * settings.attractorSpeedFactor);
				for (int i = flock.members.Count - 1; i >= 0; i--)
				{
					BirdAgent a = flock.members[i];
					if (Vector3.Distance(a.pos, flock.leaveOrigin) > settings.leaveKillDistance)
						DespawnAgent(a);
				}
				if (flock.members.Count == 0)
					SchedulePendingReturn(now);
				break;
		}
	}

	private void MoveAttractor(BirdFlock flock, float dt, float speed)
	{
		if (flock.wpIndex >= flock.waypoints.Count)
			return;
		Vector3 target = flock.waypoints[flock.wpIndex];
		flock.attractor = Vector3.MoveTowards(flock.attractor, target, speed * dt);
		if (Vector3.Distance(flock.attractor, target) < 2f)
			flock.wpIndex++;
	}

	private bool TryStartCruise(BirdFlock flock)
	{
		BirdPerchZone target = PickTargetZone(flock);
		if (target == null)
			return false;

		flock.targetZone = target;
		flock.phase = BirdFlock.Phase.Cruise;
		flock.waypoints.Clear();
		flock.wpIndex = 0;

		Vector3 from = flock.attractor;
		Vector3 to = target.WorldCenter + Vector3.up * 2f;
		Vector3 cruiseTo = new Vector3(to.x, flock.baseY + flock.cruiseAltitude, to.z);

		// Дуга: промежуточная точка с боковым отклонением
		Vector3 mid = (from + cruiseTo) * 0.5f;
		Vector3 dir = (cruiseTo - from).WithY(0f);
		if (dir.sqrMagnitude > 1f)
		{
			Vector3 side = Vector3.Cross(Vector3.up, dir.normalized);
			float lateral = dir.magnitude * Mathf.Tan(Random.Range(15f, 30f) * Mathf.Deg2Rad) * (Random.value < 0.5f ? -1f : 1f) * 0.5f;
			mid += side * lateral;
		}
		mid.y = flock.baseY + flock.cruiseAltitude;

		flock.waypoints.Add(mid);
		flock.waypoints.Add(cruiseTo);
		return true;
	}

	private void StartDescend(BirdFlock flock)
	{
		flock.phase = BirdFlock.Phase.Descend;
		flock.waypoints.Clear();
		flock.wpIndex = 0;
		flock.waypoints.Add(flock.targetZone.WorldCenter + Vector3.up * 2f);
	}

	private void UpdateDescend(BirdFlock flock, float dt, float now)
	{
		BirdPerchZone zone = flock.targetZone;
		if (!IsZoneLandable(zone, now))
		{
			// Зона пропала или её только что испугали — перенацелить всю стаю
			if (!TryStartCruise(flock))
				StartLoiter(flock, now);
			return;
		}

		// Аттрактор замедляется у зоны и висит над ней
		Vector3 target = zone.WorldCenter + Vector3.up * 2f;
		float dist = Vector3.Distance(flock.attractor, target);
		float speed = Mathf.Lerp(2f, settings.cruiseSpeed * settings.attractorSpeedFactor, Mathf.Clamp01(dist / 10f));
		flock.attractor = Vector3.MoveTowards(flock.attractor, target, speed * dt);

		// Отпускать птиц на посадку по одной, со стаггером
		if (Vector3.Distance(flock.Centroid, zone.WorldCenter) > settings.descendReleaseDistance)
			return;

		float nextRelease = now;
		foreach (BirdAgent a in flock.members)
		{
			if (a.state != BirdAgent.BirdState.Flying)
				continue;
			if (a.releaseAt > now)   // уже назначен на посадку позже
				continue;

			if (!zone.TryReserveSlot(a.pos, a, out int slot))
			{
				// Зона забита — остаток стаи перенацелить
				if (!TryStartCruise(flock))
					StartLoiter(flock, now);
				return;
			}
			a.zone = zone;
			a.slot = slot;
			nextRelease += Random.Range(settings.landingStaggerMin, settings.landingStaggerMax);
			a.releaseAt = nextRelease;
			a.state = BirdAgent.BirdState.Landing;
		}
	}

	private void StartLoiter(BirdFlock flock, float now)
	{
		flock.phase = BirdFlock.Phase.Loiter;
		flock.targetZone = null;
		flock.loiterStartTime = now;
		flock.loiterCenter = new Vector3(flock.attractor.x, flock.baseY + flock.cruiseAltitude, flock.attractor.z);
		flock.loiterAngle = 0f;
	}

	private void UpdateLoiter(BirdFlock flock, float dt, float now)
	{
		float radius = settings.loiterRadius;
		flock.loiterAngle += (settings.cruiseSpeed * 0.8f / radius) * dt;
		flock.attractor = flock.loiterCenter
			+ new Vector3(Mathf.Cos(flock.loiterAngle), 0f, Mathf.Sin(flock.loiterAngle)) * radius;

		// Периодически пробуем найти зону
		if (TryStartCruise(flock))
			return;

		if (now - flock.loiterStartTime > settings.loiterTimeout)
		{
			flock.phase = BirdFlock.Phase.Leave;
			flock.leaveOrigin = flock.attractor;
			flock.waypoints.Clear();
			flock.wpIndex = 0;
			Vector3 dir = Random.insideUnitCircle.ToXZ().normalized;
			flock.waypoints.Add(flock.attractor + dir * (settings.leaveKillDistance + 20f) + Vector3.up * 5f);
			foreach (BirdAgent a in flock.members)
				a.state = BirdAgent.BirdState.Leaving;
		}
	}

	private BirdPerchZone PickTargetZone(BirdFlock flock)
	{
		float now = Time.time;
		Vector3 centroid = flock.Centroid;
		BirdPerchZone best = null;
		float bestScore = float.NegativeInfinity;

		foreach (BirdPerchZone zone in BirdPerchZone.ActiveZones)
		{
			if (!IsZoneLandable(zone, now) || zone.FreeSlotCount == 0)
				continue;
			float score = Vector3.Distance(zone.WorldCenter, flock.threatPos)
				- 0.5f * Vector3.Distance(zone.WorldCenter, centroid);
			if (score > bestScore)
			{
				bestScore = score;
				best = zone;
			}
		}
		return best;
	}

	private void DecayZoneHeat(float dt)
	{
		foreach (BirdPerchZone zone in BirdPerchZone.ActiveZones)
		{
			if (zone.scareHeat > 0f)
				zone.scareHeat = Mathf.Max(0f, zone.scareHeat - dt / settings.heatDecayInterval);
		}
	}

	private bool IsZoneLandable(BirdPerchZone zone, float now)
	{
		return zone != null
			&& zone.isActiveAndEnabled
			&& now - zone.lastScareTime > settings.calmDelay
			&& zone.scareHeat < settings.heatAbandonThreshold
			&& now > zone.abandonedUntil;
	}

	// ================== Полёт ==================

	private void UpdateFlying(BirdAgent a, float dt, float now)
	{
		BirdFlock flock = a.flock;
		Vector3 steer = Vector3.zero;

		// Боидные силы по соседям в стае
		if (flock != null)
		{
			Vector3 sep = Vector3.zero, align = Vector3.zero, coh = Vector3.zero;
			int alignCount = 0, cohCount = 0;
			foreach (BirdAgent other in flock.members)
			{
				if (other == a || other.state == BirdAgent.BirdState.Touchdown)
					continue;
				Vector3 diff = a.pos - other.pos;
				float dist = diff.magnitude;
				if (dist < 0.001f)
					continue;
				if (dist < settings.separationRadius)
					sep += diff / (dist * dist);
				if (dist < settings.alignmentRadius) { align += other.vel; alignCount++; }
				if (dist < settings.cohesionRadius) { coh += other.pos; cohCount++; }
			}
			steer += sep * settings.separationWeight;
			if (alignCount > 0)
				steer += ((align / alignCount).normalized * settings.cruiseSpeed - a.vel) * (settings.alignmentWeight / settings.cruiseSpeed);
			if (cohCount > 0)
				steer += ((coh / cohCount - a.pos).normalized * settings.cruiseSpeed - a.vel) * (settings.cohesionWeight / settings.cruiseSpeed);

			// Стремление к аттрактору
			Vector3 desired = (flock.attractor - a.pos).normalized * settings.cruiseSpeed;
			steer += (desired - a.vel) * (settings.seekWeight / settings.cruiseSpeed);

			// Не ниже минимальной высоты
			float minY = flock.baseY + settings.minAltitude;
			if (a.pos.y < minY)
				steer += Vector3.up * ((minY - a.pos.y) * settings.groundAvoidWeight);
		}

		// Блуждание (Перлин, у каждой птицы своя фаза)
		float t = now * 0.5f;
		steer.x += (Mathf.PerlinNoise(a.wanderSeed, t) * 2f - 1f) * settings.wanderWeight * settings.maxSteerAccel * 0.3f;
		steer.z += (Mathf.PerlinNoise(a.wanderSeed + 71.3f, t) * 2f - 1f) * settings.wanderWeight * settings.maxSteerAccel * 0.3f;

		IntegrateAndOrient(a, steer, dt, now);
	}

	private void UpdateLanding(BirdAgent a, float dt, float now)
	{
		if (now < a.releaseAt)
		{
			UpdateFlying(a, dt, now);   // ещё в стае, ждёт очереди
			return;
		}

		if (a.zone == null || !a.zone.isActiveAndEnabled)
		{
			AbortLanding(a);
			return;
		}

		Vector3 hover = a.zone.GetSlotWorldPos(a.slot)
			+ Vector3.up * (settings.hoverHeight + settings.flyingPivotYOffset);
		Vector3 toTarget = hover - a.pos;
		float dist = toTarget.magnitude;

		// Условие тачдауна
		if (dist < settings.touchdownDistance && a.vel.magnitude < settings.touchdownMaxSpeed)
		{
			StartTouchdown(a);
			return;
		}

		float desiredSpeed = Mathf.Lerp(settings.landingMinSpeed, settings.cruiseSpeed,
			Mathf.Clamp01(dist / settings.approachSlowRadius));
		Vector3 steer = ((toTarget / Mathf.Max(dist, 0.001f)) * desiredSpeed - a.vel) * 2f;

		// Разделение с другими птицами стаи (чтобы не сталкиваться на подлёте)
		if (a.flock != null)
		{
			Vector3 sep = Vector3.zero;
			foreach (BirdAgent other in a.flock.members)
			{
				if (other == a || other.state == BirdAgent.BirdState.Touchdown)
					continue;
				Vector3 diff = a.pos - other.pos;
				float d = diff.magnitude;
				if (d > 0.001f && d < settings.separationRadius)
					sep += diff / (d * d);
			}
			steer += sep * settings.separationWeight;
		}

		IntegrateAndOrient(a, steer, dt, now, clampMinSpeed: false);
	}

	private void IntegrateAndOrient(BirdAgent a, Vector3 steer, float dt, float now, bool clampMinSpeed = true)
	{
		steer = Vector3.ClampMagnitude(steer, settings.maxSteerAccel);
		a.vel += steer * dt;

		float maxSpd = now < a.scareBoostUntil ? settings.scareBoostSpeed : settings.maxSpeed;
		float spd = a.vel.magnitude;
		if (spd > maxSpd)
			a.vel *= maxSpd / spd;
		else if (clampMinSpeed && spd < settings.minSpeed && spd > 0.001f)
			a.vel *= settings.minSpeed / spd;
		a.vel.y = Mathf.Clamp(a.vel.y, -settings.maxSinkSpeed, settings.maxClimbSpeed);

		a.pos += a.vel * dt;

		// Ориентация: не интегрируется, а выводится — переворот невозможен
		Vector3 flat = a.vel.WithY(0f);
		if (flat.sqrMagnitude > 0.0001f && a.prevFlatVel.sqrMagnitude > 0.0001f)
		{
			float yawRate = Vector3.SignedAngle(a.prevFlatVel, flat, Vector3.up) / Mathf.Max(dt, 0.0001f);
			float targetRoll = Mathf.Clamp(-yawRate * settings.bankFactor, -settings.maxBankAngle, settings.maxBankAngle);
			a.roll = Mathf.LerpAngle(a.roll, targetRoll, 1f - Mathf.Exp(-settings.rollSmoothing * dt));
		}
		a.prevFlatVel = flat;

		if (a.flyingTf != null && a.vel.sqrMagnitude > 0.0001f)
		{
			Quaternion look = Quaternion.LookRotation(a.vel.normalized, Vector3.up);
			a.flyingTf.SetPositionAndRotation(a.pos, look * Quaternion.Euler(0f, 0f, a.roll));
		}
	}

	// ================== Посадка ==================

	private void StartTouchdown(BirdAgent a)
	{
		a.state = BirdAgent.BirdState.Touchdown;
		a.swapStarted = false;
		a.touchdownStartPos = a.pos;
		a.touchdownStartRot = a.flyingTf != null ? a.flyingTf.rotation : Quaternion.identity;
		a.KillTween();

		Vector3 endPos = a.zone.GetSlotWorldPos(a.slot) + Vector3.up * settings.flyingPivotYOffset;
		Quaternion endRot = a.zone.GetSlotWorldRotation(a.slot);
		float duration = settings.touchdownDuration;
		float swapAt = 1f - Mathf.Clamp01(settings.swapDuration * 0.7f / duration);

		float tv = 0f;
		a.activeTween = DOTween.To(() => tv, v =>
		{
			tv = v;
			if (a.flyingTf == null)
				return;
			// Затухающая дуга вниз: твин не может промахнуться мимо слота
			Vector3 p = Vector3.Lerp(a.touchdownStartPos, endPos, tv);
			p.y += Mathf.Sin(tv * Mathf.PI) * 0.12f;
			Quaternion r = Quaternion.Slerp(a.touchdownStartRot, endRot, tv);
			a.pos = p;
			a.flyingTf.SetPositionAndRotation(p, r);

			if (!a.swapStarted && tv >= swapAt)
			{
				a.swapStarted = true;
				SwapToSitting(a);
			}
		}, 1f, duration).SetEase(Ease.InOutSine).OnComplete(() => FinishLanding(a));
	}

	private void FinishLanding(BirdAgent a)
	{
		a.activeTween = null;
		if (a.zone == null || !a.zone.isActiveAndEnabled)
		{
			AbortLanding(a);
			return;
		}
		if (!a.swapStarted)
			SwapToSitting(a);

		a.zone.OccupySlot(a.slot, a);
		if (a.flock != null)
		{
			a.flock.members.Remove(a);
			a.flock = null;
		}
		a.state = BirdAgent.BirdState.Sitting;
		a.nextHopTime = Time.time + Random.Range(settings.hopIntervalMin, settings.hopIntervalMax);
	}

	private void AbortLanding(BirdAgent a)
	{
		a.KillTween();
		if (a.zone != null && a.slot >= 0)
			a.zone.ReleaseSlot(a.slot);
		a.zone = null;
		a.slot = -1;
		a.releaseAt = 0f;
		a.swapStarted = false;

		// Если сидящая модель уже начала появляться — убрать
		if (a.sittingTf != null)
		{
			DespawnPooled(a.sittingTf);
			a.sittingTf = null;
		}
		if (a.flyingTf == null)
		{
			a.flyingTf = SpawnModel(settings.flyingPrefab, a.pos, Quaternion.identity, a.species);
		}
		else
		{
			a.flyingTf.DOKill();
			a.flyingTf.localScale = Vector3.one;
		}

		a.state = BirdAgent.BirdState.Flying;
		if (a.flock == null)
		{
			// Птица без стаи (стая уже распалась) — одиночная «стая», сама найдёт себе зону
			var flock = new BirdFlock
			{
				phase = BirdFlock.Phase.Ascend,
				threatPos = a.pos,
				ignoreScaresUntil = Time.time + settings.flockScareImmunity,
				cruiseAltitude = Random.Range(settings.cruiseAltitudeMin, settings.cruiseAltitudeMax),
				baseY = a.pos.y,
				attractor = a.pos
			};
			flock.waypoints.Add(a.pos + Vector3.up * 3f);
			flock.wpIndex = 0;
			a.flock = flock;
			flock.members.Add(a);
			flocks.Add(flock);
		}
		// Если стая жива — птица просто снова летит в ней; ретаргет решает логика стаи
	}

	// ================== Свап моделей ==================

	private void SwapToFlying(BirdAgent a)
	{
		a.flyingTf = SpawnModel(settings.flyingPrefab, a.pos, Quaternion.LookRotation(a.vel.WithY(0f).normalized, Vector3.up), a.species);
		a.flyingTf.localScale = Vector3.zero;
		a.flyingTf.DOScale(1f, settings.swapDuration);

		Transform sitting = a.sittingTf;
		a.sittingTf = null;
		if (sitting != null)
		{
			sitting.DOKill();
			sitting.DOScale(0f, settings.swapDuration).OnComplete(() => DespawnPooled(sitting));
		}
	}

	private void SwapToSitting(BirdAgent a)
	{
		Vector3 slotPos = a.zone.GetSlotWorldPos(a.slot);
		Quaternion slotRot = a.zone.GetSlotWorldRotation(a.slot);
		a.sittingTf = SpawnModel(settings.sittingPrefab, slotPos, slotRot, a.species);
		a.sittingTf.localScale = Vector3.zero;
		a.sittingTf.DOScale(1f, settings.swapDuration);

		Transform flying = a.flyingTf;
		if (flying != null)
		{
			// Скейл убирается свапом; позицию продолжает вести тачдаун-твин
			flying.DOScale(0f, settings.swapDuration).OnComplete(() =>
			{
				if (a.flyingTf == flying)
					a.flyingTf = null;
				DespawnPooled(flying);
			});
		}
	}

	// ================== Триггеры ==================

	private void OnShotFired(Vector3 pos)
	{
		Scare(pos, settings.shotScareRadius);
	}

	private void PollUnits(float now)
	{
		if (gameController == null || now < nextUnitPollTime)
			return;
		nextUnitPollTime = now + settings.unitPollInterval;

		List<Unit> enemies = gameController.GetAllEnemies();
		if (enemies == null || enemies.Count == 0)
			return;

		foreach (BirdPerchZone zone in BirdPerchZone.ActiveZones)
		{
			if (!ZoneHasSeatedBirds(zone))
				continue;
			Vector3 center = zone.WorldCenter;
			float radius = settings.unitScareDistance + zone.BoundingRadius;
			float radiusSqr = radius * radius;
			foreach (Unit unit in enemies)
			{
				if (unit == null || unit.isDead)
					continue;
				if ((unit.transform.position - center).sqrMagnitude < radiusSqr)
				{
					Scare(unit.transform.position, settings.unitScareDistance + zone.BoundingRadius);
					break;
				}
			}
		}
	}

	private bool ZoneHasSeatedBirds(BirdPerchZone zone)
	{
		foreach (BirdAgent a in agents)
			if (a.zone == zone && a.state == BirdAgent.BirdState.Sitting)
				return true;
		return false;
	}

	private void OnZoneEnabled(BirdPerchZone zone)
	{
		if (isShuttingDown || settings == null)
			return;

		// Зона вернулась вместе с островом (см. ProcessDisabledZones) — птицы всё ещё сидят на её
		// слотах, перестраивать слоты и досылать стаю не нужно
		if (ZoneHasBoundBirds(zone))
		{
			disabledZones.Remove(zone);
			return;
		}

		zone.RebuildSlots(settings.slotSpacing);

		// Первые секунды после старта (загрузка хаба, Addressables-регионы) — население мгновенно,
		// дальше в рантайме — птицы прилетают на новую зону стайкой
		if (Time.time - startTime < 3f)
		{
			PopulateZone(zone);
			return;
		}
		// Зона с нулевым населением по настройкам — пустая площадка, стайку на неё не шлём
		int designed = zone.DesignedPopulationCount;
		if (designed <= 0)
			return;
		pendingReturns.Add(new PendingReturn
		{
			dueTime = Time.time + Random.Range(5f, 15f),
			count = Mathf.Max(settings.returnFlockSizeMin, designed)
		});
	}

	private void OnZoneDisabled(BirdPerchZone zone)
	{
		if (isShuttingDown || settings == null)
			return;
		if (!disabledZones.Contains(zone))
			disabledZones.Add(zone);
	}

	/// <summary>
	/// Отложенная реакция на выключение зон. Если весь остров (и менеджер с ним) выключили,
	/// Update не крутится, а при включении зоны уже снова активны — их пропускаем,
	/// птицы остаются на местах. Зона, погасшая сама по себе, пугает своих птиц как раньше.
	/// </summary>
	private void ProcessDisabledZones(float now)
	{
		if (disabledZones.Count == 0)
			return;
		for (int i = 0; i < disabledZones.Count; i++)
		{
			BirdPerchZone zone = disabledZones[i];
			if (zone != null && zone.isActiveAndEnabled)
				continue;
			ScareZoneBirds(zone, now);
		}
		disabledZones.Clear();
	}

	private void ScareZoneBirds(BirdPerchZone zone, float now)
	{
		// Заходящие на посадку — прервать, сидящих — спугнуть
		agentBuffer.Clear();
		foreach (BirdAgent a in agents)
		{
			if (!ReferenceEquals(a.zone, zone))
				continue;
			switch (a.state)
			{
				case BirdAgent.BirdState.Landing:
				case BirdAgent.BirdState.Touchdown:
					AbortLanding(a);
					break;
				case BirdAgent.BirdState.Sitting:
				case BirdAgent.BirdState.TakingOff:
					agentBuffer.Add(a);
					break;
			}
		}
		if (agentBuffer.Count == 0)
			return;

		// Зона могла быть уже уничтожена — тогда угроза «из центра стаи», направление разлёта случайное
		Vector3 threat;
		if (zone != null)
			threat = zone.WorldCenter;
		else
		{
			threat = Vector3.zero;
			foreach (BirdAgent a in agentBuffer)
				threat += GetAgentWorldPos(a);
			threat /= agentBuffer.Count;
		}
		LaunchFlock(agentBuffer, threat, now);
	}

	private bool ZoneHasBoundBirds(BirdPerchZone zone)
	{
		foreach (BirdAgent a in agents)
			if (ReferenceEquals(a.zone, zone))
				return true;
		return false;
	}

	// ================== Возврат стай ==================

	private void SchedulePendingReturn(float now)
	{
		pendingReturns.Add(new PendingReturn
		{
			dueTime = now + Random.Range(settings.returnDelayMin, settings.returnDelayMax),
			count = Random.Range(settings.returnFlockSizeMin, settings.returnFlockSizeMax + 1)
		});
	}

	private void ProcessPendingReturns(float now)
	{
		for (int i = pendingReturns.Count - 1; i >= 0; i--)
		{
			if (now < pendingReturns[i].dueTime)
				continue;

			PendingReturn ret = pendingReturns[i];
			BirdPerchZone zone = PickCalmZoneWithSlots(now);
			if (zone == null)
			{
				// Пока некуда — попробуем позже
				ret.dueTime = now + 20f;
				pendingReturns[i] = ret;
				continue;
			}
			pendingReturns.RemoveAt(i);
			SpawnReturnFlock(zone, ret.count);
		}
	}

	private BirdPerchZone PickCalmZoneWithSlots(float now)
	{
		BirdPerchZone best = null;
		int bestFree = 0;
		foreach (BirdPerchZone zone in BirdPerchZone.ActiveZones)
		{
			if (!IsZoneLandable(zone, now))
				continue;
			int free = zone.FreeSlotCount;
			if (free > bestFree)
			{
				bestFree = free;
				best = zone;
			}
		}
		return best;
	}

	private void SpawnReturnFlock(BirdPerchZone zone, int count)
	{
		count = Mathf.Min(count, zone.FreeSlotCount);
		if (count <= 0)
			return;

		var flock = new BirdFlock
		{
			phase = BirdFlock.Phase.Cruise,
			targetZone = zone,
			threatPos = zone.WorldCenter,   // не используется для возврата
			ignoreScaresUntil = Time.time + settings.flockScareImmunity,
			cruiseAltitude = Random.Range(settings.cruiseAltitudeMin, settings.cruiseAltitudeMax),
			baseY = zone.WorldCenter.y
		};

		Vector3 dir = Random.insideUnitCircle.ToXZ().normalized;
		Vector3 spawnCenter = zone.WorldCenter + dir * 35f + Vector3.up * flock.cruiseAltitude;
		flock.attractor = spawnCenter;
		flock.waypoints.Add(zone.WorldCenter + Vector3.up * (flock.cruiseAltitude * 0.6f));
		flock.wpIndex = 0;

		for (int i = 0; i < count; i++)
		{
			var agent = new BirdAgent
			{
				state = BirdAgent.BirdState.Flying,
				flock = flock,
				wanderSeed = Random.Range(0f, 1000f),
				pos = spawnCenter + Random.insideUnitSphere * 2f,
				vel = -dir * settings.cruiseSpeed
			};
			agent.prevFlatVel = agent.vel.WithY(0f);
			agent.species = PickSpecies(zone);
			agent.flyingTf = SpawnModel(settings.flyingPrefab, agent.pos, Quaternion.LookRotation(-dir, Vector3.up), agent.species);
			flock.members.Add(agent);
			agents.Add(agent);
		}
		flocks.Add(flock);
	}

	// ================== Виды / окрас ==================

	/// <summary>Случайный вид для птицы зоны: список зоны, если задан, иначе виды по умолчанию из BirdsSettings.</summary>
	private BirdSpecies PickSpecies(BirdPerchZone zone)
	{
		List<BirdSpeciesWeight> list = zone != null && zone.Species.Count > 0 ? zone.Species : settings.species;
		return BirdSpeciesWeight.Pick(list);
	}

	/// <summary>Спавн модели птицы из пула с материалом окраса её вида.</summary>
	private Transform SpawnModel(GameObject prefab, Vector3 pos, Quaternion rot, BirdSpecies species)
	{
		Transform tf = SpawnPooled(prefab, pos, rot);
		if (!rendererCache.TryGetValue(tf, out Renderer renderer))
		{
			renderer = tf.GetComponentInChildren<Renderer>();
			rendererCache[tf] = renderer;
		}
		if (renderer != null)
		{
			// Инстанс из пула мог остаться с чужим окрасом — материал ставится всегда
			Material mat = GetSpeciesMaterial(prefab, renderer, species);
			if (renderer.sharedMaterial != mat)
				renderer.sharedMaterial = mat;
		}
		return tf;
	}

	private Material GetSpeciesMaterial(GameObject prefab, Renderer instanceRenderer, BirdSpecies species)
	{
		if (!baseMaterials.TryGetValue(prefab, out Material baseMat))
		{
			Renderer prefabRenderer = prefab.GetComponentInChildren<Renderer>();
			baseMat = prefabRenderer != null ? prefabRenderer.sharedMaterial : instanceRenderer.sharedMaterial;
			baseMaterials[prefab] = baseMat;
		}
		if (species == null)
			return baseMat;

		if (!speciesMaterials.TryGetValue((prefab, species), out Material mat))
		{
			mat = new Material(baseMat) { name = baseMat.name + "_" + species.name };
			mat.SetColor(BodyColorId, species.body);
			mat.SetColor(BellyColorId, species.belly);
			speciesMaterials[(prefab, species)] = mat;
			createdMaterials.Add(mat);
		}
		return mat;
	}

	// ================== Пул / утилиты ==================

	private Transform SpawnPooled(GameObject prefab, Vector3 pos, Quaternion rot)
	{
		GameObject go = SimplePool.Spawn(prefab, pos, rot);
		go.transform.localScale = Vector3.one;   // SimplePool не сбрасывает состояние
		return go.transform;
	}

	private void DespawnPooled(Transform tf)
	{
		if (tf == null)
			return;
		tf.DOKill();
		tf.localScale = Vector3.one;
		SimplePool.Despawn(tf.gameObject);
	}

	private void DespawnAgent(BirdAgent a)
	{
		a.KillTween();
		if (a.zone != null && a.slot >= 0)
			a.zone.ReleaseSlot(a.slot);
		DespawnPooled(a.sittingTf);
		DespawnPooled(a.flyingTf);
		a.sittingTf = null;
		a.flyingTf = null;
		if (a.flock != null)
			a.flock.members.Remove(a);
		agents.Remove(a);
	}

	private Vector3 GetAgentWorldPos(BirdAgent a)
	{
		if (a.state == BirdAgent.BirdState.Sitting || a.state == BirdAgent.BirdState.TakingOff)
			return a.sittingTf != null ? a.sittingTf.position : a.pos;
		return a.pos;
	}

	// ================== Дебаг ==================

	[Button(nameof(DebugScareAll))] public bool debugScareAllButton;

	public void DebugScareAll()
	{
		foreach (BirdPerchZone zone in BirdPerchZone.ActiveZones)
			Scare(zone.WorldCenter, zone.BoundingRadius + 1f);
	}
}

/// <summary>Мелкие векторные расширения для птиц.</summary>
public static class BirdVectorExtensions
{
	public static Vector3 WithY(this Vector3 v, float y)
	{
		v.y = y;
		return v;
	}

	public static Vector3 ToXZ(this Vector2 v) => new Vector3(v.x, 0f, v.y);
}
