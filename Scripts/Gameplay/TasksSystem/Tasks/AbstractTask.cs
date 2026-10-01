using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.TasksSystem.Tasks {
	public class AbstractTask {
		protected int currentValue = 0;
		protected int targetValue = 0;

		private bool isCompleted = false;
		private bool isFailed = false;
		private bool isStarted = false;

		public delegate void TaskStateChanged(int currentValue, int targetValue);

		public bool IsCompleted { get { return isCompleted; } }
		public bool IsFailed { get { return isFailed; } }
		public virtual TaskType Type { get; }
		public virtual string Description { get; }


		public int CurrentValue => currentValue;
		public int TargetValue => targetValue;


		protected event TaskStateChanged onTaskStateChanged;

		public event TaskStateChanged OnTaskStateChanged {
			add {
				onTaskStateChanged -= value;
				onTaskStateChanged += value;
			}
			remove {
				onTaskStateChanged -= value;
			}
		}

		public AbstractTask(int amount) {
			targetValue = amount;
		}

		// Текст строки таска. showProgress: HUD катки (текущее/цель), иначе превью в хабе (только цель)
		public virtual string GetDescription(bool showProgress) {
			return showProgress ? Description + $" ({currentValue}/{targetValue})" : Description + " " + targetValue;
		}

		// Идемпотентны: таски лежат в статическом менеджере и переживают сцены, поэтому
		// подписки снимаются явно (LevelTasksManager.Clear/OnLevelEnded), а не только по выполнению
		public void Start() {
			if (isStarted || isCompleted)
				return;
			isStarted = true;
			OnStart();
		}

		public void Stop() {
			if (!isStarted)
				return;
			isStarted = false;
			OnStop();
		}

		// now — Time.time катки; зовётся каждый кадр, пока идёт матч
		public virtual void Tick(float now) { }

		protected virtual void OnStart() { }
		protected virtual void OnStop() { }

		public virtual void Complete() {
			if (IsCompleted) {
				return;
			}
			isCompleted = true;
			Stop();
			InGameEventManager.SendEvent(InGameEvents.OnTasksStateChanged);
			onTaskStateChanged?.Invoke(currentValue, targetValue);
		}

		public virtual void Fail() {
			if (IsFailed) {
				return;
			}
			isFailed = true;
			InGameEventManager.SendEvent(InGameEvents.OnTasksStateChanged);
			onTaskStateChanged?.Invoke(currentValue, targetValue);
		}

		protected void OnUpdate() {
			onTaskStateChanged?.Invoke(currentValue, targetValue);
		}
	}
}
