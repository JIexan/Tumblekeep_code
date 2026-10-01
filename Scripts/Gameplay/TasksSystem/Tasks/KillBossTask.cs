using Assets.SimpleLocalization;
using UnityEngine;

namespace Gameplay.TasksSystem.Tasks {
	public class KillBossTask : AbstractTask {
		public KillBossTask(int amount) : base(amount) {
		}

		public override TaskType Type => TaskType.KillBoss;
		public override string Description => LocalizationManager.Localize("KillBossTaskDescription");

		protected override void OnStart() {
			InGameEventManager.Subscribe(InGameEvents.OnUnitKilled, OnUnitKilled);
		}

		protected override void OnStop() {
			InGameEventManager.Unsubscribe(InGameEvents.OnUnitKilled, OnUnitKilled);
		}

		private void OnUnitKilled(object obj) {
			// Босс, дошедший до замка, тоже шлёт OnUnitKilled — за таск не считается
			if (obj is Unit unit && unit.IsBoss && !unit.ReachedCastle) {
				currentValue += 1;
				OnUpdate();
				if (currentValue >= targetValue) {
					Complete();
				}
			}
		}
	}
}
