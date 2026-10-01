#if AUTOPLAY
using System.Collections.Generic;
using Gameplay.LevelUpgrades;
using Gameplay.Relics;
using UnityEngine;

namespace Autoplay {
	/// <summary>
	/// Игрок-бот одного матча: строит случайные башни на свободных точках, апгрейдит по максимуму DPS,
	/// торопит волны, покупает и открывает сундуки, иногда покупает у торговца, кастует спеллы
	/// и собирает RunStats из тех же событий, что и таски уровня. Живёт в сцене уровня.
	/// </summary>
	public class AutoplayBot : MonoBehaviour {
		private static readonly TowerType[] buyableTypes = { TowerType.Laser, TowerType.Arrow, TowerType.Mortar, TowerType.Electric };

		private AutoplayRunner runner;
		private AutoplayConfig cfg;
		private RunStats stats;
		private GameController gc;
		private UIController ui;
		private RouteModel routes;
		private SpellCaster spells;
		private AutoplayWindowHandler windowHandler;
		private TaskMetricsRecorder taskMetrics;
		private System.Random rng;

		private float gameTime;
		private float tickTimer;
		private bool wasClear;
		private float clearedAt;
		private readonly List<BuildingPlaceholder> placeholders = new List<BuildingPlaceholder>();
		private bool placeholdersDirty = true;
		private readonly List<TowerType> affordableTypes = new List<TowerType>();
		private readonly List<RewardChest> chestBuffer = new List<RewardChest>();
		private readonly List<Unit> aliveBuffer = new List<Unit>();
		private readonly HashSet<int> leakedWaves = new HashSet<int>();
		private MerchantShip pendingMerchant;
		private bool merchantDecided;
		private float lastCastleHP = -1f;
		private int lastAccruedCrystals;
		private bool subscribed;
		private bool finished;

		public RunStats Stats => stats;
		public RouteModel Routes => routes;
		public System.Random Rng => rng;
		public float GameTime => gameTime;
		public GameController Game => gc;
		public WindowsManager Windows => ui != null ? ui.Windows : null;
		public bool ChestBusy => gc != null && gc.Chests != null && gc.Chests.IsBusy;
		public int Coins => CoinsManager.Instance != null ? CoinsManager.Instance.Amount : 0;

		public void Init(AutoplayRunner runner, AutoplayConfig cfg, RunStats stats) {
			this.runner = runner;
			this.cfg = cfg;
			this.stats = stats;
			rng = new System.Random(stats.seed);
			gc = FindObjectOfType<GameController>();
			ui = gc != null ? gc.uiController : null;
			routes = new RouteModel();
			spells = new SpellCaster(this, cfg, stats);
			windowHandler = new AutoplayWindowHandler(this, stats);
			var level = LevelDesriptor.Instance;
			stats.authoredWavesCount = level != null && level.waves != null ? level.waves.Count : 0;
			taskMetrics = new TaskMetricsRecorder(stats);
			Subscribe();
			// In-place прогон стартует посреди идущего матча — OnLevelStarted уже прошёл
			if (gc != null && gc.IsGameStarted)
				taskMetrics.Start();
		}

		private void OnLevelStarted(object param) {
			taskMetrics.Start();
		}

		private void Subscribe() {
			if (subscribed)
				return;
			subscribed = true;
			InGameEventManager.Subscribe(InGameEvents.OnLevelStarted, OnLevelStarted);
			InGameEventManager.Subscribe(InGameEvents.OnWaveStarted, OnWaveStarted);
			InGameEventManager.Subscribe(InGameEvents.OnCoinsAmountChanged, OnCoinsAmountChanged);
			InGameEventManager.Subscribe(InGameEvents.OnUnitKilled, OnUnitKilled);
			InGameEventManager.Subscribe(InGameEvents.OnChestOpenned, OnChestOpened);
			InGameEventManager.Subscribe(InGameEvents.OnMerchantPurchase, OnMerchantPurchase);
			InGameEventManager.Subscribe(InGameEvents.OnTowerBuilt, OnTowerBuilt);
			InGameEventManager.Subscribe(InGameEvents.OnRelicAdded, OnRelicAdded);
			if (gc != null && gc.Ships != null) {
				gc.Ships.OnMerchantShipArrived += OnMerchantArrived;
				gc.Ships.OnMerchantShipDeparted += OnMerchantDeparted;
			}
		}

