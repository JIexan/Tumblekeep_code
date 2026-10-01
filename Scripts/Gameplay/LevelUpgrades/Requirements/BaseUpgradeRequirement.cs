namespace Gameplay.LevelUpgrades {
	[System.Serializable]
	public abstract class BaseUpgradeRequirement {
		// levelId — уровень, которому принадлежит конфиг апгрейда: контекст для требований,
		// завязанных на «свой» уровень (цепочки апгрейдов, левел острова). Остальные игнорируют.
		public abstract bool IsMet(string levelId);

		// Списание оплаты при покупке; требования-условия ничего не тратят.
		public virtual void Consume(string levelId) { }

		public abstract (int current, int required) GetProgress(string levelId);
	}
}
