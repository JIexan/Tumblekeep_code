using Assets.SimpleLocalization;

namespace Gameplay.TasksSystem.Tasks {
	public class ReachWaveTask : AbstractTask {
		public ReachWaveTask(int amount) : base(amount) {
		}

		public override TaskType Type => TaskType.ReachWave;
		public override string Description => LocalizationManager.Localize("ReachWaveTaskDescription");

		protected override void OnStart() {
			InGameEventManager.Subscribe(InGameEvents.OnWaveStarted, OnWaveStarted);
		}

		protected override void OnStop() {
			InGameEventManager.Unsubscribe(InGameEvents.OnWaveStarted, OnWaveStarted);
		}

		private void OnWaveStarted(object obj) {
			if (obj is int waveNumber) {
				if (currentValue != waveNumber) {
					currentValue = waveNumber;
					OnUpdate();
				}
				if (currentValue >= targetValue) {
					Complete();
				}
			}
		}
	}
}
