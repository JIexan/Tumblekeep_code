using System.Collections.Generic;
using Gameplay.Unlocks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.RestorationSystem.Actions {
	// Мета-анлок: добавить реликвии в пул выпадения (RelicsManager.CanDrop) во всех катках.
	// Идемпотентно — Apply() гоняется при каждом входе в хаб для завершённых шагов.
	[System.Serializable]
	public class UnlockRelicsAction : BaseProgressAction, IUnlockAction {
		[ValueDropdown(nameof(RelicIdOptions))]
		[SerializeField] List<string> relicIds = new();

		public IReadOnlyList<string> RelicIds => relicIds;

		public override void Apply() {
			// Только в рантайме: превью инспектора тоже зовёт Apply, а SavesManager в edit-mode нет.
			if (!Application.isPlaying) return;
			foreach (var id in relicIds)
				ContentUnlocksManager.UnlockRelic(id);
		}

		// Не под #if UNITY_EDITOR — nameof должен резолвиться и в билде.
		private static IEnumerable<ValueDropdownItem<string>> RelicIdOptions() {
			var catalog = GameConfigs.Instance != null ? GameConfigs.Instance.RelicsSettings : null;
			if (catalog == null)
				yield break;
			foreach (var relic in catalog.Relics) {
				if (relic == null || string.IsNullOrEmpty(relic.Id))
					continue;
				yield return new ValueDropdownItem<string>(relic.Rarity + "/" + relic.Id, relic.Id);
			}
		}

#if UNITY_EDITOR
		public void EditorConfigure(IEnumerable<string> ids, float chainDelay = 0f) {
			relicIds = new List<string>(ids);
			startDelay = chainDelay;
		}
#endif
	}
}
