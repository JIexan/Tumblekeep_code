using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// Результат генерации: волна + тайминги. Пауза до следующей волны отвязана от окна боя.
public class WaveGenResult {
	public Wave wave;
	public float expectedKillTime;
	public float pauseTime;
	public bool isSurge;
}

// По каким маршрутам пойдёт волна. Решается заранее — когда уплыл корабль прошлой волны, — чтобы показать игроку,
// откуда ждать врага (панель таймера встаёт на нужный рукав). Сам пул считается позже, по башням на старте волны
public class WavePlan {
	public int waveId;
	public int[] routes; // один маршрут — одиночная волна, два — двойная (идёт по обоим рукавам сразу)
	public bool IsSplit => routes.Length > 1;
}

// Генератор бесконечных волн. Состав берётся из фракций (FactionSettings): уровень задаёт список
// допустимых фракций и длительность каждой, порядок — случайный «мешок» без повтора на стыке,
// после исчерпания мешок наполняется заново. Соседние фракции перекрываются на transitionWaves волн
// с линейным кроссфейдом весов, чтобы смена состава была плавной, а не мешаниной.
public class WavesGenerator : MonoBehaviour
{
	// Промежуточные данные распределения бюджета ХП по типам юнитов
	private class UnitAllocation {
		public FactionSettings.UnitConfig config;
		public Unit unit;
		public float hpMultiplier;  // множитель ХП фракции
		public float effectiveHP;   // стоимость одного тела в бюджете: CombinedHP × множитель / резист-фактор
		public float allocatedHP;
		public int count;
		public float fraction;
	}

	private class UnitWeightData {
		public UnitType unitType;
		public float weight;
		public FactionSettings.UnitConfig config;
		public float hpMultiplier = 1f;
	}

	[System.Serializable]
	public class LevelFaction {
		public FactionSettings faction;
		[Tooltip("Сколько волн фракция активна (включая переходы с соседями).")]
		public int durationWaves = 8;
	}

	// Фракция, поставленная в расписание: окно волн [startWave, EndWave)
	private class ScheduledFaction {
		public LevelFaction entry;
		public int startWave;
		public int duration; // durationWaves (+ firstFactionExtraWaves у первой фракции катки)
		public int cycle;    // номер круга мешка, 0 — первый
		public int ordinal;  // порядковый номер в расписании уровня, 0 — первая фракция
		public int EndWave => startWave + duration;
	}

	private struct ActiveFaction {
		public ScheduledFaction scheduled;
		public float multiplier; // кроссфейд 0..1
	}

	// Куда и как выходит часть волны: маршрут, сторона дороги и набор башен, против которого она собирается
	private class RouteSlot {
		public int routeIdx;
		public int spawnPointIdx;
		public int laneSide;       // 0 — полосы от центра дороги (одиночная волна), ±1 — рукав занимает свою сторону
		public int parallelGroups;
		public int unitCap;
		public float[] towerWeights; // параллельно GameController.GetAllTowers(); null — все башни целиком
		public Dictionary<UnitType, int> usedLimits; // общий на оба прохода двойной волны: лимит юнита — на волну, а не на рукав
		public string logTag;
	}

	[SerializeField] private WavesGeneratorSettings settings;
	[SerializeField] private LevelDesriptor levelDesriptor;
	[SerializeField] private BookUnitDescription unitsDescription;
	[SerializeField] private float routeCoverageContribution = 1f;
	[SerializeField] private int bossEveryNWaves = 5;

	[Header("Фракции уровня")]
	[SerializeField] private List<LevelFaction> factions = new List<LevelFaction>();
	[Tooltip("Длина перехода в волнах: столько последних волн уходящей фракции совпадают с первыми волнами следующей.")]
	[SerializeField] private int transitionWaves = 3;
	[Tooltip("Со второго круга мешка окна элиты сдвигаются к началу фракции (phaseStart × это), чтобы каждый круг не начинался с одного мусора.")]
	[Range(0f, 1f)] [SerializeField] private float repeatCyclePhaseFactor = 0.5f;
	[Tooltip("Первая фракция катки длится на столько волн дольше — спокойный старт.")]
	[SerializeField] private int firstFactionExtraWaves = 2;
	[Tooltip("Сдвиг порогов ярусов (WavesGeneratorSettings.tierNMinWave) для этого уровня: отрицательный открывает раньше. Сюда же позже подключится мета (уровень острова/хаб).")]
	[SerializeField] private int tierUnlockShift = 0;

	[Header("Маршруты")]
	[Tooltip("Маршруты LevelDesriptor.routs, по которым идут волны. Пусто — только маршрут 0. Два маршрута — развилка: волна идёт по одному из рукавов (чаще по слабо защищённому) или по обоим сразу; башни считаются только те, что достают до рукава.")]
	[SerializeField] private List<int> routes = new List<int>();

	public int TierUnlockShift {
		get => tierUnlockShift;
		set => tierUnlockShift = value;
	}

	private readonly List<ScheduledFaction> schedule = new List<ScheduledFaction>();
	private readonly List<LevelFaction> bag = new List<LevelFaction>();
	private LevelFaction lastPicked;
	private int cycleCounter;

	private WavePlan currentPlan;
	private int lastSingleRoute = -1;
	private int sameRouteStreak;

	// Уровень с одним маршрутом идёт старой веткой целиком: все башни, общий DPS, ни одного лишнего броска Random
	// (серии автоплея сидят на Random.InitState — сдвиг потока сломал бы их сравнимость)
	private bool IsMultiRoute => routes != null && routes.Count > 1;
	private int PrimaryRoute => routes != null && routes.Count > 0 ? routes[0] : 0;

#if UNITY_EDITOR
	private void OnValidate() {
		if (routes != null && routes.Count > 2)
			Debug.LogWarning("[WaveGen] Рассчитано на развилку из двух маршрутов: доли и стороны дороги двойной волны считаются для пары рукавов.", this);
	}
#endif

