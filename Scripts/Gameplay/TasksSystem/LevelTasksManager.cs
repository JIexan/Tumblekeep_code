using Gameplay.TasksSystem.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.TasksSystem {
	public class LevelTasksManager {
		private LevelTasksCollection levelTasksCollection;
		private static LevelTasksManager instance;
		// Матч идёт: между OnLevelStarted и уничтожением GameController сцены
		private bool levelStarted;

		public static LevelTasksManager Instance => instance ??= new LevelTasksManager();

		public LevelTasksCollection LevelTasksCollection => levelTasksCollection;

		private LevelTasksManager() {
			InGameEventManager.Subscribe(InGameEvents.OnLevelStarted, OnLevelStarted);
		}

		~LevelTasksManager() {
			InGameEventManager.Unsubscribe(InGameEvents.OnLevelStarted, OnLevelStarted);
		}

		private void OnLevelStarted(object obj) {
			levelStarted = true;
			StartLevel();
		}

		public void SetLevelTasks(List<AbstractTask> tasks) {
			StopTasks();
			levelTasksCollection = new LevelTasksCollection(tasks);
			// Таски, выданные уже после старта матча (фолбэк TasksPanel, реролл читом), иначе остались бы мёртвыми
			if (levelStarted)
				StartLevel();
		}

		public void StartLevel() {
			if (levelTasksCollection != null) {
				levelTasksCollection.Start();
			}
		}

		// Каждый кадр матча из GameController.Update: пробам тасков с окнами по времени
		public void Tick() {
			if (levelTasksCollection == null)
				return;
			float now = Time.time;
			var tasks = levelTasksCollection.Tasks;
			for (int i = 0; i < tasks.Count; i++) {
				if (!tasks[i].IsCompleted)
					tasks[i].Tick(now);
			}
		}

		public void Clear() {
			StopTasks();
			levelTasksCollection = null;
		}

		// Боевая сцена выгружается. Коллекцию не обнуляем: VictoryWindow/читы могут читать её до конца кадра,
		// а следующая катка всё равно придёт со своим SetLevelTasks
		public void OnLevelEnded() {
			levelStarted = false;
			StopTasks();
		}

		private void StopTasks() {
			if (levelTasksCollection == null)
				return;
			foreach (var task in levelTasksCollection.Tasks)
				task?.Stop();
		}

		public List<AbstractTask> GetCurrentTasksList() {
			return levelTasksCollection?.Tasks;
		}
	}
}
