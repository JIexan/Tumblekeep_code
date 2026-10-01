#if AUTOPLAY
using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Cutscenes;
using Gameplay.TasksSystem;
using Gameplay.TasksSystem.Tasks;
using Gameplay.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Autoplay {
	public enum AutoplayMode { Series, InPlace }
	public enum RunnerState { Idle, WaitingScene, SceneReady, Playing, RunEnded, Finished }

	/// <summary>
	/// Сессия автоплея: переживает перезагрузки сцены (DontDestroyOnLoad), на каждую загрузку уровня
	/// подменяет профиль копией снимка, отключает таски, спавнит бота, держит timeScale,
	/// ловит поражение, пишет статистику и перезапускает уровень N раз.
	/// Series — из окна Tools/Autoplay; InPlace — кнопка в читах на уже идущем уровне.
	/// </summary>
	public class AutoplayRunner : MonoBehaviour {
		public static AutoplayRunner Current { get; private set; }

		public AutoplayConfig Config { get; private set; }
		public AutoplayMode Mode { get; private set; }
		public RunnerState State { get; private set; } = RunnerState.Idle;
		public int RunIndex { get; private set; }
		public int Seed { get; private set; }
		public string LastDecision { get; set; } = "";
		public string SessionFolder => writer != null ? writer.SessionFolder : "";

		private static readonly WindowType[] allWindowTypes = (WindowType[])Enum.GetValues(typeof(WindowType));

		private SaveDataProfile snapshot;
		private AutoplayStatsWriter writer;
		private AutoplayBot bot;
		private GameController gc;
		private WindowsManager windows;
		private int sceneReadyHandle = -1;
		private bool loadRequested;
		private int framesSinceReady;
		private float runStartRealtime;
		private GUIStyle overlayStyle;

		// ---------------- entry points ----------------

		public static AutoplayRunner StartSeries(AutoplayConfig cfg) {
			if (Current != null) {
				Debug.LogWarning("[Autoplay] runner already exists");
				return Current;
			}
			cfg.Validate();
			var runner = Create(cfg, AutoplayMode.Series);

			var level = AutoplayEditorBridge.FindLevel(cfg.levelId);
			if (level != null) {
				CurrentLevelContext.Level = level;
				if (string.IsNullOrEmpty(cfg.sceneName))
					cfg.sceneName = level.LoadScene;
			} else {
				Debug.LogWarning($"[Autoplay] level '{cfg.levelId}' not found in LevelSettings: no level context (all towers available, no level upgrades)");
			}
			DisableTasks();
			runner.State = RunnerState.WaitingScene;
			Debug.Log($"[Autoplay] session {runner.writer.SessionFolder}: level={cfg.levelId} scene={cfg.sceneName} runs={cfg.runs} timeScale={cfg.timeScale} seed={runner.Seed}");
			return runner;
		}

		/// <summary>Одиночный прогон на уже загруженном уровне (чит-окно)</summary>
		public static AutoplayRunner StartInPlace(float timeScale) {
			if (Current != null) {
				Debug.LogWarning("[Autoplay] runner already exists");
				return Current;
			}
			var gc = FindObjectOfType<GameController>();
			if (gc == null) {
				Debug.LogError("[Autoplay] no GameController in scene");
				return null;
			}
			var scene = SceneManager.GetActiveScene();
			var cfg = new AutoplayConfig {
				levelId = CurrentLevelContext.Level != null ? CurrentLevelContext.Level.Id : scene.name,
				sceneName = scene.name,
				runs = 1,
				timeScale = timeScale,
				loadScene = false,
				autoRestart = false,
				exitPlayModeWhenDone = false,
				verbose = true,
			};
			cfg.Validate();
			var runner = Create(cfg, AutoplayMode.InPlace);
			runner.snapshot = SavesManager.Instance.CloneProfile(SavesManager.Profile);
			DisableTasks();
			runner.State = RunnerState.WaitingScene;
			Debug.Log($"[Autoplay] in-place run: {runner.writer.SessionFolder} timeScale={timeScale}");
			runner.OnSceneReady(scene);
			return runner;
		}

		private static AutoplayRunner Create(AutoplayConfig cfg, AutoplayMode mode) {
			var go = new GameObject("AutoplayRunner");
			DontDestroyOnLoad(go);
			var runner = go.AddComponent<AutoplayRunner>();
			Current = runner;
			runner.Config = cfg;
			runner.Mode = mode;
			runner.Seed = cfg.seed != 0 ? cfg.seed : UnityEngine.Random.Range(1, int.MaxValue);
			SavesManager.SuppressSaving = true;
			string root = string.IsNullOrEmpty(cfg.outputFolder) ? AutoplayEditorBridge.DefaultOutputRoot : cfg.outputFolder;
			runner.writer = new AutoplayStatsWriter(root, cfg);
			AutoplayEditorBridge.SetLastSession(runner.writer.SessionFolder);
			SceneManager.sceneLoaded += runner.OnSceneLoaded;
			return runner;
		}

		// Пустая коллекция: PrimaryTaskCompleted всегда false — победы и VictoryWindow не будет
		private static void DisableTasks() {
			LevelTasksManager.Instance.SetLevelTasks(new List<AbstractTask>());
		}

		// ---------------- scene lifecycle ----------------

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
			if (State == RunnerState.Finished) {
				// Сессия закончена, а пользователь загрузил другую сцену — раннер больше не нужен
				Destroy(gameObject);
				return;
			}
			OnSceneReady(scene);
		}

		/// <summary>Вызывается после Awake всех объектов сцены (sceneLoaded / AfterSceneLoad), до Start</summary>
		public void OnSceneReady(Scene scene) {
			if (State == RunnerState.Finished || State == RunnerState.Idle)
				return;
			if (scene.handle == sceneReadyHandle)
				return;

			if (Config.loadScene && scene.name != Config.sceneName) {
				if (!loadRequested) {
					loadRequested = true;
					Debug.Log($"[Autoplay] loading scene {Config.sceneName}");
					SceneManager.LoadScene(Config.sceneName);
				}
				return;
			}

			gc = FindObjectOfType<GameController>();
			if (gc == null) {
				Debug.LogError($"[Autoplay] scene '{scene.name}' has no GameController: aborting");
				Finish("NoGameController");
				return;
			}
			sceneReadyHandle = scene.handle;
			loadRequested = false;

			UnityEngine.Random.InitState(Seed + RunIndex);
			InstallProfile();
			DisableTasks();

			windows = gc.uiController != null ? gc.uiController.Windows : null;
			if (windows != null)
				windows.OnBeforeWindowOpen += OnBeforeWindowOpen;

			var stats = new RunStats {
				runIndex = RunIndex,
				seed = Seed + RunIndex,
				levelId = Config.levelId,
				scene = scene.name,
				startedAt = DateTime.Now.ToString("s"),
			};
			var botGo = new GameObject("AutoplayBot");
			bot = botGo.AddComponent<AutoplayBot>();
			bot.Init(this, Config, stats);

			framesSinceReady = 0;
			runStartRealtime = Time.realtimeSinceStartup;
			LastDecision = "";
			State = RunnerState.SceneReady;
		}

		// Профиль прогона = копия снимка + все катсцены/шаги туториала сцены отмечены пройденными.
		// SetProfile шлёт OnProfileLoaded; CutsceneTrigger/TutorialManager подписываются в Start и увидят уже его.
		private void InstallProfile() {
			var sm = SavesManager.Instance;
			if (snapshot == null)
				snapshot = sm.CloneProfile(SavesManager.Profile);
			var runProfile = sm.CloneProfile(snapshot);

			int cutscenes = 0, steps = 0;
			foreach (var cutscene in FindObjectsOfType<Cutscene>(true)) {
				runProfile.tutorial.MarkCutsceneWatched(cutscene.Guid);
				cutscenes++;
			}
			var tutorials = FindObjectsOfType<TutorialManager>(true);
			foreach (var tm in tutorials) {
				foreach (var step in tm.Steps) {
					runProfile.tutorial.CompleteStep(step.Guid);
					steps++;
				}
			}
			sm.SetProfile(runProfile);

			// На идущем уровне туториал мог висеть на шаге с хинтом: перечитать сейв и снять его
			if (Mode == AutoplayMode.InPlace) {
				foreach (var tm in tutorials)
					tm.RestartFromSave();
			}
			Log($"profile installed for run {RunIndex + 1}/{Config.runs} (cutscenes {cutscenes}, tutorial steps {steps})");
		}

		private void Update() {
			if (State != RunnerState.SceneReady)
				return;
			framesSinceReady++;
			// Даём сцене отработать Start (стартовые монеты, OnLevelStarted) и только потом жмём «в бой»
			if (framesSinceReady < 2 || gc == null)
				return;
			if (!gc.IsGameStarted) {
				gc.uiController.OnStartGame();
				Log("match started");
			}
			State = RunnerState.Playing;
		}

		// Окна ставят timeScale=0 и возвращают 1; пока окон нет, держим свой множитель
		private void LateUpdate() {
			if (State != RunnerState.Playing)
				return;
			if (CutsceneController.IsPlaying || AnyWindowActive())
				return;
			if (!Mathf.Approximately(Time.timeScale, Config.timeScale))
				Time.timeScale = Config.timeScale;
		}

		public bool AnyWindowActive() {
			if (windows == null)
				return false;
			foreach (var type in allWindowTypes) {
				if (windows.IsWindowActive(type))
					return true;
			}
			return false;
		}

		private void OnBeforeWindowOpen(BaseWindow window) {
			if (State != RunnerState.Playing && State != RunnerState.SceneReady)
				return;
			if (window is DefeatWindow) {
				EndRun("CastleDestroyed");
			} else if (window is VictoryWindow) {
				// Окно победы финальное (продолжить катку нельзя) — прогон завершаем
				Debug.LogError("[Autoplay] VictoryWindow opened: level tasks were not disabled?");
				EndRun("Victory");
			}
		}

		// ---------------- run end ----------------

		/// <summary>Досрочное завершение прогона ботом (лимит волн/времени)</summary>
		public void RequestEndRun(string reason) {
			EndRun(reason);
		}

		// «вид:значение/цель» по всем видам доп. тасков — цель та, что кривая даёт на текущей прокачке острова
		private static string FormatTaskMetrics(RunStats stats) {
			var parts = new List<string>();
			foreach (var pair in stats.taskMetrics) {
				stats.taskTargets.TryGetValue(pair.Key, out int target);
				parts.Add($"{pair.Key}:{pair.Value}/{target}");
			}
			return string.Join(" ", parts);
		}

		private void EndRun(string reason) {
			if (State == RunnerState.RunEnded || State == RunnerState.Finished)
				return;
			State = RunnerState.RunEnded;

			RunStats stats = bot != null
				? bot.Finish(reason, Time.realtimeSinceStartup - runStartRealtime)
				: new RunStats { runIndex = RunIndex, endReason = reason };
			writer.AppendRun(stats);
			Debug.Log($"[Autoplay] run {RunIndex + 1}/{Config.runs} ended ({reason}): wave={stats.maxWaveReached}{(stats.reachedInfinite ? "(inf)" : "")} " +
				$"gold={stats.goldCollected}/{stats.goldSpent} kills={stats.unitsKilled} bosses={stats.bossesKilled} leaks={stats.leaks} " +
				$"chests={stats.chestsOpened} merchant={stats.merchantPurchases}/{stats.merchantVisits} towers={stats.towersBuilt} " +
				$"upgrades={stats.upgradesApplied} spells={stats.spellsCast} time={stats.matchDurationGameSec:F0}s ({stats.matchDurationRealSec:F0}s real) " +
				$"progress={stats.islandProgressPercent:F0}% tasks=[{FormatTaskMetrics(stats)}]");

			if (windows != null)
				windows.OnBeforeWindowOpen -= OnBeforeWindowOpen;
			windows = null;
			RunIndex++;

			if (RunIndex < Config.runs && Config.autoRestart)
				StartCoroutine(RestartNextFrame());
			else
				Finish(reason);
		}

		private IEnumerator RestartNextFrame() {
			yield return null;
			Time.timeScale = 1f;
			State = RunnerState.WaitingScene;
			gc.OnRestart();
			// ResetLevel внутри OnRestart чистит таски: ставим пустые до OnLevelStarted новой сцены
			DisableTasks();
		}

		private void Finish(string reason) {
			if (State == RunnerState.Finished)
				return;
			State = RunnerState.Finished;
			writer.WriteSummary();
			Debug.Log($"[Autoplay] session finished ({reason}): {RunIndex} run(s), summary at {writer.SessionFolder}");
			if (Mode == AutoplayMode.Series && Config.exitPlayModeWhenDone) {
				Time.timeScale = 1f;
				AutoplayEditorBridge.ExitPlayMode();
			}
			// Иначе раннер живёт до следующей загрузки сцены: SuppressSaving остаётся, чтобы
			// изменённая копия профиля не ушла на диск из DefeatWindow
		}

		private void OnDestroy() {
			SceneManager.sceneLoaded -= OnSceneLoaded;
			if (windows != null)
				windows.OnBeforeWindowOpen -= OnBeforeWindowOpen;
			SavesManager.SuppressSaving = false;
			Time.timeScale = 1f;
			if (Current == this)
				Current = null;
		}

		public void Log(string message) {
			LastDecision = message;
			if (Config != null && Config.verbose)
				Debug.Log("[Autoplay] " + message);
		}

		// ---------------- overlay ----------------

		private void OnGUI() {
			if (State == RunnerState.Idle || State == RunnerState.Finished)
				return;
			if (overlayStyle == null) {
				overlayStyle = new GUIStyle(GUI.skin.box) {
					alignment = TextAnchor.UpperLeft,
					fontSize = 12,
					richText = false,
				};
				overlayStyle.normal.textColor = Color.white;
			}
			var stats = bot != null ? bot.Stats : null;
			string text = $"AUTOPLAY {Mode} run {RunIndex + 1}/{Config.runs} x{Config.timeScale:0.#} [{State}]";
			if (stats != null) {
				text += $"\nwave {stats.maxWaveReached}{(stats.reachedInfinite ? " (inf)" : "")}  coins {bot.Coins}  towers {stats.towersBuilt}  upg {stats.upgradesApplied}  " +
					$"chests {stats.chestsOpened}  spells {stats.spellsCast}  kills {stats.unitsKilled}  leaks {stats.leaks}  t={bot.GameTime:F0}s";
			}
			text += "\n" + LastDecision;
			GUI.Box(new Rect(10, 10, 640, 64), text, overlayStyle);
		}
	}
}
#endif
