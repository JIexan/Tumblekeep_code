using Gameplay.TasksSystem;
using Gameplay.TasksSystem.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : Singleton<GameController>
{

	// Ключи для tracking панелей
	public const string NextWaveTrackingKey = "NextWave";
	// Вторая панель таймера: двойная волна уровня с развилкой показывает оба рукава, по которым пойдёт
	public const string NextWaveTrackingKey2 = "NextWave2";

	[System.Serializable]
	public class TowerPrefab {
		public string prefabID;
		public BaseTower tower;
	}

	[System.Serializable]
	public class PreviewTowerElement {
		public TowerType towerID;
		public PreviewTower tower;
	}

	public UnitPriceSettings unitPriceSettings;
	public UIController uiController;
	public GameObject selector;
	public GameObject Placeholder;

	public BookUnitDescription unitsToSpawn;

	public List<PreviewTowerElement> previewTowers;
	private PreviewTower activePreviewTower;

	public TowerRadius towerRadius;
	public TowerRadius nextTowerRadius;

	[SerializeField] private TowersController towersController;
	[SerializeField] private ShipsController shipsController;
	[SerializeField] private WavesGenerator wavesGenerator;
	[SerializeField] private ChestsManager chestsManager;
	[SerializeField] private bool infiniteMode = false;
	[SerializeField] private float difficultyMultiplier;

	// Для читов/дебага (дроп сундука нужного тира); геймплей ходит через DropChest из RemoveEnemy.
	public ChestsManager Chests => chestsManager;

	List<Unit> enemies = new List<Unit>();
	private Dictionary<int, int> unitsByWaves = new Dictionary<int, int>();
	private List<int> failedWaves = new List<int>();

	public BaseTower selected;

	float wavesTimer = 0f;
	int currentWaveID = 0;
	private float nextWaveTime = 0;

	private bool gameStarted = false;
	private bool timerStarted = false;

	private int successfulWavesCount = 0;
	private bool primaryTaskCompleted = false;
	private bool victoryClaimed = false;

	// Пауза перед первой генерируемой волной после исчерпания авторского списка
	private const float InfiniteWavesFirstPause = 10f;

	private WavePerformanceTracker performanceTracker;

	public MatchRewards MatchRewards { get; private set; } = new MatchRewards();

	private LevelTasksManager levelTasksManager;

	public float TowersDPS => towersController.GetTowersDPS();
	public CharacteristicSettings CharacteristicSettings => GameConfigs.Instance.CharacteristicSettings;
	public float TowersCount => towersController.GetTowersCount();
	public int MaxTowersCount => towersController.MaxTowersCount;
	public bool HasTowerCapacity => towersController.CanBuildAnotherTower();
	public ShipsController Ships => shipsController;
	public bool IsInfiniteMode => infiniteMode;

	// Read-only состояние матча для автоплей-бота (AUTOPLAY) и дебага
	public int CurrentWaveIndex => currentWaveID;
	public bool IsGameStarted => gameStarted;
	// Авторские волны кончились и генерируемые ещё не включены (EnableInfiniteWaves включает infiniteMode)
	public bool IsAuthoredWavesExhausted => !infiniteMode && nextWaveTime == -1;

	protected override void Awake() {
		base.Awake();

		// Обращение к Instance создаёт менеджер и подписывает его на OnLevelStarted до Start()
		levelTasksManager = LevelTasksManager.Instance;

		if (SavesManager.Instance == null) {
			var savesManager = gameObject.AddComponent<SavesManager>();
		}

		// В Awake, а не в Start: RelicsPanel.Start не должен отрисовать реликвии прошлой катки
		Gameplay.Relics.RelicsManager.ResetRun();
		// Особенность острова — второй источник эффекторов RelicsManager; ResetRun её не трогает
		Gameplay.IslandFeatures.IslandFeaturesManager.ActivateForCurrentLevel();
		Gameplay.Rerolls.RerollsManager.ResetRun();
		// До Start: таски уровня подписываются на боевые события по OnLevelStarted
		Gameplay.Combat.CombatEvents.ResetRun();
	}

	void Start() {
		performanceTracker = new WavePerformanceTracker(wavesGenerator != null ? wavesGenerator.Settings : null);
		selector.SetActive(false);
		InputController.Instance.OnClick += OnClick;
		CoinsManager.Instance.Add(Mathf.Max(0, LevelDesriptor.Instance.startCoinsAmount + Gameplay.LevelUpgrades.LevelUpgradesManager.GetStartCoinsBonus()));
		InGameEventManager.SendEvent(InGameEvents.OnLevelStarted);
		InGameEventManager.Subscribe(InGameEvents.OnTasksStateChanged, OnLevelTasksStateChanged);
		primaryTaskCompleted = false;
		victoryClaimed = false;
		MatchRewards = new MatchRewards();

		// Сохраняем сразу: поражение ничего не банкует, а туториал хаба ждёт именно «вернулся из боя»
		var tutorial = SavesManager.Profile?.tutorial;
		if (tutorial != null && !tutorial.battleVisited) {
			tutorial.battleVisited = true;
			SavesManager.Instance.SaveProfile();
		}

		// Подписываемся на события кораблей
		shipsController.OnSpawningShipArrived += HandleSpawningShipArrived;
		shipsController.OnSpawningShipDeparted += HandleSpawningShipDeparted;
		shipsController.OnMerchantShipClicked += HandleMerchantClicked;
		shipsController.OnMerchantShipArrived += HandleMerchantArrived;
		shipsController.OnMerchantShipDeparted += HandleMerchantDeparted;
	}

	private void OnDestroy() {
		// Таски живут в статическом менеджере и пережили бы сцену вместе с подписками
		levelTasksManager?.OnLevelEnded();
		Gameplay.IslandFeatures.IslandFeaturesManager.Deactivate();
		InputController.Instance.OnClick -= OnClick;
		InGameEventManager.Unsubscribe(InGameEvents.OnTasksStateChanged, OnLevelTasksStateChanged);

		if (shipsController != null) {
			shipsController.OnSpawningShipArrived -= HandleSpawningShipArrived;
			shipsController.OnSpawningShipDeparted -= HandleSpawningShipDeparted;
			shipsController.OnMerchantShipClicked -= HandleMerchantClicked;
			shipsController.OnMerchantShipArrived -= HandleMerchantArrived;
			shipsController.OnMerchantShipDeparted -= HandleMerchantDeparted;
		}
	}

	public void StartGame() {
		gameStarted = true;
		if (LevelDesriptor.Instance.waves.Count == 0) {
			Debug.LogError("Bruh, you need some waves");
			return;
		}
		GameAudioManager.Instance?.SetBattleMusic(true);
		StartNextWave();
	}

	// Поражение сжигает все награды катки: до нажатия «Уплыть» ничего не выплачивается,
	// даже если основной таск уже выполнен — в этом и есть риск остаться ради множителя.
	public void LoseGame() {
		if (!gameStarted)
			return;
		gameStarted = false;
		uiController.HideLeaveButton();
		GameAudioManager.Instance?.SetBattleMusic(false);
		GameAudio.Play(GameSound.Defeat);
		uiController.ShowLoseWindow();
	}

	private void OnLevelTasksStateChanged(object obj) {
		var currentTasks = levelTasksManager.LevelTasksCollection;
		if (currentTasks == null || primaryTaskCompleted || !currentTasks.PrimaryTaskCompleted)
			return;

		// Победа не выдаётся автоматически: в HUD появляется кнопка «Уплыть», игрок сам решает,
		// когда забрать награду (доп. таски поднимают множитель, поражение до ухода сжигает всё).
		primaryTaskCompleted = true;
		uiController.ShowLeaveButton();

		// Авторские волны уже кончились — чтобы катка не стояла, переключаемся на генерируемые
		if (IsAuthoredWavesExhausted)
			EnableInfiniteWaves();
	}

	/// <summary>
	/// Кнопка «Уплыть» в HUD: зафиксировать награды с текущим множителем и показать окно победы.
	/// Назад пути нет — катка завершена.
	/// </summary>
	public void LeaveWithVictory() {
		if (!gameStarted || !primaryTaskCompleted || victoryClaimed)
			return;
		victoryClaimed = true;

		var currentTasks = levelTasksManager.LevelTasksCollection;
		BankRewards(currentTasks);
		WinGame(currentTasks);
	}

	/// <summary>
	/// Фиксирует накопленные за катку награды с текущим множителем и выплачивает дельту в профиль.
	/// </summary>
	private void BankRewards(LevelTasksCollection tasks) {
		float multiplier = MatchRewards.GetMultiplier(tasks.SecondaryCompletedCount);
		var (xp, crystals) = MatchRewards.Bank(multiplier);

		if (crystals > 0) {
			SavesManager.Profile.AddItem(ItemType.Crystal, crystals);
			SavesManager.Instance.SaveProfile();
		}

		string levelId = Gameplay.IslandExperience.IslandExperienceManager.CurrentLevelId;
		if (string.IsNullOrEmpty(levelId)) {
			Debug.LogWarning("MatchRewards: нет CurrentLevelContext.Level — island XP не начислен");
		} else if (xp > 0) {
			Gameplay.IslandExperience.IslandExperienceManager.AddXP(levelId, xp);
		}
	}

	private void WinGame(LevelTasksCollection currentTasks) {
		gameStarted = false;
		uiController.HideLeaveButton();
		GameAudioManager.Instance?.SetBattleMusic(false);
		GameAudio.Play(GameSound.Victory);
		uiController.ShowWinWindow(new VictoryWindowData {
			LevelId = Gameplay.IslandExperience.IslandExperienceManager.CurrentLevelId,
			BankedXP = MatchRewards.BankedXP,
			AccruedCrystals = MatchRewards.AccruedCrystals,
			BankedCrystals = MatchRewards.BankedCrystals,
			SecondaryCount = currentTasks.SecondaryCompletedCount,
		});
	}

	/// <summary>
	/// Переключение на генерируемые волны, когда авторский список уже исчерпан и следующей волны нет
	/// (игрок остался после основного таска; автоплей без тасков).
	/// </summary>
	public void EnableInfiniteWaves() {
		if (infiniteMode)
			return;
		infiniteMode = true;
		nextWaveTime = wavesTimer = InfiniteWavesFirstPause;
		timerStarted = true;
	}

	public TowerUpgradeSettings.TowerSettings GetTowerSettings(TowerType type, int level) {
		return towersController.GetTowerSettings(type, level);
	}

	private PreviewTower GetPreviewTowerByType(TowerType type) {
		foreach (var tower in previewTowers) {
			if (tower.towerID == type)
				return tower.tower;
		}
		return null;
	}

	private void UpdateNewTowerDirections(BaseTower newTower) {
		if (newTower == null)
			return;
		newTower.UpdateDirections(selected.GetDirectionVector(selected.towerDirection), selected.GetDirectionVector(selected.weaponDirection));
		newTower.weaponDirection = selected.weaponDirection;
		newTower.towerDirection = selected.towerDirection;
	}

	public void ShowPreviewTower(int type) {
		HidePreviewTower();

		if (selected != null && selected.towerType == TowerType.Placeholder) {
			(selected as BuildingPlaceholder).SetVisualsActive(false);
			var settings = towersController.GetTowerSettings((TowerType)type, 0);

			activePreviewTower = GetPreviewTowerByType((TowerType)type);
			Vector3 baseDirection = Vector3.forward;
			Vector3 weaponDirection = Vector3.forward;
			BuildingPlaceholder placeholder = selected as BuildingPlaceholder;
			if (placeholder != null) {
				baseDirection = placeholder.GetDirectionVector(placeholder.towerDirection);
				weaponDirection = placeholder.GetDirectionVector(placeholder.weaponDirection);
			}
			activePreviewTower.gameObject.SetActive(true);
			activePreviewTower.Init(selected.transform.position, baseDirection, weaponDirection);

			ShowTowerRadius(towerRadius, selected.transform.position, settings.radius);
		}
	}

	public void ShowUpgradeTowerPreview(int upgrade) {
		if (selected != null && selected.towerType != TowerType.Placeholder) {
			var settings = towersController.GetTowerSettings(selected.Id);
			var nextTower = settings.GetNextUpgrade(upgrade);
			if (nextTower != null)
				ShowTowerRadius(nextTowerRadius, selected.transform.position, nextTower.radius);
		}
	}

	public void HideUpgradeTowerPreview() {
		HideRadius(nextTowerRadius);
	}

	public void BuildTowerOnSelectedPlot(int type) {
		if (selected != null && selected.towerType == TowerType.Placeholder) {
			if (!HasTowerCapacity) {
				HidePreviewTower();
				DeselectTower();
				uiController.ShowTowerLimitReached();
				return;
			}
			if (!Gameplay.LevelUpgrades.LevelUpgradesManager.IsTowerTypeAvailable((TowerType)type)
				|| !CoinsManager.Instance.CanPay(towersController.GetTowerPrice((TowerType)type)))
				return;

			var tower = towersController.BuildTower(type, selected.transform.position);
			if (tower == null)
				return;
			CameraController.Instance.Shake(1f);
			InGameEventManager.SendEvent(InGameEvents.OnTowerBuilt, tower);

			UpdateNewTowerDirections(tower);

			HidePreviewTower();
			Destroy(selected.gameObject);
			selected = null;
			uiController.HideTowerPanel();
			selector.SetActive(false);
			HideRadius(towerRadius);
			CoinsManager.Instance.Substract(towersController.GetTowerPrice((TowerType)type));
		}
	}

	public void OnClick() {
		Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
		HidePreviewTower();

		RaycastHit hit;
		if (selected != null) {
			HideRadius(nextTowerRadius);
			HideRadius(towerRadius);
		}
		if (Physics.Raycast(r, out hit, 200, LayerMask.GetMask("Clickable"))) {
			var distraction = hit.collider.GetComponentInParent<PlayerDistraction>();
			if (distraction != null && distraction.isActiveAndEnabled) {
				DeselectTower();
				distraction.RestoreHPOnClick();
				return;
			}
			var tower = hit.collider.gameObject.GetComponent<BaseTower>();
			if (tower != null) {
				OnTowerClick(tower);
			} else {
				var chest = hit.collider.gameObject.GetComponent<RewardChest>();
				if (chest != null)
					chestsManager.OnChestClick(chest);
			}
		} else {
			DeselectTower();
		}
	}

	private void DeselectTower() {
		selector.SetActive(false);
		selected = null;
		HideRadius(towerRadius);
		HideRadius(nextTowerRadius);
		uiController.HideTowerPanel();
	}

	public void HideRadius(TowerRadius tr) {
		if (tr.gameObject.activeInHierarchy) {
			tr.Stop();
			tr.gameObject.SetActive(false);
		}
	}

	// Волна вне учёта (чит-парад, объекты отвлечения): юниты не считаются в успех/провал волн — ни сундуков за зачистку,
	// ни роста сложности, ни pressure. Дети таких юнитов (экипаж, порталы) наследуют id и тоже вне учёта
	public const int UntrackedWaveId = -1000;

#if CHEATS
	/// <summary>
	/// Высадить произвольную волну мимо расписания. Таймер обычных волн встаёт и сам продолжится, когда корабль
	/// этой волны уплывёт (HandleSpawningShipDeparted). false — на точке спавна ещё стоит корабль, надо дождаться отплытия
	/// </summary>
	public bool StartCheatWave(Wave wave) {
		foreach (var group in wave.groups) {
			if (shipsController.HasSpawningShip(group.spawnPointIdx))
				return false;
		}

		UntrackNextWavePanels();
		timerStarted = false;
		shipsController.SpawnShipsForWave(wave);
		return true;
	}
#endif

	private void UntrackNextWavePanels() {
		InGameEventManager.SendEvent(InGameEvents.OnUntrackingRequested, new UntrackingRequest(NextWaveTrackingKey));
		InGameEventManager.SendEvent(InGameEvents.OnUntrackingRequested, new UntrackingRequest(NextWaveTrackingKey2));
	}

	public void AddEnemy(Unit enemy) {
		enemies.Add(enemy);
		if (enemy.WaveId == UntrackedWaveId)
			return;
		if (unitsByWaves.ContainsKey(enemy.WaveId))
			unitsByWaves[enemy.WaveId]++;
		else if (!failedWaves.Contains(enemy.WaveId))
			unitsByWaves.Add(enemy.WaveId, 1);
	}

	public void RemoveEnemy(Unit enemy, bool killed) {
		enemies.Remove(enemy);
		if (enemy.WaveId == UntrackedWaveId)
			return;
		if (unitsByWaves.ContainsKey(enemy.WaveId) && killed) {
			unitsByWaves[enemy.WaveId]--;
			if (unitsByWaves[enemy.WaveId] <= 0) {
				successfulWavesCount++;
				performanceTracker?.OnWaveCleared(enemy.WaveId);
				chestsManager.DropChest(enemy.transform.position, enemy.transform.forward);
			}
		} else {
			if (!failedWaves.Contains(enemy.WaveId)) {
				failedWaves.Add(enemy.WaveId);
			}
			performanceTracker?.OnWaveLeaked(enemy.WaveId);
			unitsByWaves.Remove(enemy.WaveId);
		}
	}

	public List<Unit> GetAllEnemies() {
		return enemies;
	}

	public GameObject GetEnemy(Vector3 position, float radius, TowerType type, List<Unit> exceptions = null) {
		float sqrRadius = radius * radius;
		foreach (var enemy in enemies) {
			if ((enemy.transform.position - position).sqrMagnitude <= sqrRadius && (enemy.targetMask & (int)type) != 0 && (exceptions != null ? !exceptions.Contains(enemy) : true))
				return enemy.gameObject;
		}
		return null;
	}

	public GameObject GetEnemyWithoutEffect(Vector3 position, float radius, TowerType type, UnitEffect effect) {
		float sqrRadius = radius * radius;
		foreach (var enemy in enemies) {
			if ((enemy.transform.position - position).sqrMagnitude <= sqrRadius && (enemy.targetMask & (int)type) != 0 && !enemy.HasEffect(effect))
				return enemy.gameObject;
		}
		return null;
	}

	public Unit[] GetEnemiesInRadius(Vector3 position, float radius, TowerType type) {
		List<Unit> res = new List<Unit>();
		float sqrRadius = radius * radius;
		foreach (var enemy in enemies) {
			if ((enemy.transform.position - position).sqrMagnitude <= sqrRadius && (enemy.targetMask & (int)type) != 0)
				res.Add(enemy);
		}
		return res.ToArray();
	}

	/// <summary>
	/// Все враги в радиусе по горизонтали (высота не учитывается), без фильтра по targetMask —
	/// для спеллов, которые бьют и наземных, и летающих (например, SpellExplode)
	/// </summary>
	public Unit[] GetAllEnemiesInRadius(Vector3 position, float radius) {
		List<Unit> res = new List<Unit>();
		float sqrRadius = radius * radius;
		foreach (var enemy in enemies) {
			Vector3 d = enemy.transform.position - position;
			d.y = 0f;
			if (d.sqrMagnitude <= sqrRadius)
				res.Add(enemy);
		}
		return res.ToArray();
	}

	public Unit[] GetDamagedEnemiesInRadius(Vector3 position, float radius) {
		List<Unit> res = new List<Unit>();
		float sqrRadius = radius * radius;
		foreach (var enemy in enemies) {
			if ((enemy.transform.position - position).sqrMagnitude <= sqrRadius && enemy.Damaged)
				res.Add(enemy);
		}
		return res.ToArray();
	}

	public int GetDamagedEnemiesInRadiusCount(Vector3 position, float radius) {
		int res = 0;
		float sqrRadius = radius * radius;
		foreach (var enemy in enemies) {
			if ((enemy.transform.position - position).sqrMagnitude <= sqrRadius && enemy.Damaged)
				res++;
		}
		return res;
	}

	public void HidePreviewTower() {
		if (activePreviewTower != null) {
			if (selected != null && selected.towerType == TowerType.Placeholder) {
				(selected as BuildingPlaceholder).SetVisualsActive(true);
			}
			activePreviewTower.gameObject.SetActive(false);
			activePreviewTower = null;
		}
	}

	public void OnTowerClick(BaseTower tower) {
		selector.SetActive(true);
		selector.transform.position = tower.transform.position;
		selected = tower;
		if (tower.towerType == TowerType.Placeholder) {
			var placeholder = tower as BuildingPlaceholder;
			if (!HasTowerCapacity) {
				DeselectTower();
				uiController.ShowTowerLimitReached();
			} else {
				uiController.ShowNewTowerPanel();
				InGameEventManager.SendEvent(InGameEvents.OnBuildSpotClicked, placeholder);
			}
		} else if (tower.CanUpgrade) {
			uiController.ShowUpgradeTowerWindow(tower, DeselectTower);
		} else {
			uiController.HideTowerPanel();
		}
		if (tower.towerType != TowerType.Placeholder)
			ShowTowerRadius(towerRadius, tower.transform.position, tower.Radius);
	}

	private void ShowTowerRadius(TowerRadius tr, Vector3 pos, float radius) {
		pos.y = 0.2f;
		tr.transform.position = pos;
		tr.Init(radius);
		tr.gameObject.SetActive(true);
		tr.Play();
	}

	public Unit SpawnUnitExactPosition(int waveId, Vector3 position, Quaternion rotation, UnitType type, int routeId, Waypoint currentWaypoint, float hpMultiplier = 1f, bool isBoss = false, float rewardMultiplier = -1f) {
		GameObject enemy = Instantiate(unitsToSpawn.unitsToSpawn[type].gameObject, position, rotation);
		Unit u = enemy.GetComponent<Unit>();
		u.Init(waveId, type, hpMultiplier, isBoss, rewardMultiplier);
		// Единая точка спавна (включая порталы/клетки/печи) — открытие записи в книге здесь
		if (BookDiscovery.TryDiscoverUnit(type))
			InGameEventManager.SendEvent(InGameEvents.OnUnitDiscovered, type);
		if (routeId != -1)
			u.SetRouteIndex(routeId);
		if (currentWaypoint != null) {
			u.SetCurrentWaypoint(currentWaypoint);
		}
		// Экипаж тарана по умолчанию берётся с префаба (RamUnit.settings);
		// авторская волна может переопределить его через Wave.ramSettings, генератор — нет
		if (u is RamUnit ru) {
			var wave = shipsController.GetCurrentWave();
			if (wave != null && wave.ramSettings != null)
				ru.SetRamSettings(wave.ramSettings);
		}

		AddEnemy(u);
		uiController.AddHealthBar(u);
		return u;
	}

	public Unit SpawnUnit(int waveId, Vector3 position, Quaternion rotation, UnitType type, int routeId, Waypoint currentWaypoint, float hpMultiplier = 1f, bool isBoss = false, float rewardMultiplier = -1f) {
		Vector3 pos = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));
		pos.Normalize();
		pos *= UnityEngine.Random.Range(0f, 2f);

		return SpawnUnitExactPosition(waveId, position + pos, rotation, type, routeId, currentWaypoint, hpMultiplier, isBoss, rewardMultiplier);
	}

	public void StartNextWave() {
		// Убираем панель следующей волны при старте новой
		UntrackNextWavePanels();

		InGameEventManager.SendEvent(InGameEvents.OnWaveStarted, currentWaveID + 1);
		if (infiniteMode) {
			// Итог прошлой волны (потеря ХП замка) учитывается до расчёта сложности новой
			performanceTracker.OnWaveStarting(Castle.Instance != null ? Castle.Instance.CurrentHP : 0f);
			float difficulty = Mathf.Pow(difficultyMultiplier, successfulWavesCount) * performanceTracker.Pressure;
			Debug.Log($"Starting wave {currentWaveID}. Succeded waves {successfulWavesCount}. Difficulty {difficulty:F2} (pressure {performanceTracker.Pressure:F2})");
			var result = wavesGenerator.GenerateWave(currentWaveID, difficulty);
			shipsController.SpawnShipsForWave(result.wave);
			nextWaveTime = wavesTimer = result.pauseTime;
			performanceTracker.RegisterWave(currentWaveID, result.expectedKillTime);
			Instance.uiController.UpdateWave(currentWaveID + 1);
			currentWaveID++;
		} else {
			while (LevelDesriptor.Instance.waves[currentWaveID].disabled) {
				currentWaveID++;
			}

			Instance.uiController.UpdateWave(currentWaveID + 1);
			shipsController.SpawnShipsForWave(LevelDesriptor.Instance.waves[currentWaveID]);

			currentWaveID++;

			if (currentWaveID >= LevelDesriptor.Instance.waves.Count) {
				// Основной таск уже выполнен, а игрок остался — дальше волны генерируются
				if (primaryTaskCompleted) {
					infiniteMode = true;
					nextWaveTime = wavesTimer = InfiniteWavesFirstPause;
				} else {
					nextWaveTime = -1;
				}
			} else {
				nextWaveTime = wavesTimer = LevelDesriptor.Instance.waves[currentWaveID].spawnTime;
			}
		}
		timerStarted = false;
	}

	private void HandleSpawningShipArrived(SpawningShip ship) {
		var waveSpawner = ship.GetComponent<WaveSpawner>();
		if (waveSpawner != null) {
			waveSpawner.Init(shipsController.GetCurrentWave(), ship.spawnPointID);
		}
	}

	private void HandleSpawningShipDeparted(int spawnPointID) {
		// Волна полностью высажена — с этого момента трекается время её зачистки
		performanceTracker?.OnWaveSpawnCompleted(currentWaveID - 1);

		// В infiniteMode волны генерируются и следующая есть всегда: авторский список
		// не индексируем (он короче, и панель молча пропадала после его конца)
		if (infiniteMode || currentWaveID < LevelDesriptor.Instance.waves.Count) {
			if (infiniteMode && wavesGenerator != null) {
				// Маршрут следующей волны решается сейчас, и панель таймера встаёт на её рукав — игрок видит, откуда
				// ждать врага, и успевает ответить. Двойная волна показывает обе панели; основной ключ всегда у первой
				// (по нему кнопка раша ищется автоплеем)
				var plan = wavesGenerator.PlanNextWave(currentWaveID);
				for (int i = 0; i < plan.routes.Length && i < 2; i++)
					RequestNextWavePanel(i == 0 ? NextWaveTrackingKey : NextWaveTrackingKey2, wavesGenerator.GetRoutePreviewPoint(plan.routes[i]));
			} else {
				RequestNextWavePanel(NextWaveTrackingKey, LevelDesriptor.Instance.spawnPoints[0].point.transform);
			}
		}
		timerStarted = true;
	}

	private void RequestNextWavePanel(string key, Transform target) {
		if (target == null)
			return;
		InGameEventManager.SendEvent(InGameEvents.OnTrackingTimerRequested,
			new TrackingTimerRequest(
				key,
				TrackingPanelType.NextWave,
				target,
				wavesTimer
			));
	}

	private void HandleMerchantArrived(MerchantShip ship) {
		float waitDuration = shipsController.MerchantSettings.waitDuration;

		// Торговец прибыл — показываем трекинг-панель с таймером
		InGameEventManager.SendEvent(InGameEvents.OnTrackingTimerRequested,
			new TrackingTimerRequest(
				ship,
				TrackingPanelType.Merchant,
				ship.transform,
				waitDuration,
				() => HandleMerchantClicked(ship)
			));
	}

	private void HandleMerchantDeparted(MerchantShip ship) {
		// Торговец уплыл — убираем трекинг-панель
		InGameEventManager.SendEvent(InGameEvents.OnUntrackingRequested, new UntrackingRequest(ship));
	}

	private void HandleMerchantClicked(MerchantShip ship) {
		var data = new MerchantWindowData {
			Ship = ship,
			PurchaseCount = shipsController.MerchantPurchaseCount
		};
		uiController.ShowWindow(WindowType.MerchantWindow, data);
	}

	public void Update() {
		if (Input.touchCount == 3 || Input.GetKeyDown(KeyCode.U)) {
			if (uiController.gameObject.activeInHierarchy) {
				uiController.gameObject.SetActive(false);
			} else {
				uiController.gameObject.SetActive(true);
			}
		}

		if (!gameStarted)
			return;

		levelTasksManager.Tick();

		if (timerStarted) {
			wavesTimer -= Time.deltaTime;
			if (wavesTimer <= 0 && nextWaveTime != -1) {
				StartNextWave();
			}
		}
	}

	public void OnRestart() {
		ResetLevel();
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	public void OnMainMenu() {
		SceneManager.LoadScene("MainMenu");
	}

	private void ResetLevel() {
		enemies = new List<Unit>();
		shipsController.Reset();
		wavesTimer = 0f;
		currentWaveID = 0;
		nextWaveTime = 0;
		gameStarted = false;
		timerStarted = false;
		successfulWavesCount = 0;
		primaryTaskCompleted = false;
		victoryClaimed = false;
		MatchRewards = new MatchRewards();
		unitsByWaves.Clear();
		failedWaves.Clear();
		performanceTracker?.Reset();
		Gameplay.Relics.RelicsManager.ResetRun();
		Gameplay.Rerolls.RerollsManager.ResetRun();

		// Сброс заданий уровня (сначала таски — их пробы отписываются от CombatEvents сами)
		levelTasksManager.Clear();
		Gameplay.Combat.CombatEvents.ResetRun();
	}

	public float GetRouteCoverage(int r, float routeLength) {
		return towersController.GetRouteCoverage(r, routeLength);
	}

	public List<UpgradeData> GenerateUpgradeOptions(BaseTower tower) {
		return TowerUpgradeGenerator.GenerateUpgradeOptions(tower, GameConfigs.Instance.TowerUpgradeRules, GameConfigs.Instance.RarityConfig, GameConfigs.Instance.CharacteristicSettings);
	}

	// Единая цена башни для UI и списания (со скидками реликвий из TowersController.GetTowerPrice)
	public int GetTowerPrice(TowerType type) {
		return towersController.GetTowerPrice(type);
	}

	public List<BaseTower> GetAllTowers() {
		if (towersController != null)
			return towersController.GetAllTowers();
		return new List<BaseTower>();
	}

	public bool CanBuildAnotherTower() {
		return towersController.CanBuildAnotherTower() && CoinsManager.Instance.CanPay(towersController.GetTowerPrice(TowerType.Arrow));
	}
}