		private void Unsubscribe() {
			if (!subscribed)
				return;
			subscribed = false;
			InGameEventManager.Unsubscribe(InGameEvents.OnLevelStarted, OnLevelStarted);
			InGameEventManager.Unsubscribe(InGameEvents.OnWaveStarted, OnWaveStarted);
			InGameEventManager.Unsubscribe(InGameEvents.OnCoinsAmountChanged, OnCoinsAmountChanged);
			InGameEventManager.Unsubscribe(InGameEvents.OnUnitKilled, OnUnitKilled);
			InGameEventManager.Unsubscribe(InGameEvents.OnChestOpenned, OnChestOpened);
			InGameEventManager.Unsubscribe(InGameEvents.OnMerchantPurchase, OnMerchantPurchase);
			InGameEventManager.Unsubscribe(InGameEvents.OnTowerBuilt, OnTowerBuilt);
			InGameEventManager.Unsubscribe(InGameEvents.OnRelicAdded, OnRelicAdded);
			if (gc != null && gc.Ships != null) {
				gc.Ships.OnMerchantShipArrived -= OnMerchantArrived;
				gc.Ships.OnMerchantShipDeparted -= OnMerchantDeparted;
			}
		}

		private void OnDestroy() {
			Unsubscribe();
		}

		private void Update() {
			if (finished || runner == null || runner.State != RunnerState.Playing || gc == null)
				return;
			windowHandler.Tick();
			if (Time.timeScale <= 0f || runner.AnyWindowActive())
				return;

			float dt = Time.deltaTime;
			gameTime += dt;
			TrackCastle();
			taskMetrics.Tick(Time.time, stats.maxWaveReached);

			if (cfg.maxWave > 0 && stats.maxWaveReached >= cfg.maxWave) {
				runner.RequestEndRun("WaveLimit");
				return;
			}
			if (cfg.maxGameSeconds > 0f && gameTime >= cfg.maxGameSeconds) {
				runner.RequestEndRun("TimeLimit");
				return;
			}

			tickTimer += dt;
			if (tickTimer < cfg.decisionInterval)
				return;
			tickTimer = 0f;
			Decide();
		}

		private void Decide() {
			CollectAlive();
			bool clear = aliveBuffer.Count == 0 && !AnySpawningShip();
			if (clear && !wasClear)
				clearedAt = gameTime;
			wasClear = clear;

			TryContinueAfterAuthoredWaves(clear);
			TryRushWave(clear);
			TryUpgrade();
			TryBuild();
			TryOpenChest();
			if (runner.AnyWindowActive())
				return;
			TryMerchant();
			if (runner.AnyWindowActive())
				return;
			spells.Tick(gameTime, aliveBuffer);
		}

		// ---------------- decisions ----------------

		private void CollectAlive() {
			aliveBuffer.Clear();
			foreach (var unit in gc.GetAllEnemies()) {
				if (unit != null && !unit.isDead)
					aliveBuffer.Add(unit);
			}
		}

		private bool AnySpawningShip() {
			var level = LevelDesriptor.Instance;
			if (level == null || level.spawnPoints == null || gc.Ships == null)
				return false;
			for (int i = 0; i < level.spawnPoints.Count; i++) {
				if (gc.Ships.HasSpawningShip(i))
					return true;
			}
			return false;
		}

		// Авторские волны кончились: без тасков основной не выполнится, и игра просто стоит. Включаем
		// генерируемые волны тем же путём, что игра после основного таска, но только когда последняя волна добита
		private void TryContinueAfterAuthoredWaves(bool clear) {
			if (!clear || !gc.IsAuthoredWavesExhausted)
				return;
			gc.EnableInfiniteWaves();
			Log("authored waves exhausted: switching to infinite mode");
		}

		private void TryRushWave(bool clear) {
			if (!cfg.rushWaves || !clear || gameTime - clearedAt < cfg.rushDelay)
				return;
			if (ui == null || ui.trackingPanelsController == null)
				return;
			var panel = ui.trackingPanelsController.GetPanel(GameController.NextWaveTrackingKey);
			if (panel == null || !panel.IsTimerActive)
				return;
			panel.OnPanelClicked();
			stats.wavesRushed++;
			Log("rush next wave");
		}