	// Настройки обязаны быть; фолбэк с дефолтами полей — чтобы уровень не падал без ассета.
	public WavesGeneratorSettings Settings {
		get {
			if (settings == null) {
				Debug.LogWarning("WavesGenerator: не назначен WavesGeneratorSettings, использую дефолты.");
				settings = ScriptableObject.CreateInstance<WavesGeneratorSettings>();
			}
			return settings;
		}
	}

	public bool IsSurgeWave(int waveId) {
		return Settings.surgePeriod > 0 && waveId > 0 && (waveId + 1) % Settings.surgePeriod == 0;
	}

	// difficultyFactor = базовый рост (1.1^успешных волн) × адаптивный pressure; клампится здесь.
	public WaveGenResult GenerateWave(int waveId, float difficultyFactor = 1f) {
		if (IsMultiRoute)
			return GenerateMultiRouteWave(waveId, difficultyFactor);

		var s = Settings;
		var res = new WaveGenResult();

		if (waveId == 0) {
			res.expectedKillTime = 10f;
			res.pauseTime = 10f;
			res.wave = CreateWave(s.firstWaveHpPool, waveId, SingleRouteSlot());
			return res;
		}

		bool isBossWave = bossEveryNWaves > 0 && (waveId + 1) % bossEveryNWaves == 0;
		UnitType bossType = UnitType.Pirate;
		if (isBossWave)
			bossType = PickBoss(waveId, bossType);

		bool isSurge = IsSurgeWave(waveId);
		float dps = GameController.Instance.TowersDPS;
		float pool;
		float engagement;
		float overload = Mathf.Clamp(difficultyFactor, s.minOverload, s.maxOverload);
		if (isSurge)
			overload *= s.surgeMultiplier; // намеренно поверх клампа — волна «за гранью»

		if (dps <= 0f) {
			// Башен нет — без делений на ноль и бесконечных пулов
			pool = s.firstWaveHpPool * overload;
			engagement = 10f;
		} else {
			float routeLength = levelDesriptor.GetRouteLength(PrimaryRoute);
			float unitsAverageSpeed = isBossWave ? GetUnitSpeed(bossType) : GetUnitsAverageSpeed(waveId);
			float coverage = Mathf.Clamp01(GameController.Instance.GetRouteCoverage(PrimaryRoute, routeLength) * routeCoverageContribution);
			// sqrt: покрытие влияет на окно боя мягко, иначе пул растёт суперлинейно с числом башен
			engagement = Mathf.Clamp(routeLength / unitsAverageSpeed * Mathf.Sqrt(coverage), s.minEngagementTime, s.maxEngagementTime);

			// Степень >1 держит рамп низким первые волны (время отстроиться) и разгоняет его к концу
			float earlyRamp = s.earlyRampWaves > 0
				? Mathf.Lerp(s.earlyRampStart, 1f, Mathf.Pow(Mathf.Clamp01((float)waveId / s.earlyRampWaves), s.earlyRampPower))
				: 1f;

			pool = dps * engagement * overload * s.poolSafetyFactor * earlyRamp;

			// Есть свободная точка и деньги на башню — игрок недоиспользует мощность, поджимаем
			if (GameController.Instance.CanBuildAnotherTower()) {
				pool += pool / Mathf.Max(GameController.Instance.TowersCount, 1f);
			}

			// Пол пула по номеру волны: при малом DPS (две башни в начале) волны иначе выходят из 2–3 тел.
			// Протечки (overload < 1) пол опускают, успехи не поднимают
			if (s.poolFloorBase > 0f) {
				float floor = s.poolFloorBase * (1f + s.poolFloorGrowthPerWave * waveId) * Mathf.Min(overload, 1f);
				pool = Mathf.Max(pool, floor);
			}
		}

		res.expectedKillTime = engagement;
		res.pauseTime = Mathf.Clamp(engagement * s.pauseFactor, s.minPause, s.maxPause);
		res.isSurge = isSurge;
		res.wave = isBossWave ? CreateBossWave(bossType, pool, waveId, SingleRouteSlot()) : CreateWave(pool, waveId, SingleRouteSlot());

		Debug.Log($"[WaveGen] wave {waveId + 1}: pool {pool:F0}, dps {dps:F1}, engagement {engagement:F1}s, " +
			$"overload {overload:F2}, pause {res.pauseTime:F1}s{(isSurge ? ", SURGE" : "")}{(isBossWave ? ", BOSS " + bossType : "")}, " +
			$"factions: {DescribeActiveFactions(waveId)}");

		return res;
	}

	// ===== Маршруты =====

	// Вся волна одним потоком по единственному маршруту уровня, против всех башен
	private RouteSlot SingleRouteSlot() {
		var s = Settings;
		return new RouteSlot {
			routeIdx = PrimaryRoute,
			spawnPointIdx = GetRouteSpawnPointIdx(PrimaryRoute),
			laneSide = 0,
			parallelGroups = s.maxParallelGroups,
			unitCap = s.maxUnitsPerWave
		};
	}

	private int GetRouteSpawnPointIdx(int route) {
		if (levelDesriptor == null || levelDesriptor.routs == null || route < 0 || route >= levelDesriptor.routs.Count)
			return 0;
		return levelDesriptor.routs[route].spawnPointIdx;
	}

	/// <summary>
	/// Решить, по каким маршрутам пойдёт волна. Идемпотентно по номеру волны: телеграф, уже показанный игроку,
	/// не перебрасывается (HandleSpawningShipDeparted приходит и от чит-волн). GenerateWave планирует сам,
	/// если волну не спланировали заранее (первая волна, вход в бесконечные волны после авторских)
	/// </summary>
	public WavePlan PlanNextWave(int waveId) {
		if (currentPlan != null && currentPlan.waveId == waveId)
			return currentPlan;

		currentPlan = new WavePlan { waveId = waveId, routes = IsMultiRoute ? ChooseRoutes(waveId) : new[] { PrimaryRoute } };

		if (currentPlan.IsSplit) {
			lastSingleRoute = -1;
			sameRouteStreak = 0;
		} else if (currentPlan.routes[0] == lastSingleRoute) {
			sameRouteStreak++;
		} else {
			lastSingleRoute = currentPlan.routes[0];
			sameRouteStreak = 1;
		}
		return currentPlan;
	}

