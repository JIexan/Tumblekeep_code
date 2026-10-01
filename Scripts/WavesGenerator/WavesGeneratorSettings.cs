using UnityEngine;

// Тюнинг модели сложности генератора волн. Пер-уровневый контент (unitConfigs, боссы)
// остаётся на сценном WavesGenerator — здесь только числа модели, общие для уровней.
[CreateAssetMenu(menuName = "Settings/WavesGeneratorSettings")]
public class WavesGeneratorSettings : ScriptableObject {

	[Header("Пул ХП волны")]
	[Tooltip("Пул ХП нулевой волны (кол-во юнитов = пул / ХП юнита).")]
	public float firstWaveHpPool = 80f;
	[Tooltip("Глобальная поправка пула: оверкилл и подлёт снарядов (резисты считаются отдельно через эффективный ХП).")]
	public float poolSafetyFactor = 0.85f;
	[Tooltip("Пол пула: poolFloorBase × (1 + poolFloorGrowthPerWave × номер волны), чтобы с двумя башнями волны не выглядели пустыми. База 0 = выключено.")]
	public float poolFloorBase = 50f;
	public float poolFloorGrowthPerWave = 0.1f;
	[Tooltip("Первые N волн пул нарастает от earlyRampStart до 1.")]
	public int earlyRampWaves = 10;
	[Range(0f, 1f)] public float earlyRampStart = 0.35f;
	[Tooltip("Степень кривой рампа: >1 — медленный старт (время подышать), разгон к концу рампа.")]
	public float earlyRampPower = 2f;

	[Header("Окно боя и пауза между волнами")]
	public float minEngagementTime = 10f;
	public float maxEngagementTime = 35f;
	[Tooltip("Пауза до следующей волны = окно боя × pauseFactor (с клампом ниже).")]
	public float pauseFactor = 0.8f;
	public float minPause = 8f;
	public float maxPause = 22f;

	[Header("Перегруз (адаптивная сложность)")]
	[Tooltip("Кламп итогового множителя сложности (база × pressure).")]
	public float minOverload = 0.7f;
	public float maxOverload = 2.5f;
	[Tooltip("Границы адаптивного pressure-фактора.")]
	public float minPressure = 0.75f;
	public float maxPressure = 1.35f;
	[Tooltip("Дельта pressure: волна зачищена быстро и без урона замку.")]
	public float pressureFastFlawless = 0.08f;
	[Tooltip("Дельта pressure: волна зачищена без протечек.")]
	public float pressureFlawless = 0.03f;
	[Tooltip("Дельта pressure: волна протекла (применяется один раз на волну).")]
	public float pressureSmallLeak = -0.05f;
	[Tooltip("Дельта pressure: за волну потеряно много ХП замка (заменяет small).")]
	public float pressureBigLeak = -0.12f;
	[Tooltip("Потеря ХП замка за волну, начиная с которой протечка считается крупной.")]
	public float bigLeakCastleHP = 4f;
	[Tooltip("Зачистка быстрее expectedKillTime × это значение считается быстрой.")]
	public float fastClearRatio = 0.6f;

	[Header("Surge-волны (намеренно за грань)")]
	[Tooltip("Каждая N-я волна получает перегруз сверх клампа. 0 = выключено.")]
	public int surgePeriod = 7;
	public float surgeMultiplier = 1.3f;

	[Header("Множитель ХП по фракциям")]
	[Tooltip("Прибавка к множителю ХП (и наград) за каждую следующую фракцию в расписании уровня: 1 + шаг × номер.")]
	public float hpStepPerFaction = 0.25f;
	[Tooltip("Прибавка за каждый новый круг мешка фракций.")]
	public float hpStepPerCycle = 0.5f;

	[Header("Ярусы юнитов")]
	[Tooltip("С какой волны (0 = первая) открывается ярус; T1 открыт всегда. Уровень может сдвигать пороги (tierUnlockShift у WavesGenerator).")]
	public int tier2MinWave = 3;
	public int tier3MinWave = 11;
	public int tier4MinWave = 19;

	[Header("Эффективный ХП")]
	[Tooltip("Насколько бюджет учитывает резисты юнита против текущего набора башен (доля DPS по типам урона): 0 — сырой ХП, 1 — полностью.")]
	[Range(0f, 1f)] public float resistBudgetWeight = 1f;

