#if AUTOPLAY
using System.Collections.Generic;
using Gameplay.LevelUpgrades;
using Gameplay.TasksSystem;
using Gameplay.TasksSystem.Probes;
using UnityEngine;

namespace Autoplay {
	/// <summary>
	/// Меряет ВСЕ виды доп. тасков уровня за прогон, независимо от того, открыты ли они покупками:
	/// сами таски в автоплее отключены (пустая коллекция — иначе победа), а пробы работают без целей.
	/// Рядом пишется прокачка острова и цель, которую генератор выдал бы на этом проценте, —
	/// по этим данным правятся кривые в LevelTasksSettings.
	/// </summary>
	public class TaskMetricsRecorder {
		private readonly RunStats stats;
		private readonly List<KeyValuePair<string, TaskProbe>> probes = new List<KeyValuePair<string, TaskProbe>>();
		private bool started;

		public TaskMetricsRecorder(RunStats stats) {
			this.stats = stats;

			string levelId = LevelUpgradesManager.CurrentLevelId;
			float progress = LevelUpgradesManager.GetIslandProgress01(levelId);
			stats.islandProgressPercent = progress * 100f;
			stats.purchasedUpgrades = LevelUpgradesManager.GetPurchasedUpgradeIds(levelId);
			foreach (var definition in LevelTasksGenerator.GetAvailableDefinitions(levelId))
				stats.availableTaskTypes.Add(definition.Type.ToString());

			var settings = GameConfigs.Instance.LevelTasksSettings;
			if (settings == null) {
				Debug.LogWarning("[Autoplay] LevelTasksSettings не назначен в GameConfigs — метрики тасков не пишутся");
				return;
			}

			foreach (var definition in settings.Definitions) {
				if (definition == null)
					continue;
				var probe = LevelTasksFactory.CreateProbe(definition.Type, definition.Param);
				if (probe == null)
					continue;
				string key = definition.Type.ToString();
				probes.Add(new KeyValuePair<string, TaskProbe>(key, probe));
				stats.taskTargets[key] = definition.GetMedianTarget(progress);
				stats.taskCompletedAtWave[key] = -1;
			}
		}

		// С OnLevelStarted, как настоящие таски: стартовые монеты в CollectGold не идут
		public void Start() {
			if (started)
				return;
			started = true;
			foreach (var pair in probes)
				pair.Value.Start();
		}

		public void Tick(float now, int currentWave) {
			if (!started)
				return;
			foreach (var pair in probes) {
				pair.Value.Tick(now);
				if (stats.taskCompletedAtWave[pair.Key] < 0 && pair.Value.Value >= stats.taskTargets[pair.Key])
					stats.taskCompletedAtWave[pair.Key] = currentWave;
			}
		}

		public Dictionary<string, int> Snapshot() {
			var res = new Dictionary<string, int>(probes.Count);
			foreach (var pair in probes)
				res[pair.Key] = pair.Value.Value;
			return res;
		}

		public void Finish() {
			stats.taskMetrics = Snapshot();
			foreach (var pair in probes)
				pair.Value.Stop();
			started = false;
		}
	}
}
#endif