	/// <summary>
	/// Куда ставить панель «следующая волна» для маршрута: первый вейпоинт, которого нет у остальных маршрутов
	/// (начало рукава после развилки). У уровня с одним маршрутом — точка высадки, как и раньше
	/// </summary>
	public Transform GetRoutePreviewPoint(int route) {
		var spawnPoint = levelDesriptor.spawnPoints[GetRouteSpawnPointIdx(route)].point;
		if (!IsMultiRoute)
			return spawnPoint != null ? spawnPoint.transform : null;

		int divergence = GetDivergenceIndex(route);
		var waypoints = levelDesriptor.routs[route].waypoints;
		if (divergence >= 0 && divergence < waypoints.Count && waypoints[divergence].marker != null)
			return waypoints[divergence].marker.transform;
		return spawnPoint != null ? spawnPoint.transform : null;
	}

	// Индекс первого вейпоинта маршрута, чей маркер не совпадает с маркером остальных маршрутов на той же позиции.
	// Общие отрезки маршрутов обязаны ссылаться на одни и те же маркеры. -1 — маршруты не расходятся
	private int GetDivergenceIndex(int route) {
		var waypoints = levelDesriptor.routs[route].waypoints;
		for (int i = 0; i < waypoints.Count; i++) {
			foreach (int other in routes) {
				if (other == route)
					continue;
				var otherWaypoints = levelDesriptor.routs[other].waypoints;
				if (i >= otherWaypoints.Count || otherWaypoints[i].marker != waypoints[i].marker)
					return i;
			}
		}
		return -1;
	}

	// Сторона дороги, в которую на развилке уходит рукав: +1 — вправо по ходу, −1 — влево («право» — как у WaveSpawner).
	// Двойная волна ведёт каждый рукав по его стороне: потоки не пересекаются ни на развилке, ни после слияния.
	// 0 — маршруты расходятся прямо от высадки, общей дороги нет (полосы от центра)
	private int GetLaneSide(int route) {
		int divergence = GetDivergenceIndex(route);
		if (divergence <= 0)
			return 0;

		var waypoints = levelDesriptor.routs[route].waypoints;
		Vector3 fork = waypoints[divergence - 1].marker.transform.position;
		Vector3 before = divergence >= 2 ? waypoints[divergence - 2].marker.transform.position : levelDesriptor.GetRouteSpawnPosition(route);
		Vector3 dirIn = fork - before;
		Vector3 dirOut = waypoints[divergence].marker.transform.position - fork;
		dirIn.y = 0f;
		dirOut.y = 0f;
		float turn = Vector3.Dot(Vector3.Cross(Vector3.up, dirIn.normalized), dirOut);
		return Mathf.Abs(turn) < 0.01f ? 0 : (turn > 0f ? 1 : -1);
	}

	// cover[t][k] — сколько метров маршрута routes[k] лежит в радиусе башни t
	// (тот же шаг и тот же тест, что у TowersController.GetRouteCoverage)
	private float[][] BuildCover(List<BaseTower> towers, float[] routeLengths) {
		const float step = 0.5f;
		var cover = new float[towers.Count][];
		for (int t = 0; t < towers.Count; t++)
			cover[t] = new float[routes.Count];

		for (int k = 0; k < routes.Count; k++) {
			for (float position = 0f; position < routeLengths[k]; position += step) {
				Vector3 point = levelDesriptor.GetRoutePoint(routes[k], position);
				for (int t = 0; t < towers.Count; t++) {
					float radius = towers[t].Radius;
					if ((point - towers[t].transform.position).sqrMagnitude < radius * radius)
						cover[t][k] += step;
				}
			}
		}
		return cover;
	}

	// Одиночная волна: башня в счёт целиком, если достаёт до маршрута. Двойная: башня делит DPS между потоками
	// пропорционально накрытой длине маршрутов (башня у общего отрезка держит оба потока сразу)
	private float[] GetTowerWeights(float[][] cover, int k, bool split) {
		var weights = new float[cover.Length];
		for (int t = 0; t < cover.Length; t++) {
			if (cover[t][k] <= 0f)
				continue;
			if (!split) {
				weights[t] = 1f;
				continue;
			}
			float total = 0f;
			for (int j = 0; j < cover[t].Length; j++)
				total += cover[t][j];
			weights[t] = cover[t][k] / total;
		}
		return weights;
	}

	private static float GetWeightedDPS(List<BaseTower> towers, float[] weights) {
		float dps = 0f;
		for (int t = 0; t < towers.Count; t++)
			dps += towers[t].DPS * weights[t];
		return dps;
	}

	private static float TowerWeight(float[] weights, int towerIndex) {
		if (weights == null)
			return 1f;
		return towerIndex < weights.Length ? weights[towerIndex] : 0f;
	}

	private float[] GetRouteLengths() {
		var lengths = new float[routes.Count];
		for (int k = 0; k < routes.Count; k++)
			lengths[k] = levelDesriptor.GetRouteLength(routes[k]);
		return lengths;
	}

