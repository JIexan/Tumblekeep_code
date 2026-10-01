using Gameplay.LevelUpgrades;
using Gameplay.TasksSystem.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.TasksSystem {
	// Таски катки: [0] — основной (условие победы, кнопка «Уплыть»), дальше — доп. таски (множитель наград).
	// Доп. таски давят на направление билда башен: пул открывают купленные апгрейды уровня,
	// цель растёт с прокачкой острова по кривым из LevelTasksSettings.
	public static class LevelTasksGenerator {

		private static LevelTasksFactory tasksFactory = new LevelTasksFactory();

		// level == null — прямой запуск боевой сцены в редакторе: прокачка 0, только стартовый пул
		public static List<AbstractTask> Generate(Level level) {
			List<AbstractTask> tasks = new List<AbstractTask>();
			tasks.Add(CreateRandomPrimaryTask(level != null ? level.difficulty : LevelDifficulty.easy));
			tasks.AddRange(CreateSecondaryTasks(level?.Id));
			return tasks;
		}

		private static AbstractTask CreateRandomPrimaryTask(LevelDifficulty difficulty) {
			TaskType type = Random.value > 0.5f ? TaskType.ReachWave : TaskType.KillBoss;
			int value = Mathf.Max(Mathf.RoundToInt((type == TaskType.ReachWave ? Random.Range(10, 16) : 1) * GetDifficultyMultiplier(difficulty)), 1);

			return tasksFactory.CreatePrimary(type, value);
		}

		// Виды доп. тасков, доступные при текущих покупках уровня
		public static List<TaskDefinition> GetAvailableDefinitions(string levelId) {
			var res = new List<TaskDefinition>();
			var settings = GameConfigs.Instance.LevelTasksSettings;
			if (settings == null)
				return res;

			var unlocked = LevelUpgradesManager.GetUnlockedTaskTypes(levelId);
			foreach (var definition in settings.Definitions) {
				if (definition == null || definition.Weight <= 0f)
					continue;
				if (definition.AvailableFromStart || unlocked.Contains(definition.Type))
					res.Add(definition);
			}
			return res;
		}

		private static List<AbstractTask> CreateSecondaryTasks(string levelId) {
			var tasks = new List<AbstractTask>();
			var settings = GameConfigs.Instance.LevelTasksSettings;
			if (settings == null) {
				Debug.LogWarning("[LevelTasksGenerator] LevelTasksSettings не назначен в GameConfigs — доп. тасков не будет (Tools/Level Tasks/Create Default Settings).");
				return tasks;
			}

			float progress = LevelUpgradesManager.GetIslandProgress01(levelId);
			var pool = GetAvailableDefinitions(levelId);

			while (tasks.Count < settings.SecondaryCount && pool.Count > 0) {
				var definition = PickWeighted(pool);
				// Не выдаём два таска на одно направление билда («урон огнём» + «горят одновременно»)
				pool.RemoveAll(d => d.Group == definition.Group);

				var task = tasksFactory.Create(definition, definition.GetTarget(progress));
				if (task != null)
					tasks.Add(task);
			}
			return tasks;
		}

		private static TaskDefinition PickWeighted(List<TaskDefinition> pool) {
			float total = 0f;
			foreach (var definition in pool)
				total += definition.Weight;

			float roll = Random.value * total;
			foreach (var definition in pool) {
				roll -= definition.Weight;
				if (roll <= 0f)
					return definition;
			}
			return pool[pool.Count - 1];
		}

		private static float GetDifficultyMultiplier(LevelDifficulty difficulty) {
			switch (difficulty) {
				case LevelDifficulty.easy:
					return 0.5f;
				case LevelDifficulty.medium:
					return 1f;
				case LevelDifficulty.hard:
					return 2f;
				default:
					return 1f;
			}
		}
	}
}
