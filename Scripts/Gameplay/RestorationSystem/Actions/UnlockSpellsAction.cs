using System.Collections.Generic;
using Gameplay.Unlocks;
using UnityEngine;

namespace Gameplay.RestorationSystem.Actions {
	// Мета-анлок: добавить свитки (ItemType >= 50) в пулы сундуков и торговца во всех катках.
	// Идемпотентно — Apply() гоняется при каждом входе в хаб для завершённых шагов.
	[System.Serializable]
	public class UnlockSpellsAction : BaseProgressAction, IUnlockAction {
		[SerializeField] List<ItemType> spells = new();

		public IReadOnlyList<ItemType> Spells => spells;

		public override void Apply() {
			if (!Application.isPlaying) return;
			foreach (var spell in spells)
				ContentUnlocksManager.UnlockSpell(spell);
		}

#if UNITY_EDITOR
		public void EditorConfigure(IEnumerable<ItemType> types, float chainDelay = 0f) {
			spells = new List<ItemType>(types);
			startDelay = chainDelay;
		}
#endif
	}
}