	// Выбор маршрутов волны. Сила рукава = DPS достающих до него башен × sqrt(покрытия); чем рукав слабее сильнейшего,
	// тем чаще по нему идёт волна — голый рукав игроку придётся прикрыть
	private int[] ChooseRoutes(int waveId) {
		var s = Settings;
		var towers = GameController.Instance.GetAllTowers();
		var lengths = GetRouteLengths();
		var cover = BuildCover(towers, lengths);

		var power = new float[routes.Count];
		float maxPower = 0f;
		float minPower = float.MaxValue;
		for (int k = 0; k < routes.Count; k++) {
			float dps = GetWeightedDPS(towers, GetTowerWeights(cover, k, false));
			float coverage = Mathf.Clamp01(GameController.Instance.GetRouteCoverage(routes[k], lengths[k]) * routeCoverageContribution);
			power[k] = dps * Mathf.Sqrt(coverage);
			maxPower = Mathf.Max(maxPower, power[k]);
			minPower = Mathf.Min(minPower, power[k]);
		}

		bool isBossWave = bossEveryNWaves > 0 && (waveId + 1) % bossEveryNWaves == 0;

		// Двойная волна — только когда оба рукава хоть как-то защищены; surge-волна делится всегда
		if (!isBossWave && waveId >= s.firstSplitWave && minPower > 0f && (IsSurgeWave(waveId) || Random.value < s.splitWaveChance))
			return routes.ToArray();

		var weights = new float[routes.Count];
		float totalWeight = 0f;
		for (int k = 0; k < routes.Count; k++) {
			float deficit = maxPower > 0f ? 1f - power[k] / maxPower : 0f;
			weights[k] = 1f + s.weakRouteBias * deficit;
			// Босс по незащищённому рукаву неубиваем и ничему не учит — ведём его туда, где есть башни
			if (isBossWave && maxPower > 0f && power[k] <= 0f)
				weights[k] = 0f;
			if (routes[k] == lastSingleRoute && sameRouteStreak >= s.maxSameRouteStreak)
				weights[k] = 0f;
			totalWeight += weights[k];
		}

		// Запреты сняли все рукава (босс + серия по единственному защищённому) — серия важнее не становится
		if (totalWeight <= 0f) {
			for (int k = 0; k < routes.Count; k++) {
				weights[k] = power[k] > 0f || maxPower <= 0f ? 1f : 0f;
				totalWeight += weights[k];
			}
		}

		float roll = Random.value * totalWeight;
		for (int k = 0; k < routes.Count; k++) {
			roll -= weights[k];
			if (roll <= 0f && weights[k] > 0f)
				return new[] { routes[k] };
		}
		for (int k = routes.Count - 1; k >= 0; k--) {
			if (weights[k] > 0f)
				return new[] { routes[k] };
		}
		return new[] { routes[0] };
	}

	// Волна уровня с развилкой. Одиночная считается как обычная, но против башен своего рукава;
	// двойная — пул каждого рукава от его доли DPS, в сумме примерно полный пул острова
	private WaveGenResult GenerateMultiRouteWave(int waveId, float difficultyFactor) {
		var s = Settings;
		var res = new WaveGenResult();
		var plan = PlanNextWave(waveId);

		bool isBossWave = bossEveryNWaves > 0 && (waveId + 1) % bossEveryNWaves == 0;
		UnitType bossType = UnitType.Pirate;
		if (isBossWave)
			bossType = PickBoss(waveId, bossType);

		bool isSurge = IsSurgeWave(waveId);
		float overload = Mathf.Clamp(difficultyFactor, s.minOverload, s.maxOverload);
		if (isSurge)
			overload *= s.surgeMultiplier;

		var towers = GameController.Instance.GetAllTowers();
		var lengths = GetRouteLengths();
		var cover = BuildCover(towers, lengths);

		int count = plan.routes.Length;
		var slots = new RouteSlot[count];
		var pools = new float[count];
		var usedLimits = new Dictionary<UnitType, int>();
		float unitsAverageSpeed = isBossWave ? GetUnitSpeed(bossType) : GetUnitsAverageSpeed(waveId);
		float earlyRamp = s.earlyRampWaves > 0
			? Mathf.Lerp(s.earlyRampStart, 1f, Mathf.Pow(Mathf.Clamp01((float)waveId / s.earlyRampWaves), s.earlyRampPower))
			: 1f;

		float totalPool = 0f;
		float totalDps = 0f;
		float engagement = 0f;
		var engagedTowers = new HashSet<int>();
		for (int i = 0; i < count; i++) {
			int k = routes.IndexOf(plan.routes[i]);
			var weights = GetTowerWeights(cover, k, plan.IsSplit);
			for (int t = 0; t < weights.Length; t++) {
				if (weights[t] > 0f)
					engagedTowers.Add(t);
			}

			slots[i] = new RouteSlot {
				routeIdx = plan.routes[i],
				spawnPointIdx = GetRouteSpawnPointIdx(plan.routes[i]),
				laneSide = plan.IsSplit ? GetLaneSide(plan.routes[i]) : 0,
				parallelGroups = plan.IsSplit ? s.splitParallelGroups : s.maxParallelGroups,
				towerWeights = weights,
				usedLimits = usedLimits,
				logTag = "r" + plan.routes[i]
			};

			float dps = GetWeightedDPS(towers, weights);
			if (waveId == 0 || dps <= 0f)
				continue;

			float coverage = Mathf.Clamp01(GameController.Instance.GetRouteCoverage(plan.routes[i], lengths[k]) * routeCoverageContribution);
			float routeEngagement = Mathf.Clamp(lengths[k] / unitsAverageSpeed * Mathf.Sqrt(coverage), s.minEngagementTime, s.maxEngagementTime);
			pools[i] = dps * routeEngagement * overload * s.poolSafetyFactor * earlyRamp;
			totalPool += pools[i];
			totalDps += dps;
			engagement = Mathf.Max(engagement, routeEngagement);
		}

		// Доли рукавов — до бампа и пола: они двигают сумму, а не соотношение
		var shares = new float[count];
		for (int i = 0; i < count; i++)
			shares[i] = totalPool > 0f ? pools[i] / totalPool : 1f / count;
		if (count == 2) {
			shares[0] = Mathf.Clamp(shares[0], s.minSplitShare, 1f - s.minSplitShare);
			shares[1] = 1f - shares[0];
		}

		if (waveId == 0) {
			totalPool = s.firstWaveHpPool;
			engagement = 10f;
		} else {
			if (totalPool > 0f && GameController.Instance.CanBuildAnotherTower())
				totalPool += totalPool / Mathf.Max(engagedTowers.Count, 1f);

			// Пол работает и для рукава без башен: волна по голому рукаву небольшая, но протечёт целиком
			if (s.poolFloorBase > 0f) {
				float floor = s.poolFloorBase * (1f + s.poolFloorGrowthPerWave * waveId) * Mathf.Min(overload, 1f);
				totalPool = Mathf.Max(totalPool, floor);
			}
			if (totalPool <= 0f)
				totalPool = s.firstWaveHpPool * overload;
			if (engagement <= 0f)
				engagement = 10f;
		}

		res.expectedKillTime = engagement;
		res.pauseTime = waveId == 0 ? 10f : Mathf.Clamp(engagement * s.pauseFactor, s.minPause, s.maxPause);
		res.isSurge = isSurge;

		for (int i = 0; i < count; i++) {
			pools[i] = totalPool * shares[i];
			slots[i].unitCap = Mathf.Max(1, Mathf.RoundToInt(s.maxUnitsPerWave * shares[i]));
		}

		if (isBossWave) {
			res.wave = CreateBossWave(bossType, pools[0], waveId, slots[0]);
		} else {
			res.wave = new Wave { id = waveId, groups = new List<SpawnGroup>() };
			for (int i = 0; i < count; i++)
				AppendGroups(res.wave, pools[i], waveId, slots[i]);
		}

		var routeLog = new StringBuilder();
		for (int i = 0; i < count; i++) {
			if (i > 0) routeLog.Append(" / ");
			routeLog.Append(slots[i].logTag);
			if (plan.IsSplit) routeLog.Append($" {shares[i] * 100f:F0}%");
		}
		Debug.Log($"[WaveGen] wave {waveId + 1}: {(plan.IsSplit ? "split" : "route")} {routeLog}, pool {totalPool:F0}, dps {totalDps:F1}, engagement {engagement:F1}s, " +
			$"overload {overload:F2}, pause {res.pauseTime:F1}s{(isSurge ? ", SURGE" : "")}{(isBossWave ? ", BOSS " + bossType : "")}, " +
			$"factions: {DescribeActiveFactions(waveId)}");

		return res;
	}

