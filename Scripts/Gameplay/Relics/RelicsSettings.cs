using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Relics {
	// Каталог реликвий. Контент генерируется билдером (Tools/Relics/Rebuild Relics Catalog),
	// ручные правки в инспекторе перетираются перезапуском билдера.
	[CreateAssetMenu(fileName = "RelicsSettings", menuName = "Settings/RelicsSettings")]
	// Шансы/таблицы выпадения из сундуков живут в ChestsSettings (по тирам), здесь — только каталог.
	public class RelicsSettings : ScriptableObject {
		[ListDrawerSettings(ListElementLabelName = "Id")]
		[SerializeField] List<RelicDefinition> relics = new();

		public List<RelicDefinition> Relics => relics;

		public RelicDefinition GetRelic(string id) {
			foreach (var relic in relics) {
				if (relic != null && relic.Id == id)
					return relic;
			}
			return null;
		}

#if UNITY_EDITOR
		public void EditorSetRelics(List<RelicDefinition> relics) {
			this.relics = relics ?? new List<RelicDefinition>();
		}

		private void OnValidate() {
			var seen = new HashSet<string>();
			foreach (var relic in relics) {
				if (relic == null) {
					Debug.LogWarning($"[RelicsSettings] '{name}': пустая запись реликвии.", this);
					continue;
				}
				if (string.IsNullOrEmpty(relic.Id))
					Debug.LogWarning($"[RelicsSettings] '{name}': реликвия с пустым id.", this);
				else if (!seen.Add(relic.Id))
					Debug.LogWarning($"[RelicsSettings] '{name}': дублирующийся id реликвии '{relic.Id}'.", this);

				if (!relic.Stackable && relic.MaxStacks > 0)
					Debug.LogWarning($"[RelicsSettings] '{name}': реликвия '{relic.Id}' нестакаемая, но maxStacks > 0 — лимит не имеет смысла.", this);

				foreach (var effector in relic.Effectors) {
					if (effector == null)
						Debug.LogWarning($"[RelicsSettings] '{name}': реликвия '{relic.Id}' содержит пустой эффектор.", this);
				}
			}
		}
#endif
	}
}
