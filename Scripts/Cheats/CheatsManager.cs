#if CHEATS
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cheats {
	public class CheatsManager : MonoBehaviour {
		private Canvas mainCanvas; // ������ �� �������� Canvas ����
		private CheatWindow cheatWindow;
		private CanvasGroup uiGroup;
		private bool uiHidden;
		private float savedUiAlpha;
		private bool savedUiInteractable;
		private bool savedUiBlocksRaycasts;

		private void Init(List<CheatWindow.CheatItem> cheatList) {
			cheatList.Insert(0, new CheatWindow.CheatItem("Toggle UI", ToggleUI));
			cheatList.Insert(1, new CheatWindow.CheatItem("Toggle camera limits", ToggleCameraLimits));
			foreach (var path in FindObjectsOfType<Gameplay.Marketing.CinematicCameraPath>()) {
				var route = path;
				cheatList.Insert(2, new CheatWindow.CheatItem($"Camera: {route.name}", () => {
					if (route.Play())
						cheatWindow.Hide();
				}));
			}
			cheatList.Insert(2, new CheatWindow.CheatItem("Stop cinematic camera", Gameplay.Marketing.CinematicCameraPath.StopActive));
			mainCanvas = GetComponentInParent<Canvas>();
			// ������� ��������� �������� ����
			cheatWindow = gameObject.AddComponent<CheatWindow>();

			// �������������� ���� ����� � �������� Canvas
			cheatWindow.Initialize(mainCanvas, cheatList, "Game Debug Console");

			// ����������� ��������� (�����������)
			cheatWindow.SetGridColumns(3);
			cheatWindow.SetButtonSize(new Vector2(180, 60));

			// �������� ���� ��� ������
			cheatWindow.Hide();
		}

		private void ToggleCameraLimits() {
			var camera = CameraController.Instance;
			if (camera == null)
				return;

			camera.ToggleLimits();
			Debug.Log($"[Cheats] Camera limits: {(camera.LimitsDisabled ? "off" : "on")}.");
		}

		private void ToggleUI() {
			if (uiHidden) {
				RestoreUI();
				return;
			}
			if (mainCanvas == null)
				return;

			uiGroup = mainCanvas.GetComponent<CanvasGroup>();
			if (uiGroup == null)
				uiGroup = mainCanvas.gameObject.AddComponent<CanvasGroup>();

			savedUiAlpha = uiGroup.alpha;
			savedUiInteractable = uiGroup.interactable;
			savedUiBlocksRaycasts = uiGroup.blocksRaycasts;
			uiHidden = true;
			uiGroup.alpha = 0f;
			uiGroup.interactable = false;
			uiGroup.blocksRaycasts = false;
		}

		private void RestoreUI() {
			if (!uiHidden)
				return;

			if (uiGroup != null) {
				uiGroup.alpha = savedUiAlpha;
				uiGroup.interactable = savedUiInteractable;
				uiGroup.blocksRaycasts = savedUiBlocksRaycasts;
			}
			uiHidden = false;
		}

		private void OnDisable() {
			RestoreUI();
		}

		void Update() {
			// ������� ������� ��� ���������� �����
			if (Input.GetKeyDown(KeyCode.BackQuote)) // ������� `
			{
				cheatWindow.Toggle();
			}

			if (Input.GetKeyDown(KeyCode.F1)) {
				cheatWindow.Show();
			}

			if (Input.GetKeyDown(KeyCode.Escape)) {
				cheatWindow.Hide();
			}
		}

		public void GenerateGameCheats() {
			// ������� ������ �����
			var cheatList = new List<CheatWindow.CheatItem>
			{
				new CheatWindow.CheatItem("Add 100 Gold", () => AddGold(100)),
				new CheatWindow.CheatItem("Complete primary task", CompletePrimaryTask),
				new CheatWindow.CheatItem("Complete all tasks", CompleteAllTasks),
				new CheatWindow.CheatItem("Defeat", Defeat),
				new CheatWindow.CheatItem("Toggle immortal castle", ToggleImmortalCastle),
				new CheatWindow.CheatItem("Toggle endless parade", ToggleEndlessParade),
				new CheatWindow.CheatItem("All towers: +10000 XP", AddExperienceToAllTowers),
				new CheatWindow.CheatItem("Add 100 Crystals", () => AddCrystals(100)),
				new CheatWindow.CheatItem("Log level upgrades", LogLevelUpgrades),
				new CheatWindow.CheatItem("Buy next upgrade", BuyNextUpgrade),
				new CheatWindow.CheatItem("Force-buy all upgrades", ForceBuyAllUpgrades),
				//new CheatWindow.CheatItem("Add 100 island XP", () => AddIslandXP(100)),
				new CheatWindow.CheatItem("Add 100 island XP", () => AddLevelXpAccured(100)),
				new CheatWindow.CheatItem("Log island level", LogIslandLevel),
				new CheatWindow.CheatItem("Log island progress + task pool", LogIslandProgressAndTaskPool),
				new CheatWindow.CheatItem("Reroll level tasks", RerollLevelTasks),
				new CheatWindow.CheatItem("Add random relic", AddRandomRelic),
				new CheatWindow.CheatItem("Add next relic", AddNextRelic),
				new CheatWindow.CheatItem("Log relics", LogRelics),
				new CheatWindow.CheatItem("Reset relics", ResetRelics),
				new CheatWindow.CheatItem("Add 3 rerolls", AddRerolls),
				new CheatWindow.CheatItem("Drop Bronze chest", () => DropChest(ChestTier.Bronze)),
				new CheatWindow.CheatItem("Drop Iron chest", () => DropChest(ChestTier.Iron)),
				new CheatWindow.CheatItem("Drop Gold chest", () => DropChest(ChestTier.Gold)),
				new CheatWindow.CheatItem("Reset tutorial+cutscenes", ResetTutorialProgress),
				new CheatWindow.CheatItem("Book: discover all units", BookDiscovery.DiscoverAllUnits),
				new CheatWindow.CheatItem("Book: reset", BookDiscovery.ResetBook),
				new CheatWindow.CheatItem("Unlocks: unlock all", UnlockAllContent),
				new CheatWindow.CheatItem("Unlocks: reset", ResetContentUnlocks),
				new CheatWindow.CheatItem("Unlocks: log", LogContentUnlocks),
#if AUTOPLAY
				new CheatWindow.CheatItem("Autoplay x1", () => Autoplay.AutoplayRunner.StartInPlace(1f)),
				new CheatWindow.CheatItem("Autoplay x5", () => Autoplay.AutoplayRunner.StartInPlace(5f)),
#endif
			};
			AddSpellCheats(cheatList);

			Init(cheatList);
		}

		public void GenerateMetaCheats() {
			// ������� ������ �����
			var cheatList = new List<CheatWindow.CheatItem>
			{
				new CheatWindow.CheatItem("Add 1 crystal", () => AddCrystals(1)),
				new CheatWindow.CheatItem("Add 50 crystals", () => AddCrystals(50)),
				new CheatWindow.CheatItem("Hub: complete next zone", () => CompleteHubZones(false).Forget()),
				new CheatWindow.CheatItem("Hub: complete all zones", () => CompleteHubZones(true).Forget()),
				new CheatWindow.CheatItem("Hub: unlock all zones", () => UnlockAllHubZones().Forget()),
				new CheatWindow.CheatItem("Islands: +1 level (all)", AddIslandLevelToAll),
				new CheatWindow.CheatItem("Play scene cutscene", PlaySceneCutscene),
				new CheatWindow.CheatItem("Reset tutorial+cutscenes", ResetTutorialProgress),
				new CheatWindow.CheatItem("Book: discover all units", BookDiscovery.DiscoverAllUnits),
				new CheatWindow.CheatItem("Book: reset", BookDiscovery.ResetBook),
				new CheatWindow.CheatItem("Unlocks: unlock all", UnlockAllContent),
				new CheatWindow.CheatItem("Unlocks: reset", ResetContentUnlocks),
				new CheatWindow.CheatItem("Unlocks: log", LogContentUnlocks),
			};
			AddSpellCheats(cheatList);

			Init(cheatList);
		}

		// По кнопке на каждый спелл из ItemType (значения от 50 — см. enum в SaveDataProfile).
		// Список берётся из enum, чтобы новые спеллы попадали в читы сами
		private void AddSpellCheats(List<CheatWindow.CheatItem> cheatList) {
			foreach (ItemType type in Enum.GetValues(typeof(ItemType))) {
				if ((int)type < 50)
					continue;
				ItemType captured = type;
				string name = type.ToString().Replace("Spell", "");
				cheatList.Add(new CheatWindow.CheatItem($"+1 scroll: {name}", () => AddScroll(captured, 1)));
			}
		}

		private void AddScroll(ItemType type, int count) {
			SavesManager.Profile.AddItem(type, count);
		}

		// ������� ������� �����
		private void AddGold(int amount) {
			CoinsManager.Instance.Add(amount);
		}

		private void AddExperienceToAllTowers() {
			var game = GameController.Instance;
			if (game == null)
				return;
			int count = 0;
			foreach (var tower in game.GetAllTowers()) {
				if (tower == null)
					continue;
				tower.CheatAddXP(10000);
				count++;
			}
			Debug.Log($"[Cheats] +10000 XP выдано башням: {count}. Выбери улучшения в меню каждой башни.");
		}

		private void CompletePrimaryTask() {
			var tasks = Gameplay.TasksSystem.LevelTasksManager.Instance.GetCurrentTasksList();
			// Пустой список — автоплей (таски отключены)
			if (tasks == null || tasks.Count == 0)
				return;
			if (!tasks[0].IsCompleted)
				tasks[0].Complete();
		}

		private void CompleteAllTasks() {
			var tasks = Gameplay.TasksSystem.LevelTasksManager.Instance.GetCurrentTasksList();
			if (tasks == null)
				return;
			foreach (var task in tasks) {
				if (!task.IsCompleted) {
					task.Complete();
				}
			}
		}

		// Прокачка острова, доступный пул доп. тасков и их цели на текущем проценте — для балансировки кривых
		private void LogIslandProgressAndTaskPool() {
			string levelId = Gameplay.LevelUpgrades.LevelUpgradesManager.CurrentLevelId;
			float progress = Gameplay.LevelUpgrades.LevelUpgradesManager.GetIslandProgress01(levelId);
			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"[Cheats] Island '{levelId}' progress {progress * 100f:0.#}%, purchased: {string.Join(", ", Gameplay.LevelUpgrades.LevelUpgradesManager.GetPurchasedUpgradeIds(levelId))}");
			foreach (var definition in Gameplay.TasksSystem.LevelTasksGenerator.GetAvailableDefinitions(levelId))
				sb.AppendLine($"  {definition.Type} [{definition.Group}] weight {definition.Weight:0.##} target ~{definition.GetMedianTarget(progress)}");

			var tasks = Gameplay.TasksSystem.LevelTasksManager.Instance.GetCurrentTasksList();
			if (tasks != null) {
				sb.AppendLine("  current tasks:");
				foreach (var task in tasks)
					sb.AppendLine($"    {task.Type}: {task.CurrentValue}/{task.TargetValue}{(task.IsCompleted ? " (done)" : "")}");
			}
			Debug.Log(sb.ToString());
		}

		private void RerollLevelTasks() {
			var tasks = Gameplay.TasksSystem.LevelTasksGenerator.Generate(CurrentLevelContext.Level);
			Gameplay.TasksSystem.LevelTasksManager.Instance.SetLevelTasks(tasks);
			foreach (var panel in FindObjectsOfType<TasksPanel>())
				panel.Init(tasks);
			LogIslandProgressAndTaskPool();
		}

		private void Defeat() {
			Castle.Instance.Damage(LevelDesriptor.Instance.castleHP);
		}

		private void AddCrystals(int amount) {
			SavesManager.Profile.AddItem(ItemType.Crystal, amount);
		}

		// ---- Читы для просмотра волн ----

		private void ToggleImmortalCastle() {
			if (Castle.Instance == null)
				return;
			Castle.Instance.Invulnerable = !Castle.Instance.Invulnerable;
			Debug.Log($"[Cheats] Бессмертный замок: {(Castle.Instance.Invulnerable ? "ВКЛ" : "ВЫКЛ")}.");
		}

		private const int ParadeUnitsPerType = 3;
		private Wave paradeWave;

		// Бесконечный парад: все типы юнитов по кругу через настоящий пайплайн (корабль → WaveSpawner) — смотреть
		// на дистанцию спавна. Раскладка как у генератора: медленные вперёд, maxParallelGroups групп вперемешку,
		// зазор из WavesGeneratorSettings. Юниты вне учёта волн и без наград, чтобы не накручивать катку и профиль.
		// Повторное нажатие отзывает корабль, обычные волны продолжаются сами
		private void ToggleEndlessParade() {
			if (paradeWave != null) {
				// Корабль мог ещё не доплыть: без групп и без loop он по прибытии сразу уплывёт
				paradeWave.loop = false;
				paradeWave.groups = new List<SpawnGroup>();
				foreach (var spawner in FindObjectsOfType<WaveSpawner>()) {
					if (spawner.WaveId == GameController.UntrackedWaveId)
						spawner.FinishNow();
				}
				paradeWave = null;
				Debug.Log("[Cheats] Парад остановлен: корабль уплывает, обычные волны продолжатся.");
				return;
			}

			var prefabs = GameController.Instance.unitsToSpawn.unitsToSpawn;
			var types = new List<UnitType>();
			foreach (var pair in prefabs) {
				if (pair.Value != null)
					types.Add(pair.Key);
			}
			if (types.Count == 0) {
				Debug.Log("[Cheats] В GameController.unitsToSpawn нет префабов юнитов.");
				return;
			}
			types.Sort((a, b) => ((float)prefabs[a].speed).CompareTo((float)prefabs[b].speed));

			var generator = FindObjectOfType<WavesGenerator>();
			var settings = generator != null ? generator.Settings : ScriptableObject.CreateInstance<WavesGeneratorSettings>();
			int lanes = Mathf.Max(settings.maxParallelGroups, 1);
			int laneCount = Mathf.Min(lanes, types.Count);
			Vector2 gap = settings.spawnGapFactor;

			var wave = new Wave { id = GameController.UntrackedWaveId, groups = new List<SpawnGroup>(), loop = true };
			for (int i = 0; i < types.Count; i++) {
				wave.groups.Add(new SpawnGroup {
					type = types[i],
					spawnPointIdx = 0,
					routeIdx = 0,
					minGapFactor = gap.x,
					maxGapFactor = gap.y,
					laneOffset = settings.GetLaneOffset(i % lanes, laneCount),
					waitingList = i < lanes ? new int[0] : new[] { i - lanes },
					unitsCount = ParadeUnitsPerType,
					unitsHPMultiplier = 1f,
					rewardMultiplier = 0f
				});
			}

			if (!GameController.Instance.StartCheatWave(wave)) {
				Debug.Log("[Cheats] На точке спавна ещё стоит корабль обычной волны — дождись отплытия и нажми снова.");
				return;
			}
			paradeWave = wave;
			Debug.Log($"[Cheats] Парад запущен: {types.Count} типов по {ParadeUnitsPerType} шт. по кругу. Повторное нажатие — остановить.");
		}
		// ---- Читы зон хаба (логика — в PlayerProgressController под CHEATS) ----

		// Восстанавливает зоны целиком, без оплаты и анимаций: all == false — только следующую
		// (первую зону в работе, иначе первую закрытую). Анлоки контента из шагов попадают в профиль
		private async UniTaskVoid CompleteHubZones(bool all) {
			var hub = GetReadyHub();
			if (hub == null)
				return;
			int completed = await hub.CheatCompleteRegions(all);
			Debug.Log(completed > 0
				? $"[Cheats] Восстановлено зон: {completed}. {DescribeHubZones(hub)}"
				: "[Cheats] Все зоны хаба уже восстановлены.");
		}

		// Только открывает закрытые зоны (Destroyed → InProgress) — шаги остаются игроку
		private async UniTaskVoid UnlockAllHubZones() {
			var hub = GetReadyHub();
			if (hub == null)
				return;
			int unlocked = await hub.CheatUnlockAllRegions();
			Debug.Log($"[Cheats] Открыто зон: {unlocked}. {DescribeHubZones(hub)}");
		}

		private PlayerProgressController GetReadyHub() {
			var hub = PlayerProgressController.Instance;
			if (hub == null) {
				Debug.Log("[Cheats] PlayerProgressController не найден — чит работает только в хабе.");
				return null;
			}
			if (!hub.IsInitialized) {
				Debug.Log("[Cheats] Хаб ещё грузит зоны (или предыдущий чит зон не закончил) — нажми позже.");
				return null;
			}
			return hub;
		}

		private string DescribeHubZones(PlayerProgressController hub) {
			var parts = new List<string>();
			foreach (var region in hub.Regions)
				parts.Add($"{region.Id}={region.State}");
			return string.Join(", ", parts);
		}

		// ---- Читы катсцен/туториала ----

		// Запускает первую найденную катсцену сцены (флаг «просмотрено» не проверяется)
		private void PlaySceneCutscene() {
			var cutscene = FindObjectOfType<Gameplay.Cutscenes.Cutscene>(true);
			if (cutscene == null) {
				Debug.Log("[Cheats] Катсцена в сцене не найдена.");
				return;
			}
			Gameplay.Cutscenes.CutsceneController.Instance.Play(cutscene);
			Debug.Log($"[Cheats] Катсцена '{cutscene.Id}' запущена.");
		}

		// Сбрасывает прогресс туториала и просмотренность катсцен, перезапускает туториал сцены
		private void ResetTutorialProgress() {
			var tutorial = SavesManager.Profile.tutorial;
			tutorial.watchedCutsceneIds.Clear();
			tutorial.completedTutorialStepIds.Clear();
			tutorial.battleVisited = false;
			SavesManager.Instance.SaveProfile();

			Gameplay.Tutorial.TutorialManager manager = null;
			foreach (var m in FindObjectsOfType<Gameplay.Tutorial.TutorialManager>()) {
				manager = m;
				m.RestartFromSave();
			}
			Debug.Log("[Cheats] Прогресс туториала/катсцен сброшен" + (manager != null ? ", туториал перезапущен." : "."));
		}

		// ---- Читы системы апгрейдов уровня (эффекты применяются при загрузке уровня —
		// после force-buy нужен рестарт уровня) ----

		private void LogLevelUpgrades() {
			string levelId = Gameplay.LevelUpgrades.LevelUpgradesManager.CurrentLevelId;
			var config = Gameplay.LevelUpgrades.LevelUpgradesManager.GetConfig(levelId);
			if (config == null) {
				Debug.Log($"[Cheats] Нет конфига апгрейдов для уровня '{levelId}'.");
				return;
			}

			foreach (var upgrade in config.Upgrades) {
				if (upgrade == null)
					continue;
				bool purchased = Gameplay.LevelUpgrades.LevelUpgradesManager.IsPurchased(levelId, upgrade.Id);
				bool canPurchase = Gameplay.LevelUpgrades.LevelUpgradesManager.CanPurchase(levelId, upgrade.Id);
				string requirements = "";
				foreach (var requirement in upgrade.Requirements) {
					if (requirement == null)
						continue;
					var (current, required) = requirement.GetProgress(levelId);
					requirements += $"\n  - {requirement.GetType().Name}: {current}/{required} (met={requirement.IsMet(levelId)})";
				}
				Debug.Log($"[Cheats] '{upgrade.Id}': purchased={purchased}, canPurchase={canPurchase}{requirements}");
			}
		}

		private void BuyNextUpgrade() {
			string levelId = Gameplay.LevelUpgrades.LevelUpgradesManager.CurrentLevelId;
			var config = Gameplay.LevelUpgrades.LevelUpgradesManager.GetConfig(levelId);
			if (config == null) {
				Debug.Log($"[Cheats] Нет конфига апгрейдов для уровня '{levelId}'.");
				return;
			}

			foreach (var upgrade in config.Upgrades) {
				if (upgrade == null || !Gameplay.LevelUpgrades.LevelUpgradesManager.CanPurchase(levelId, upgrade.Id))
					continue;
				bool result = Gameplay.LevelUpgrades.LevelUpgradesManager.TryPurchase(levelId, upgrade.Id);
				Debug.Log($"[Cheats] TryPurchase('{upgrade.Id}') = {result}");
				return;
			}
			Debug.Log("[Cheats] Нет доступных для покупки апгрейдов.");
		}

		private void ForceBuyAllUpgrades() {
			string levelId = Gameplay.LevelUpgrades.LevelUpgradesManager.CurrentLevelId;
			var config = Gameplay.LevelUpgrades.LevelUpgradesManager.GetConfig(levelId);
			if (config == null) {
				Debug.Log($"[Cheats] Нет конфига апгрейдов для уровня '{levelId}'.");
				return;
			}

			foreach (var upgrade in config.Upgrades) {
				if (upgrade != null)
					SavesManager.Profile.levelUpgrades.MarkPurchased(levelId, upgrade.Id);
			}
			SavesManager.Instance.SaveProfile();
			Debug.Log($"[Cheats] Все апгрейды уровня '{levelId}' помечены купленными (в обход требований). Перезапустите уровень.");
		}

		private void AddIslandXP(int amount) {
			string levelId = Gameplay.IslandExperience.IslandExperienceManager.CurrentLevelId;
			if (string.IsNullOrEmpty(levelId)) {
				Debug.Log("[Cheats] CurrentLevelContext.Level не установлен — остров неизвестен.");
				return;
			}

			Gameplay.IslandExperience.IslandExperienceManager.AddXP(levelId, amount);
			LogIslandLevel();
		}

		// Хаб: +1 левел каждому острову из LevelSettings карты. Начисляется ровно цена следующего левелапа,
		// так что набранный внутри левела XP сохраняется. «Home» (без сцены) — не остров, пропускается
		private void AddIslandLevelToAll() {
			var map = GlobalMapController.Instance;
			if (map == null || map.SettingsLevel == null) {
				Debug.Log("[Cheats] GlobalMapController/LevelSettings не найден — чит работает только в хабе.");
				return;
			}

			var sb = new System.Text.StringBuilder("[Cheats] +1 левел островам:");
			foreach (var level in map.SettingsLevel.Levels) {
				if (level == null || string.IsNullOrEmpty(level.LoadScene))
					continue;
				int totalXP = Gameplay.IslandExperience.IslandExperienceManager.GetTotalXP(level.Id);
				int amount = Gameplay.IslandExperience.IslandExperienceManager.GetXPToNextLevel(totalXP);
				if (amount <= 0) {
					sb.Append($"\n  '{level.Id}': уже максимальный левел {Gameplay.IslandExperience.IslandExperienceManager.GetLevel(level.Id)}");
					continue;
				}
				Gameplay.IslandExperience.IslandExperienceManager.AddXP(level.Id, amount);
				sb.Append($"\n  '{level.Id}': +{amount} XP → левел {Gameplay.IslandExperience.IslandExperienceManager.GetLevel(level.Id)} (totalXP={totalXP + amount})");
			}
			Debug.Log(sb.ToString());
		}

		private void AddLevelXpAccured(int amount) {
			GameController.Instance.MatchRewards.AccrueForKill(new ItemUnitPrice() { exp = amount });
		}

		private void LogIslandLevel() {
			string levelId = Gameplay.IslandExperience.IslandExperienceManager.CurrentLevelId;
			if (string.IsNullOrEmpty(levelId)) {
				Debug.Log("[Cheats] CurrentLevelContext.Level не установлен — остров неизвестен.");
				return;
			}

			int totalXP = Gameplay.IslandExperience.IslandExperienceManager.GetTotalXP(levelId);
			int level = Gameplay.IslandExperience.IslandExperienceManager.GetLevel(levelId);
			float progress = Gameplay.IslandExperience.IslandExperienceManager.GetProgressToNextLevel(levelId);
			bool isMax = Gameplay.IslandExperience.IslandExperienceManager.IsMaxLevel(levelId);
			Debug.Log($"[Cheats] Остров '{levelId}': totalXP={totalXP}, level={level}, progress={progress:P0}, isMax={isMax}");
		}

		// ---- Читы реликвий (внутрикаточные пассивки) ----

		private static int nextRelicIndex;

		private void AddRandomRelic() {
			var catalog = Gameplay.Relics.RelicsManager.Catalog;
			if (catalog == null) {
				Debug.Log("[Cheats] RelicsSettings не подключён к GameConfigs.");
				return;
			}
			// Мимо таблиц сундуков; нестакаемые во владении и упершиеся в лимит пропускаются
			var eligible = new List<Gameplay.Relics.RelicDefinition>();
			foreach (var relic in catalog.Relics) {
				if (Gameplay.Relics.RelicsManager.CanDrop(relic))
					eligible.Add(relic);
			}
			if (eligible.Count == 0) {
				Debug.Log("[Cheats] Все реликвии каталога уже на максимуме.");
				return;
			}
			var picked = eligible[UnityEngine.Random.Range(0, eligible.Count)];
			Gameplay.Relics.RelicsManager.Add(picked);
			Debug.Log($"[Cheats] Реликвия добавлена: '{picked.Id}' ({picked.Rarity}), стаков: {Gameplay.Relics.RelicsManager.GetOwnedStacks(picked)}.");
		}

		private void AddNextRelic() {
			var catalog = Gameplay.Relics.RelicsManager.Catalog;
			if (catalog == null || catalog.Relics.Count == 0) {
				Debug.Log("[Cheats] RelicsSettings не подключён к GameConfigs или каталог пуст.");
				return;
			}
			var relic = catalog.Relics[nextRelicIndex % catalog.Relics.Count];
			nextRelicIndex++;
			if (relic == null)
				return;
			Gameplay.Relics.RelicsManager.Add(relic);
			Debug.Log($"[Cheats] Реликвия добавлена: '{relic.Id}' ({relic.Rarity}), стаков: {Gameplay.Relics.RelicsManager.GetOwnedStacks(relic)}.");
		}

		private void LogRelics() {
			// Особенность острова живёт вне Owned; удача — чтобы видеть её вклад («Звонкая казна»: +1 за 10 монет)
			var feature = Gameplay.IslandFeatures.IslandFeaturesManager.Current;
			float luck = Castle.Instance != null ? Castle.Instance.GetCharacteristicValue(CharacteristicType.Luck) : 0f;
			Debug.Log($"[Cheats] Особенность острова: {(feature != null ? $"'{feature.Id}'" : "нет")}, удача замка: {luck:0.#}, монет: {CoinsManager.Instance.Amount}");

			var owned = Gameplay.Relics.RelicsManager.Owned;
			if (owned.Count == 0) {
				Debug.Log("[Cheats] Реликвий нет.");
				return;
			}
			string log = $"[Cheats] Реликвий: {owned.Count}, волна: {Gameplay.Relics.RelicsManager.CurrentWave}";
			foreach (var relic in owned)
				log += $"\n  - '{relic.Definition.Id}' ({relic.Definition.Rarity}) x{relic.Stacks}";
			Debug.Log(log);
		}

		private void ResetRelics() {
			Gameplay.Relics.RelicsManager.ResetRun();
			Debug.Log("[Cheats] Реликвии катки сброшены.");
		}

		private void AddRerolls() {
			Gameplay.Rerolls.RerollsManager.AddFree(3);
			Debug.Log($"[Cheats] +3 бесплатных реролла, в пуле {Gameplay.Rerolls.RerollsManager.FreeLeft}, платных было {Gameplay.Rerolls.RerollsManager.PaidCount}.");
		}

		// ---- Читы мета-анлоков (ContentUnlocksManager) ----

		// Флаг unlockAll в профиле: списки анлоков не трогает, «Unlocks: reset» возвращает честное состояние
		private void UnlockAllContent() {
			Gameplay.Unlocks.ContentUnlocksManager.SetUnlockAll(true);
			SavesManager.Instance.SaveProfile();
			Debug.Log("[Cheats] Весь контент открыт (unlockAll). " + Gameplay.Unlocks.ContentUnlocksManager.DescribeState());
		}

		private void ResetContentUnlocks() {
			Gameplay.Unlocks.ContentUnlocksManager.ResetAll();
			SavesManager.Instance.SaveProfile();
			Debug.Log("[Cheats] Мета-анлоки сброшены к дефолтам. " + Gameplay.Unlocks.ContentUnlocksManager.DescribeState());
		}

		private void LogContentUnlocks() {
			Debug.Log(Gameplay.Unlocks.ContentUnlocksManager.DescribeState());
		}

		// ---- Читы сундуков ----

		// Роняет сундук нужного тира в точку, куда смотрит центр экрана (плоскость земли y=0).
		private void DropChest(ChestTier tier) {
			var chests = GameController.Instance != null ? GameController.Instance.Chests : null;
			if (chests == null) {
				Debug.Log("[Cheats] ChestsManager не найден на GameController.");
				return;
			}
			Vector3 position = Vector3.zero;
			var camera = Camera.main;
			if (camera != null) {
				var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
				if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance))
					position = ray.GetPoint(distance);
			}
			chests.DropChest(position, Vector3.forward, tier);
			Debug.Log($"[Cheats] Сундук {tier} выпал в {position}.");
		}
	}
}
#endif