	// ===== Расписание фракций =====

	// Достраивает расписание так, чтобы все фракции, чьё окно накрывает waveId, уже были поставлены.
	// Следующая фракция стартует за transitionWaves до конца предыдущей (там и идёт кроссфейд).
	private void EnsureSchedule(int waveId) {
		if (factions == null || factions.Count == 0)
			return;

		int t = Mathf.Max(transitionWaves, 0);
		while (schedule.Count == 0 || schedule[schedule.Count - 1].EndWave - t <= waveId) {
			var entry = NextFromBag();
			if (entry == null)
				return;

			int start = 0;
			int duration = Mathf.Max(entry.durationWaves, 1);
			if (schedule.Count > 0) {
				var last = schedule[schedule.Count - 1];
				// max(..., +1): при длительности <= переходу окно всё равно должно двигаться вперёд
				start = Mathf.Max(last.EndWave - t, last.startWave + 1);
			} else {
				duration += Mathf.Max(firstFactionExtraWaves, 0);
			}
			schedule.Add(new ScheduledFaction { entry = entry, startWave = start, duration = duration, cycle = cycleCounter, ordinal = schedule.Count });
		}
	}

	// Мешок: перемешанный список фракций уровня; когда пуст — наполняется заново,
	// первая фракция нового мешка никогда не совпадает с последней предыдущего.
	private LevelFaction NextFromBag() {
		if (bag.Count == 0) {
			// Мета: в мешок идут только открытые фракции (ContentUnlocksManager). Если открытых нет
			// (кривой конфиг уровня) — берём все, иначе волны опустеют и катка встанет
			foreach (var f in factions) {
				if (f != null && f.faction != null && f.durationWaves > 0 && Gameplay.Unlocks.ContentUnlocksManager.IsFactionUnlocked(f.faction.factionId))
					bag.Add(f);
			}
			if (bag.Count == 0) {
				foreach (var f in factions) {
					if (f != null && f.faction != null && f.durationWaves > 0)
						bag.Add(f);
				}
				if (bag.Count > 0)
					Debug.LogWarning("[WaveGen] Ни одна фракция уровня не открыта метой — используются все фракции уровня.");
			}
			if (bag.Count == 0)
				return null;

			for (int i = bag.Count - 1; i > 0; i--) {
				int j = Random.Range(0, i + 1);
				(bag[i], bag[j]) = (bag[j], bag[i]);
			}
			if (bag.Count > 1 && bag[0] == lastPicked) {
				int j = Random.Range(1, bag.Count);
				(bag[0], bag[j]) = (bag[j], bag[0]);
			}
			if (schedule.Count > 0)
				cycleCounter++;
		}

		var e = bag[0];
		bag.RemoveAt(0);
		lastPicked = e;
		return e;
	}

	// Активные на волне фракции с множителем кроссфейда. На переходе множители уходящей и входящей
	// фракций в сумме дают 1: k-я волна перехода (с нуля) — (k+1)/(T+1) входящей, (T-k)/(T+1) уходящей.
	private List<ActiveFaction> GetActiveFactions(int waveId) {
		EnsureSchedule(waveId);
		var res = new List<ActiveFaction>();
		int t = Mathf.Max(transitionWaves, 0);

		for (int i = 0; i < schedule.Count; i++) {
			var sf = schedule[i];
			if (waveId < sf.startWave || waveId >= sf.EndWave)
				continue;

			float m = 1f;
			if (t > 0) {
				int fromStart = waveId - sf.startWave;
				if (i > 0 && fromStart < t) // первая фракция уровня не «вплывает» — она сразу в полную силу
					m = Mathf.Min(m, (fromStart + 1f) / (t + 1f));
				int toEnd = sf.EndWave - 1 - waveId;
				if (toEnd < t)
					m = Mathf.Min(m, (toEnd + 1f) / (t + 1f));
			}
			res.Add(new ActiveFaction { scheduled = sf, multiplier = m });
		}
		return res;
	}

	private string DescribeActiveFactions(int waveId) {
		var active = GetActiveFactions(waveId);
		if (active.Count == 0)
			return "none";
		return string.Join(", ", active.Select(a => $"{a.scheduled.entry.faction.DisplayName} x{a.multiplier:F2} (hp x{FactionHpMultiplier(a.scheduled):F2})"));
	}

