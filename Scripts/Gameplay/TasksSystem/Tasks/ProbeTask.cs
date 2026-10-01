using Assets.SimpleLocalization;
using Gameplay.TasksSystem.Probes;
using UnityEngine;

namespace Gameplay.TasksSystem.Tasks {
	// Доп. таск уровня: проба-измеритель + цель из LevelTasksSettings
	public class ProbeTask : AbstractTask {
		private readonly TaskType type;
		private readonly TaskProbe probe;
		private readonly string descriptionKey;
		private readonly float param;

		public ProbeTask(TaskType type, TaskProbe probe, int target, string descriptionKey, float param) : base(target) {
			this.type = type;
			this.probe = probe;
			this.descriptionKey = descriptionKey;
			this.param = param;
		}

		public override TaskType Type => type;
		public override string Description => GetDescription(false);

		// Формат-строка локализации: {0} — цель (в HUD «текущее/цель»), {1} — параметр вида таска
		public override string GetDescription(bool showProgress) {
			string value = showProgress ? $"{currentValue}/{targetValue}" : targetValue.ToString();
			string pattern = LocalizationManager.Localize(descriptionKey);
			// Ключа нет в таблице — Localize вернул "{key}", string.Format на нём упадёт
			if (pattern == "{" + descriptionKey + "}")
				return $"{descriptionKey}: {value}";
			return string.Format(pattern, value, param);
		}

		protected override void OnStart() {
			probe.Changed += OnProbeChanged;
			probe.Start();
		}

		protected override void OnStop() {
			probe.Changed -= OnProbeChanged;
			probe.Stop();
		}

		public override void Tick(float now) {
			probe.Tick(now);
		}

		private void OnProbeChanged() {
			currentValue = Mathf.Min(probe.Value, targetValue);
			OnUpdate();
			if (currentValue >= targetValue)
				Complete();
		}
	}
}
