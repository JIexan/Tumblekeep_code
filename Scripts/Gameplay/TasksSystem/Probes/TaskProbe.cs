using System;
using UnityEngine;

namespace Gameplay.TasksSystem.Probes {
	// Измеритель одной метрики катки без знания о цели. Таск = проба + цель (ProbeTask);
	// автоплей держит пробы всех видов сразу и пишет их значения в статистику (TaskMetricsRecorder).
	// Счётчики копят сумму, рекорды держат максимум — наружу это одно Value.
	public abstract class TaskProbe {
		private const float TickInterval = 0.1f;

		private bool isStarted;
		private float nextTick;
		private float accumulated;

		public int Value { get; private set; }
		public event Action Changed;

		public void Start() {
			if (isStarted)
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

		// now — Time.time (игровое время: на ускорении автоплея окна тасков идут в игровых секундах)
		public void Tick(float now) {
			if (!isStarted || now < nextTick)
				return;
			nextTick = now + TickInterval;
			OnTick(now);
		}

		protected abstract void OnStart();
		protected abstract void OnStop();
		protected virtual void OnTick(float now) { }

		protected void Add(float delta) {
			if (delta <= 0f)
				return;
			accumulated += delta;
			Set(Mathf.FloorToInt(accumulated));
		}

		protected void RaiseTo(int value) {
			if (value > Value)
				Set(value);
		}

		private void Set(int value) {
			if (value == Value)
				return;
			Value = value;
			Changed?.Invoke();
		}
	}
}