	// Множитель ХП и наград фракции: растёт с её номером в расписании и с кругом мешка,
	// чтобы на поздних волнах тел было меньше, а не толпа стартового мусора
	private float FactionHpMultiplier(ScheduledFaction sf) {
		var s = Settings;
		return Mathf.Max(1f + s.hpStepPerFaction * sf.ordinal + s.hpStepPerCycle * sf.cycle, 0.1f);
	}

	// «Главная» фракция волны: с наибольшим множителем кроссфейда, при равенстве — входящая
	private ScheduledFaction GetMainFaction(int waveId) {
		var active = GetActiveFactions(waveId);
		if (active.Count == 0)
			return null;
		ActiveFaction main = active[0];
		for (int i = 1; i < active.Count; i++) {
			if (active[i].multiplier >= main.multiplier)
				main = active[i];
		}
		return main.scheduled;
	}

	// Позиция волны внутри окна фракции, 0..1
	private static float FactionPhase(ScheduledFaction sf, int waveId) {
		int d = Mathf.Max(sf.duration, 1);
		return Mathf.Clamp01((waveId - sf.startWave) / (float)d);
	}

	// ===== Ярусы =====

	private int TierMinWave(UnitTier tier) {
		var s = Settings;
		switch (tier) {
			case UnitTier.T2: return s.tier2MinWave;
			case UnitTier.T3: return s.tier3MinWave;
			case UnitTier.T4: return s.tier4MinWave;
			default: return 0;
		}
	}

	// Ярус открыт, если открыт метой (хаб, ContentUnlocksManager) И дошла волна. Закрывает юниты, боссов и чемпиона
	public bool IsTierUnlocked(UnitTier tier, int waveId) {
		if (!Gameplay.Unlocks.ContentUnlocksManager.IsTierUnlocked(tier))
			return false;
		return tier == UnitTier.T1 || waveId >= TierMinWave(tier) + tierUnlockShift;
	}

	private UnitType PickBoss(int waveId, UnitType fallback) {
		var main = GetMainFaction(waveId);
		if (main == null)
			return fallback;

		var faction = main.entry.faction;
		if (faction.bosses != null) {
			var unlocked = faction.bosses.FindAll(b => IsTierUnlocked(b.tier, waveId));
			if (unlocked.Count > 0)
				return unlocked[Random.Range(0, unlocked.Count)].unitType;
		}

		// Боссы фракции ещё закрыты (или не заданы) — «чемпион»: самый высокий открытый ярус состава,
		// при равенстве самый толстый. ХП всё равно масштабируется под пул босс-волны
		UnitType best = fallback;
		int bestTier = -1;
		float bestHp = -1f;
		foreach (var cfg in faction.units) {
			if (!IsTierUnlocked(cfg.tier, waveId))
				continue;
			var unit = GetUnitPrefab(cfg.unitType);
			if (unit == null)
				continue;
			if ((int)cfg.tier > bestTier || ((int)cfg.tier == bestTier && unit.CombinedHP > bestHp)) {
				bestTier = (int)cfg.tier;
				bestHp = unit.CombinedHP;
				best = cfg.unitType;
			}
		}
		Debug.Log($"[WaveGen] wave {waveId + 1}: боссы фракции {faction.DisplayName} закрыты, чемпион {best}");
		return best;
	}

	// ===== Сборка волны =====

	private Wave CreateBossWave(UnitType bossType, float totalHP, int waveId, RouteSlot slot) {
		var s = Settings;
		float escortHP = totalHP * s.bossEscortFraction;
		float bossPool = totalHP - escortHP;

		var unit = GetUnitPrefab(bossType);
		float bossHp = Mathf.Max(unit != null ? unit.CombinedHP : 1f, 1f);
		// Награда и дети босса масштабируются по фракции, а не по его масштабу под пул волны
		var mainFaction = GetMainFaction(waveId);
		float factionMultiplier = mainFaction != null ? FactionHpMultiplier(mainFaction) : 1f;

		// Эскорт собирается обычной волной; босс добавляется В КОНЕЦ списка групп,
		// чтобы не сдвигать индексы waitingList эскорта (id группы = её индекс в списке).
		Wave res = escortHP > 0f ? CreateWave(escortHP, waveId, slot) : new Wave { id = waveId, groups = new List<SpawnGroup>() };
		res.id = waveId; // у CreateWave уже стоит, но фиксируем на случай пустого эскорта

		res.groups.Add(new SpawnGroup() {
			type = bossType,
			spawnPointIdx = slot.spawnPointIdx,
			routeIdx = slot.routeIdx,
			startDelay = 2f,
			minGapFactor = s.spawnGapFactor.x,
			maxGapFactor = s.spawnGapFactor.y,
			waitingList = new int[0],
			unitsCount = 1,
			unitsHPMultiplier = bossPool / bossHp,
			rewardMultiplier = factionMultiplier,
			isBoss = true
		});

		return res;
	}

	private Wave CreateWave(float totalHP, int waveId, RouteSlot slot) {
		Wave res = new Wave();
		res.id = waveId;
		res.groups = new List<SpawnGroup>();
		AppendGroups(res, totalHP, waveId, slot);
		return res;
	}