	[Header("Состав волны")]
	[Tooltip("Жёсткий кап тел в волне; излишек бюджета конвертируется в ХП-множитель.")]
	public int maxUnitsPerWave = 60;
	[Tooltip("Доля пула босс-волны, уходящая в эскорт из обычных юнитов. 0 = босс без эскорта.")]
	[Range(0f, 1f)] public float bossEscortFraction = 0.35f;
	[Tooltip("Вес юнита × кривая(кол-во контр-башен его towerWeakness): 0 башен — давим этим юнитом.")]
	public AnimationCurve weaknessPressureCurve = new AnimationCurve(
		new Keyframe(0f, 1.5f), new Keyframe(1f, 1.15f), new Keyframe(2f, 1f), new Keyframe(3f, 0.85f));
	[Tooltip("Вес юнита с ограниченной targetMask × кривая(доля DPS башен, способных его бить).")]
	public AnimationCurve flyerPressureCurve = new AnimationCurve(
		new Keyframe(0f, 1.6f), new Keyframe(0.3f, 1.2f), new Keyframe(0.6f, 1f), new Keyframe(1f, 0.9f));

	[Header("Дистанция спавна")]
	[Tooltip("Юниты выходят с корабля по дистанции, а не по таймеру (WaveSpawner): следующий ждёт, пока предыдущие отойдут на сумму габаритов тел × (1 + зазор). Зазор случайный в этом диапазоне: 0 — тела вплотную, 1 — просвет длиной в сами тела. Габарит берётся с коллайдера юнита (или spacingHalfLength на префабе). Между разными полосами дистанция считается по диагонали, так что главная крутилка плотности волны — это поле.")]
	public Vector2 spawnGapFactor = new Vector2(0.6f, 1.4f);

	[Header("Интерливинг групп")]
	[Tooltip("Сколько групп волны может спавниться одновременно.")]
	public int maxParallelGroups = 3;
	[Tooltip("Ступенька startDelay между параллельными группами, сек.")]
	public float laneStagger = 1.5f;
	[Tooltip("Шаг между полосами параллельных групп поперёк дороги, юниты мира. Группы расходятся по разным сторонам дороги и держат полосу до замка; крайняя полоса + тело юнита должны помещаться в полуширину дороги.")]
	public float laneSpacing = 1.5f;

	[Header("Маршруты (уровни с развилкой; при одном маршруте не участвуют)")]
	[Tooltip("Насколько чаще волна идёт по слабо защищённому рукаву: вес рукава = 1 + это × (1 − сила рукава / сила сильнейшего). 0 — выбор равновероятный.")]
	public float weakRouteBias = 1.5f;
	[Tooltip("Столько одиночных волн подряд максимум идёт по одному рукаву.")]
	public int maxSameRouteStreak = 2;
	[Tooltip("С какой волны (0 = первая) волна может разделиться на оба рукава.")]
	public int firstSplitWave = 3;
	[Tooltip("Шанс, что волна пойдёт по обоим рукавам сразу (surge-волна делится всегда). На рукав без башен двойные волны не ходят.")]
	[Range(0f, 1f)] public float splitWaveChance = 0.35f;
	[Tooltip("Минимальная доля пула двойной волны на рукав (максимальная = 1 − это).")]
	[Range(0f, 0.5f)] public float minSplitShare = 0.35f;
	[Tooltip("Сколько групп рукава выходит одновременно в двойной волне. Рукав занимает свою сторону дороги, крайняя полоса = (это − 0.5) × splitLaneSpacing — вместе с телом юнита должна помещаться в полуширину дороги.")]
	public int splitParallelGroups = 2;
	[Tooltip("Шаг полос двойной волны. Уже обычного laneSpacing: на общей дороге идут сразу оба рукава, по splitParallelGroups линий на каждой стороне.")]
	public float splitLaneSpacing = 1.2f;

	// Сдвиг полосы от оси: полосы заполняются от центра наружу (0 → центр, дальше влево/вправо по очереди),
	// чтобы первые — самые медленные и обычно самые крупные — группы шли посередине дороги
	public float GetLaneOffset(int laneIndex, int laneCount) {
		if (laneCount <= 1)
			return 0f;
		laneIndex %= laneCount;
		if (laneCount % 2 == 1) {
			int step = (laneIndex + 1) / 2;
			return (laneIndex % 2 == 1 ? -step : step) * laneSpacing;
		}
		float halfStep = laneIndex / 2 + 0.5f;
		return (laneIndex % 2 == 0 ? -halfStep : halfStep) * laneSpacing;
	}

	// Полоса двойной волны: рукав идёт по своей стороне дороги (side = ±1), полосы — от оси наружу.
	// Так потоки разных рукавов не пересекаются ни на развилке, ни после слияния
	public float GetSideLaneOffset(int laneIndex, int side) {
		return side * (laneIndex + 0.5f) * splitLaneSpacing;
	}
}
