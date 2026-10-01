using Gameplay.TasksSystem.Probes;
using Gameplay.TasksSystem.Tasks;

namespace Gameplay.TasksSystem {
	public class LevelTasksFactory {
		// Основные таски (условие победы)
		public AbstractTask CreatePrimary(TaskType type, int param) {
			switch (type) {
				case TaskType.ReachWave:
					return new ReachWaveTask(param);
				case TaskType.KillBoss:
					return new KillBossTask(param);
				default:
					return null;
			}
		}

		// Доп. таск по описанию из LevelTasksSettings
		public AbstractTask Create(TaskDefinition definition, int target) {
			var probe = CreateProbe(definition.Type, definition.Param);
			if (probe == null)
				return null;
			return new ProbeTask(definition.Type, probe, target, definition.DescriptionKey, definition.Param);
		}

		// Отдельно от таска: автоплей меряет все виды тасков без целей
		public static TaskProbe CreateProbe(TaskType type, float param) {
			var statusEffects = GameConfigs.Instance.StatusEffectsSettings;
			switch (type) {
				case TaskType.CollectGold:
					return new CollectGoldProbe();
				case TaskType.OneShotKills:
					return new OneShotKillsProbe();
				case TaskType.DamagedInWindow:
					return new DamagedInWindowProbe(param > 0f ? param : 2f);
				case TaskType.MultiHit:
					return new MultiHitProbe();
				case TaskType.MultiKill:
					return new MultiKillProbe();
				case TaskType.BiggestHit:
					return new BiggestHitProbe();
				case TaskType.FireDamageTotal:
					return new DamageByTypeProbe(DamageType.Fire);
				case TaskType.BurningSimultaneously:
					// Огонь тикает раз в секунду (FireTrait)
					return new AffectedSimultaneouslyProbe(DamageType.Fire, 1f);
				case TaskType.PoisonDamageTotal:
					return new DamageByTypeProbe(DamageType.Poison);
				case TaskType.PoisonedSimultaneously:
					return new AffectedSimultaneouslyProbe(DamageType.Poison, statusEffects != null ? statusEffects.poisonTickInterval : 1f);
				case TaskType.SlowedSimultaneously:
					return new SlowedSimultaneouslyProbe();
				case TaskType.SlowedDuration:
					return new SlowedDurationProbe();
				default:
					return null;
			}
		}
	}
}
