using Gameplay.TasksSystem.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.TasksSystem {
	[System.Serializable]
	public class LevelTasksCollection {
		private List<AbstractTask> tasks = new List<AbstractTask>();

		public LevelTasksCollection(List<AbstractTask> tasksList) {
			tasks = tasksList;
		}


		public bool PrimaryTaskCompleted {
			get {
				if (tasks.Count > 0) {
					return tasks[0].IsCompleted;
				}
				return false;
			}
		}

		public bool AllTasksFinished {
			get {
				foreach (var task in tasks) {
					if (!(task.IsCompleted || task.IsFailed)) {
						return false;
					}
				}
				return true;
			}
		}

		public List<AbstractTask> Tasks => tasks;

		public void Start() {
			foreach (var task in tasks) {
				task.Start();
			}
		}

		public int GetCompletedTasksCount() {
			int res = 0;
			foreach (var task in tasks) {
				if (task.IsCompleted) {
					res++;
				}
			}

			return res;
		}

		// Выполненные доп. таски ([1..]) — от них считается множитель наград (MatchRewards.GetMultiplier)
		public int SecondaryCompletedCount => GetCompletedTasksCount() - (PrimaryTaskCompleted ? 1 : 0);
	}
}
