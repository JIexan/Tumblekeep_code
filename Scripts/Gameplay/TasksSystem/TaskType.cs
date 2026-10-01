namespace Gameplay.TasksSystem {
	// Сериализуется в LevelTasksSettings и в LevelUpgradeEntry.unlockedTasks —
	// значения не менять и не переиспользовать, новые типы дописывать в конец.
	public enum TaskType {
		// Основные (условие победы)
		ReachWave = 0,
		KillBoss = 1,

		// Дополнительные
		CollectGold = 2,
		OneShotKills = 3,            // убить X врагов с одного удара (с полного ХП)
		DamagedInWindow = 4,         // задеть X разных врагов за Y секунд
		MultiHit = 5,                // задеть X врагов одним ударом
		MultiKill = 6,               // убить X врагов одним ударом
		BiggestHit = 7,              // нанести X урона одним попаданием
		FireDamageTotal = 8,
		BurningSimultaneously = 9,
		PoisonDamageTotal = 10,
		PoisonedSimultaneously = 11,
		SlowedSimultaneously = 12,   // X врагов одновременно под замедлением электро-башни
		SlowedDuration = 13,         // один враг под замедлением X секунд подряд
	}

	// Направление билда, на которое давит таск. Два таска одной группы в одну катку не выдаются.
	public enum TaskGroup {
		Economy = 0,
		Burst = 1,
		Area = 2,
		Tempo = 3,
		Fire = 4,
		Poison = 5,
		Electric = 6,
	}
}