		private void TryUpgrade() {
			foreach (var tower in gc.GetAllTowers()) {
				if (tower == null || tower.towerType == TowerType.Placeholder || !tower.CanUpgrade)
					continue;
				var options = tower.GetUpgradeOptions();
				var best = UpgradeScorer.Best(tower, options, rng, out string why);
				if (best == null)
					continue;
				float before = tower.DPS;
				best.Upgrade();
				stats.upgradesApplied++;
				if (best is TowerTraitUpgradeData)
					stats.upgradeTraits++;
				routes.MarkDirty();
				Log($"upgrade {tower.towerType} -> L{tower.Level}: {why}, dps {before:F1} -> {tower.DPS:F1}");
				return; // одна башня за тик
			}
		}

		private void TryBuild() {
			if (!gc.HasTowerCapacity)
				return;
			RefreshPlaceholders();
			if (placeholders.Count == 0)
				return;
			affordableTypes.Clear();
			foreach (var type in buyableTypes) {
				if (LevelUpgradesManager.IsTowerTypeAvailable(type) && CoinsManager.Instance.CanPay(gc.GetTowerPrice(type)))
					affordableTypes.Add(type);
			}
			if (affordableTypes.Count == 0)
				return;

			var spot = placeholders[rng.Next(placeholders.Count)];
			var towerType = affordableTypes[rng.Next(affordableTypes.Count)];
			int price = gc.GetTowerPrice(towerType);
			gc.selected = spot;
			gc.BuildTowerOnSelectedPlot((int)towerType);
			placeholdersDirty = true;
			Log($"build {towerType} for {price} at {spot.name}");
		}

		private void RefreshPlaceholders() {
			if (!placeholdersDirty) {
				placeholders.RemoveAll(p => p == null);
				return;
			}
			placeholdersDirty = false;
			placeholders.Clear();
			foreach (var placeholder in FindObjectsOfType<BuildingPlaceholder>()) {
				if (placeholder != null && placeholder.gameObject.activeInHierarchy)
					placeholders.Add(placeholder);
			}
		}

		private void TryOpenChest() {
			var chests = gc.Chests;
			if (chests == null || chests.IsBusy || chests.ActiveChests == null || chests.ActiveChests.Count == 0)
				return;
			var settings = GameConfigs.Instance.ChestsSettings;
			if (settings == null)
				return;
			chestBuffer.Clear();
			chestBuffer.AddRange(chests.ActiveChests);
			foreach (var chest in chestBuffer) {
				if (chest == null || chest.IsOpening)
					continue;
				var tier = settings.GetTier(chest.Tier);
				if (tier == null)
					continue;
				int price = chests.GetChestPrice(tier);
				if (!CoinsManager.Instance.CanPay(price)) {
					stats.chestsUnaffordable++;
					continue;
				}
				if (chests.TryBuyAndOpen(chest)) {
					RunStats.Inc(stats.chestsOpenedByTier, chest.Tier.ToString());
					Log($"open {chest.Tier} chest for {price}");
					return;
				}
			}
		}

		private void TryMerchant() {
			if (pendingMerchant == null || merchantDecided || !pendingMerchant.IsArrived)
				return;
			merchantDecided = true;
			if (rng.NextDouble() >= cfg.merchantChance) {
				Log("merchant: skipped by chance");
				return;
			}
			ui.ShowWindow(WindowType.MerchantWindow, new MerchantWindowData {
				Ship = pendingMerchant,
				PurchaseCount = gc.Ships != null ? gc.Ships.MerchantPurchaseCount : 0,
			});
			stats.merchantWindowsOpened++;
			Log("merchant: window opened");
		}

		// «Перо феникса»: HP замка подскочило без нашего лечения
		private void TrackCastle() {
			var castle = Castle.Instance;
			if (castle == null)
				return;
			float hp = castle.CurrentHP;
			if (lastCastleHP >= 0f && hp > lastCastleHP + 0.2f * Mathf.Max(castle.MaxHP, 1f) && gameTime - spells.LastHealTime > 1f) {
				stats.secondLifeUsed = true;
				Log("castle second life (phoenix feather)");
			}
			lastCastleHP = hp;
		}

		// ---------------- events ----------------

