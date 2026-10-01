using Gameplay.Cutscenes;
using UnityEngine;

/// <summary>Keep clicking to maintain HP. Spawns enemies only while HP is empty; clicks can always restore it.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Tower Defence/Player Distraction")]
public sealed class PlayerDistraction : MonoBehaviour
{
    private const float MinimumSpawnDelay = 0.05f;

    [Header("HP")]
    [SerializeField, Min(0.01f)] private float maxHP = 100f;
    [SerializeField, Min(0f), Tooltip("HP, восстанавливаемое одним кликом.")]
    private float hpPerClick = 20f;
    [SerializeField, Min(0f), Tooltip("Потеря HP в секунду игрового времени.")]
    private float hpLossPerSecond = 5f;
    [SerializeField, Min(0f), Tooltip("Пауза после заполнения HP, в том числе в начале боя. Клики при уже полном HP не продлевают её.")]
    private float fullHPDelay = 3f;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 5f, 0f);

    [Header("Spawning after HP reaches zero")]
    [SerializeField, Tooltip("Точная позиция спавна. Юнит разворачивается к первому waypoint маршрута.")]
    private Transform spawnPoint;
    [SerializeField, Min(0), Tooltip("Индекс маршрута в LevelDesriptor.routs, начиная с 0.")]
    private int routeIndex;
    [SerializeField] private UnitType unitType = UnitType.Skeleton;
    [SerializeField, Tooltip("X — номер текущей волны в HUD (с 1), Y — секунды между юнитами. За границами используются значения крайних ключей. Минимальная задержка 0.05 с.")]
    private AnimationCurve spawnDelayByWave = AnimationCurve.Linear(1f, 8f, 20f, 2f);

    private GameController game;
    private float currentHP;
    private float fullHPDelayRemaining;
    private float timeSinceLastSpawn;
    private bool hasSpawned;
    private bool initialized;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public float NormalizedHP => maxHP > 0f ? Mathf.Clamp01(currentHP / maxHP) : 0f;
    public bool IsDepleted => initialized && currentHP <= 0f;
    public Vector3 HealthBarPosition => transform.position + healthBarOffset;

    private bool CanOperate => isActiveAndEnabled && game != null && game.IsGameStarted &&
        Time.timeScale > 0f && !CutsceneController.IsPlaying;

    private void Reset() {
        spawnPoint = transform;
    }

    private void OnValidate() {
        maxHP = Mathf.Max(0.01f, maxHP);
        hpPerClick = Mathf.Max(0f, hpPerClick);
        hpLossPerSecond = Mathf.Max(0f, hpLossPerSecond);
        fullHPDelay = Mathf.Max(0f, fullHPDelay);
        routeIndex = Mathf.Max(0, routeIndex);
    }

    private void Awake() {
        currentHP = maxHP;
        fullHPDelayRemaining = fullHPDelay;
        initialized = true;
    }

    private void Start() {
        game = GameController.Instance;
        string error = null;
        if (game == null || !TryValidateSpawn(out error)) {
            Debug.LogError($"[PlayerDistraction] {name}: " + (game == null ? "GameController не найден." : error), this);
            enabled = false;
            return;
        }
        if (game.uiController == null || game.uiController.heathBarController == null) {
            Debug.LogError($"[PlayerDistraction] {name}: HeathBarController не назначен в UIController.", this);
            enabled = false;
            return;
        }
        game.uiController.heathBarController.AddDistractionBar(this);
    }

    private void Update() {
        if (!CanOperate)
            return;

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        if (!IsDepleted)
            TickHealth(deltaTime);
        if (!IsDepleted)
            return;

        // First enemy appears immediately on depletion. Re-evaluate the interval against the
        // current HUD wave each frame, so a new wave also affects an interval already in progress.
        timeSinceLastSpawn += deltaTime;
        if (hasSpawned && timeSinceLastSpawn < GetSpawnDelay(game.CurrentWaveIndex))
            return;

        if (!TryValidateSpawn(out string error)) {
            Debug.LogError($"[PlayerDistraction] {name}: {error}", this);
            enabled = false;
            return;
        }
        Vector3 direction = LevelDesriptor.Instance.GetRoute(routeIndex).position - spawnPoint.position;
        direction.y = 0f;
        Quaternion rotation = direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : spawnPoint.rotation;
        game.SpawnUnitExactPosition(GameController.UntrackedWaveId, spawnPoint.position, rotation, unitType, routeIndex, null);
        hasSpawned = true;
        // No catch-up burst after a slow frame or an interval change.
        timeSinceLastSpawn = 0f;
    }

    /// <summary>Called by the existing world-click handler (UI, dragging and weapon aiming are filtered there).</summary>
    public bool RestoreHPOnClick() {
        if (!CanOperate || currentHP >= maxHP || hpPerClick <= 0f)
            return false;

        currentHP = Mathf.Min(maxHP, currentHP + hpPerClick);
        // Restoring any HP stops spawning. The next depletion starts a fresh spawn cycle.
        hasSpawned = false;
        timeSinceLastSpawn = 0f;
        if (currentHP >= maxHP)
            fullHPDelayRemaining = fullHPDelay;
        return true;
    }

    private void TickHealth(float deltaTime) {
        if (IsDepleted || deltaTime <= 0f)
            return;
        // Only the part of the frame after the grace period drains HP.
        float pausedTime = Mathf.Min(fullHPDelayRemaining, deltaTime);
        fullHPDelayRemaining -= pausedTime;
        currentHP = Mathf.Max(0f, currentHP - hpLossPerSecond * (deltaTime - pausedTime));
    }

    public float GetSpawnDelay(int waveNumber) {
        if (spawnDelayByWave == null || spawnDelayByWave.length == 0)
            return 1f;
        float firstWave = spawnDelayByWave[0].time;
        float lastWave = spawnDelayByWave[spawnDelayByWave.length - 1].time;
        // Explicit clamping also overrides Loop/PingPong wrap modes on the authored curve.
        float delay = spawnDelayByWave.Evaluate(Mathf.Clamp(waveNumber, firstWave, lastWave));
        return float.IsNaN(delay) || float.IsInfinity(delay) ? 1f : Mathf.Max(MinimumSpawnDelay, delay);
    }

    private bool TryValidateSpawn(out string error) {
        error = null;
        var level = LevelDesriptor.Instance;
        if (spawnPoint == null)
            error = "Не назначена точка Spawn Point.";
        else if (level == null || level.routs == null || routeIndex < 0 || routeIndex >= level.routs.Count)
            error = "Route Index не указывает на маршрут в LevelDesriptor.routs.";
        else {
            var route = level.routs[routeIndex];
            if (route == null || route.waypoints == null || route.waypoints.Count < 2 ||
                route.waypoints.Exists(w => w == null || w.marker == null))
                error = "Маршрут должен содержать минимум два назначенных Waypoint Marker и заканчиваться у замка.";
            else if (game.unitsToSpawn == null || game.unitsToSpawn.unitsToSpawn == null ||
                !game.unitsToSpawn.unitsToSpawn.TryGetValue(unitType, out var prefab) || prefab == null)
                error = $"Префаб {unitType} не назначен в GameController.unitsToSpawn.";
        }
        return error == null;
    }
}
