namespace Gameplay.Relics {
	// Контекст вычисления эффекта: для событийных запросов (килл, спавн, входящий урон)
	// несёт юнита, для контекстно-независимых — None. Условия с RequiresUnit при
	// отсутствии юнита дают false, поэтому «только против летающих» не протекает
	// в общий агрегат.
	public readonly struct RelicContext {
		public readonly Unit Unit;

		public RelicContext(Unit unit) {
			Unit = unit;
		}

		public static readonly RelicContext None = default;
	}
}