		private void OnWaveStarted(object param) {
			int wave = param is int number ? number : stats.wavesStarted + 1;
			stats.wavesStarted++;
			stats.maxWaveReached = Mathf.Max(stats.maxWaveReached, wave);
			if (wave > stats.authoredWavesCount)
				stats.reachedInfinite = true;
			spells.OnWaveStarted();
			wasClear = false;

			var castle = Castle.Instance;
			stats.waves.Add(new WaveSnapshot {
				wave = wave,
				gameTime = gameTime,
				castleHP = castle != null ? castle.CurrentHP : 0f,
				castleNormalized = castle != null ? castle.NormalizedHP : 0f,
				coins = Coins,
				goldCollected = stats.goldCollected,
				goldSpent = stats.goldSpent,
				towers = stats.towersBuilt,
				upgrades = stats.upgradesApplied,
				chests = stats.chestsOpened,
				kills = stats.unitsKilled,
				bosses = stats.bossesKilled,
				merchantPurchases = stats.merchantPurchases,
				spells = stats.spellsCast,
				towersDps = gc != null ? gc.TowersDPS : 0f,
				accruedXP = AccruedXP,
				accruedCrystals = AccruedCrystals,
				taskMetrics = taskMetrics.Snapshot(),
			});
			Log($"wave {wave}: castleHP={(castle != null ? castle.CurrentHP : 0f):F0} coins={Coins} crystals={AccruedCrystals} towers={stats.towersBuilt} dps={(gc != null ? gc.TowersDPS : 0f):F1} alive={(gc != null ? gc.GetAllEnemies().Count : 0)}");
		}

		private void OnCoinsAmountChanged(object param) {
			if (!(param is int delta))
				return;
			if (delta > 0)
				stats.goldCollected += delta;
			else
				stats.goldSpent -= delta;
		}

		private void OnUnitKilled(object param) {
			var unit = param as Unit;
			if (unit == null)
				return;
			// Дошедший до замка юнит убивает себя сам (Kill(false)) и тоже шлёт OnUnitKilled
			bool leak = unit.ReachedCastle;
			string unitType = unit.Type.ToString();
			if (leak) {
				stats.leaks++;
				stats.castleDamageTaken += unit.damagePower;
				RunStats.Inc(stats.leaksByUnitType, unitType);
				int wave = unit.WaveId + 1; // WaveId нулевой, номера волн в событиях — с единицы
				if (leakedWaves.Add(unit.WaveId))
					stats.wavesLeaked++;
				if (stats.firstLeakWave < 0)
					stats.firstLeakWave = wave;
			} else {
				stats.unitsKilled++;
				if (unit.IsBoss)
					stats.bossesKilled++;
				RunStats.Inc(stats.killsByUnitType, unitType);
				// AccrueForKill уже отработал внутри Unit.Kill до события — дельта накопа и есть дроп с этого юнита
				int crystals = AccruedCrystals;
				int dropped = crystals - lastAccruedCrystals;
				if (dropped > 0) {
					stats.crystalDrops++;
					RunStats.Inc(stats.crystalsByUnitType, unitType, dropped);
				}
				lastAccruedCrystals = crystals;
			}
		}

		private int AccruedXP => gc != null && gc.MatchRewards != null ? gc.MatchRewards.AccruedXP : 0;
		private int AccruedCrystals => gc != null && gc.MatchRewards != null ? gc.MatchRewards.AccruedCrystals : 0;

		private void OnChestOpened(object param) {
			stats.chestsOpened++;
		}

		private void OnMerchantPurchase(object param) {
			stats.merchantPurchases++;
		}

		private void OnTowerBuilt(object param) {
			stats.towersBuilt++;
			if (param is BaseTower tower)
				RunStats.Inc(stats.towersBuiltByType, tower.towerType.ToString());
			placeholdersDirty = true;
			routes.MarkDirty();
		}

		private void OnRelicAdded(object param) {
			if (param is RelicDefinition definition)
				stats.relics.Add(definition.Id);
		}

		private void OnMerchantArrived(MerchantShip ship) {
			pendingMerchant = ship;
			merchantDecided = false;
			stats.merchantVisits++;
		}

		private void OnMerchantDeparted(MerchantShip ship) {
			if (pendingMerchant == ship)
				pendingMerchant = null;
		}

		// ---------------- end ----------------

		public RunStats Finish(string reason, float realDuration) {
			if (!finished) {
				finished = true;
				Unsubscribe();
				taskMetrics.Finish();
			}
			stats.endReason = reason;
			stats.matchDurationGameSec = gameTime;
			stats.matchDurationRealSec = realDuration;
			stats.finalCoins = Coins;
			stats.finalTowersDps = gc != null ? gc.TowersDPS : 0f;
			stats.accruedXP = AccruedXP;
			stats.accruedCrystals = AccruedCrystals;
			return stats;
		}

		public void Log(string message) {
			runner?.Log(message);
		}
	}
}
#endif