	// Собирает группы на бюджет totalHP и дописывает их в волну. Двойная волна зовёт это по разу на рукав:
	// id группы (его ждут в waitingList) — её индекс в общем списке wave.groups, поэтому индексы второго прохода сдвинуты
	private void AppendGroups(Wave res, float totalHP, int waveId, RouteSlot slot) {
		var s = Settings;

		var availableUnits = GetAvailableUnitsWithWeights(waveId, slot.towerWeights);
		availableUnits.RemoveAll(u => u.weight <= 0f);
		if (availableUnits.Count == 0) {
			Debug.LogWarning("No available units for wave " + waveId);
			return;
		}

		float totalWeight = availableUnits.Sum(u => u.weight);
		var damageShares = GetDamageTypeShares(slot.towerWeights);

		// 1) Раздаём бюджет пропорционально весам, целую часть — сразу.
		//    Тело стоит эффективный ХП: базовый × множитель фракции / резист-фактор против текущих башен,
		//    так резисты становятся вопросом «какую башню ставить», а не скачком сложности
		var allocations = new List<UnitAllocation>();
		foreach (var unitData in availableUnits) {
			var unit = GetUnitPrefab(unitData.unitType);
			if (unit == null) continue;

			float effectiveHP = unit.CombinedHP * unitData.hpMultiplier / GetResistFactor(unit, damageShares);
			float allocatedHP = totalHP * unitData.weight / totalWeight;
			float exact = allocatedHP / effectiveHP;
			int count = Mathf.FloorToInt(exact);

			allocations.Add(new UnitAllocation {
				config = unitData.config,
				unit = unit,
				hpMultiplier = unitData.hpMultiplier,
				effectiveHP = effectiveHP,
				allocatedHP = allocatedHP,
				count = count,
				fraction = exact - count
			});
		}

		if (allocations.Count == 0)
			return;

		// Лимит юнита — на всю волну: второй рукав двойной волны получает то, что не выбрал первый
		int LimitLeft(UnitAllocation a) {
			if (a.config.limit <= 0)
				return int.MaxValue;
			int used = 0;
			slot.usedLimits?.TryGetValue(a.config.unitType, out used);
			return Mathf.Max(a.config.limit - used, 0);
		}

		// 2) Лимиты: срезанное лимитом ХП возвращается в общий остаток
		float remaining = totalHP;
		foreach (var a in allocations) {
			a.count = Mathf.Min(a.count, LimitLeft(a));
			remaining -= a.count * a.effectiveHP;
		}

		// 3) Остаток пула — по наибольшим дробным частям (largest remainder),
		//    чтобы RoundToInt не терял и не раздувал бюджет
		foreach (var a in allocations.OrderByDescending(x => x.fraction)) {
			if (a.count >= LimitLeft(a))
				continue;
			if (remaining < a.effectiveHP * 0.5f)
				continue;
			a.count++;
			remaining -= a.effectiveHP;
		}

		// 4) Гарантия непустой волны: хотя бы один самый дешёвый юнит
		if (allocations.Sum(a => a.count) == 0) {
			var cheapest = allocations.OrderBy(a => LimitLeft(a) > 0 ? 0 : 1).ThenBy(a => a.effectiveHP).First();
			cheapest.count = 1;
		}

		// 5) Кап тел: излишек конвертируется в ХП-множитель (бюджет сохраняется, юниты толще)
		int totalUnits = allocations.Sum(a => a.count);
		if (totalUnits > slot.unitCap) {
			float scale = (float)slot.unitCap / totalUnits;
			foreach (var a in allocations) {
				if (a.count == 0) continue;
				int newCount = Mathf.Max(1, Mathf.FloorToInt(a.count * scale));
				a.fraction = a.count / (float)newCount; // временно храним ХП-множитель
				a.count = newCount;
			}
		} else {
			foreach (var a in allocations)
				a.fraction = 1f; // множитель ХП по умолчанию
		}

		// 6) Группы: медленные вперёд; до maxParallelGroups линий идут одновременно
		//    (группа i ждёт группу i - maxParallelGroups), стартовые задержки ступенькой.
		//    У каждой линии своя полоса дороги: группа i и её сменщица i + maxParallelGroups идут по одной полосе,
		//    одновременные группы — по разным сторонам (дистанция спавна между полосами считается по диагонали)
		allocations.Sort((a, b) => ((float)a.unit.speed).CompareTo((float)b.unit.speed));

		int parallelGroups = Mathf.Max(slot.parallelGroups, 1);
		int laneCount = Mathf.Min(parallelGroups, allocations.Count(a => a.count > 0));
		int groupIdOffset = res.groups.Count;

		var log = new StringBuilder();
		int groupIndex = 0;
		foreach (var a in allocations) {
			if (a.count == 0)
				continue;

			int[] waitingList = groupIndex < parallelGroups
				? new int[0]
				: new int[] { groupIdOffset + groupIndex - parallelGroups };

			// Итоговый множитель ХП тела = фракционный × кап тел; награды (rewardMultiplier = -1) идут по нему же
			float hpMultiplier = a.hpMultiplier * a.fraction;
			res.groups.Add(new SpawnGroup() {
				type = a.config.unitType,
				spawnPointIdx = slot.spawnPointIdx,
				routeIdx = slot.routeIdx,
				startDelay = (groupIndex % parallelGroups) * s.laneStagger,
				// Таймеров между юнитами нет (min/maxSpawnDelay = 0): темп выхода задаёт дистанция до предыдущих (WaveSpawner)
				minGapFactor = s.spawnGapFactor.x,
				maxGapFactor = s.spawnGapFactor.y,
				laneOffset = slot.laneSide == 0
					? s.GetLaneOffset(groupIndex % parallelGroups, laneCount)
					: s.GetSideLaneOffset(groupIndex % parallelGroups, slot.laneSide),
				waitingList = waitingList,
				unitsCount = a.count,
				unitsHPMultiplier = hpMultiplier
			});

			if (slot.usedLimits != null) {
				slot.usedLimits.TryGetValue(a.config.unitType, out int used);
				slot.usedLimits[a.config.unitType] = used + a.count;
			}

			if (log.Length > 0) log.Append(", ");
			log.Append(a.count).Append("x ").Append(a.config.unitType);
			if (hpMultiplier > 1.001f) log.Append($" (hp x{hpMultiplier:F2})");
			groupIndex++;
		}

		Debug.Log($"[WaveGen] wave {waveId + 1} composition{(slot.logTag != null ? " " + slot.logTag : "")}: {log}");
	}

	// Доступные на волне юниты всех активных фракций с итоговыми весами.
	// Один и тот же тип из двух фракций (например волк) складывает веса.
	// towerWeights — башни, против которых собирается состав (null — все башни уровня)
	private List<UnitWeightData> GetAvailableUnitsWithWeights(int waveId, float[] towerWeights = null) {
		var s = Settings;
		var byType = new Dictionary<UnitType, UnitWeightData>();

		Dictionary<TowerType, int> towerTypeCounts = GetTowerTypeCounts(towerWeights);

		foreach (var active in GetActiveFactions(waveId)) {
			var sf = active.scheduled;
			float phase = FactionPhase(sf, waveId);
			float phaseStartFactor = sf.cycle > 0 ? repeatCyclePhaseFactor : 1f;
			float factionHpMultiplier = FactionHpMultiplier(sf);

			foreach (var config in sf.entry.faction.units) {
				if (!IsTierUnlocked(config.tier, waveId))
					continue;
				float start = config.phaseStart * phaseStartFactor;
				float end = Mathf.Max(config.phaseEnd, start);
				if (phase < start || (phase >= end && end < 1f))
					continue;

				float t = end > start ? Mathf.Clamp01((phase - start) / (end - start)) : 0.5f;
				float finalWeight = config.baseWeight * config.weightCurve.Evaluate(t) * active.multiplier;

				// Давление «наперекор игроку»: чем меньше башен, контрящих юнита, тем больше его вес
				if (config.towerWeakness != 0) {
					towerTypeCounts.TryGetValue(config.towerWeakness, out int counters);
					finalWeight *= s.weaknessPressureCurve.Evaluate(counters);
				}

				// Для юнитов с ограниченной маской (летающие): давим по фактической доле DPS,
				// способной их бить, а не по числу башен
				finalWeight *= GetMaskPressure(config.unitType, towerWeights);

				if (byType.TryGetValue(config.unitType, out var existing)) {
					// Конфиг и множитель берём от более весомого вхождения (лимит/контра), веса складываем
					if (finalWeight > existing.weight) {
						existing.config = config;
						existing.hpMultiplier = factionHpMultiplier;
					}
					existing.weight += finalWeight;
				} else {
					byType[config.unitType] = new UnitWeightData {
						unitType = config.unitType,
						weight = finalWeight,
						config = config,
						hpMultiplier = factionHpMultiplier
					};
				}
			}
		}

		return byType.Values.ToList();
	}

	// Доля DPS башен по типам урона (пусто — башен нет)
	private Dictionary<DamageType, float> GetDamageTypeShares(float[] towerWeights) {
		var shares = new Dictionary<DamageType, float>();
		var towers = GameController.Instance.GetAllTowers();
		float totalDPS = 0f;
		for (int i = 0; i < towers.Count; i++) {
			var tower = towers[i];
			float dps = tower.DPS * TowerWeight(towerWeights, i);
			if (dps <= 0f) continue;
			totalDPS += dps;
			shares.TryGetValue(tower.DamageType, out float acc);
			shares[tower.DamageType] = acc + dps;
		}
		if (totalDPS <= 0f)
			return shares;
		foreach (var key in shares.Keys.ToList())
			shares[key] /= totalDPS;
		return shares;
	}

	// Средний множитель урона по юниту от текущего набора башен (1 — резисты не влияют, <1 — юнит крепче).
	// Трейты (огонь/яд) не учитываем: это бонус игрока, а не база расчёта
	private float GetResistFactor(Unit unit, Dictionary<DamageType, float> damageShares) {
		float weight = Settings.resistBudgetWeight;
		if (damageShares.Count == 0 || weight <= 0f || unit.Resistances == null)
			return 1f;

		float factor = 0f;
		foreach (var kv in damageShares) {
			float resist = 0f;
			for (int i = 0; i < unit.Resistances.Count; i++) {
				if (unit.Resistances[i].damageType == kv.Key) {
					resist = Mathf.Clamp(unit.Resistances[i].value, -1f, 1f);
					break;
				}
			}
			factor += kv.Value * Mathf.Max(1f - resist, unit.MinDamageMultiplier);
		}
		return Mathf.Max(Mathf.Lerp(1f, factor, weight), 0.05f);
	}

	private float GetMaskPressure(UnitType unitType, float[] towerWeights) {
		var unit = GetUnitPrefab(unitType);
		if (unit == null || unit.targetMask == -1)
			return 1f;

		var towers = GameController.Instance.GetAllTowers();
		float totalDPS = 0f;
		float coveringDPS = 0f;
		for (int i = 0; i < towers.Count; i++) {
			var tower = towers[i];
			float dps = tower.DPS * TowerWeight(towerWeights, i);
			totalDPS += dps;
			if ((unit.targetMask & (int)tower.towerType) != 0)
				coveringDPS += dps;
		}

		float fraction = totalDPS > 0f ? coveringDPS / totalDPS : 0f;
		return Settings.flyerPressureCurve.Evaluate(fraction);
	}

	private Dictionary<TowerType, int> GetTowerTypeCounts(float[] towerWeights) {
		Dictionary<TowerType, int> towerTypeCounts = new Dictionary<TowerType, int>();
		var towers = GameController.Instance.GetAllTowers();
		for (int i = 0; i < towers.Count; i++) {
			var tower = towers[i];
			// Контр-башня в счёт, только если достаёт до маршрута волны
			if (TowerWeight(towerWeights, i) <= 0f)
				continue;
			if (!towerTypeCounts.ContainsKey(tower.towerType)) {
				towerTypeCounts[tower.towerType] = 0;
			}
			towerTypeCounts[tower.towerType]++;
		}
		return towerTypeCounts;
	}

	private float GetUnitsAverageSpeed(int waveId) {
		float totalSpeed = 0f;
		int count = 0;

		var availableUnits = GetAvailableUnitsWithWeights(waveId);

		foreach (var unitData in availableUnits) {
			if (unitData.weight > 0) {
				totalSpeed += GetUnitSpeed(unitData.unitType);
				count++;
			}
		}

		return count > 0 ? totalSpeed / count : 1f;
	}

	private Unit GetUnitPrefab(UnitType unitType) {
		if (unitsDescription == null || !unitsDescription.unitsToSpawn.ContainsKey(unitType))
			return null;
		return unitsDescription.unitsToSpawn[unitType];
	}

	private float GetUnitSpeed(UnitType unitType) {
		var unit = GetUnitPrefab(unitType);
		return unit != null ? unit.speed : 1f;
	}
}
