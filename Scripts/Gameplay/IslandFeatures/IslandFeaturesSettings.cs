using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.IslandFeatures {
	// Особенности островов, по одной на уровень. Контент генерируется билдером
	// (Tools/Island Features/Rebuild Island Features), ручные правки в инспекторе перетираются его перезапуском.
	[CreateAssetMenu(fileName = "IslandFeaturesSettings", menuName = "Settings/IslandFeaturesSettings")]
	public class IslandFeaturesSettings : ScriptableObject {
		[ListDrawerSettings(ListElementLabelName = "LevelId")]
		[SerializeField] List<IslandFeatureDefinition> features = new();

		public List<IslandFeatureDefinition> Features => features;

		public IslandFeatureDefinition GetFeature(string levelId) {
			if (string.IsNullOrEmpty(levelId))
				return null;
			foreach (var feature in features) {
				if (feature != null && feature.LevelId == levelId)
					return feature;
			}
			return null;
		}

#if UNITY_EDITOR
		public void EditorSetFeatures(List<IslandFeatureDefinition> features) {
			this.features = features ?? new List<IslandFeatureDefinition>();
		}

		private void OnValidate() {
			var seenLevels = new HashSet<string>();
			var seenIds = new HashSet<string>();
			foreach (var feature in features) {
				if (feature == null) {
					Debug.LogWarning($"[IslandFeaturesSettings] '{name}': пустая запись особенности.", this);
					continue;
				}
				if (string.IsNullOrEmpty(feature.LevelId))
					Debug.LogWarning($"[IslandFeaturesSettings] '{name}': особенность '{feature.Id}' без levelId.", this);
				else if (!seenLevels.Add(feature.LevelId))
					Debug.LogWarning($"[IslandFeaturesSettings] '{name}': у уровня '{feature.LevelId}' больше одной особенности — работает первая.", this);

				if (string.IsNullOrEmpty(feature.Id))
					Debug.LogWarning($"[IslandFeaturesSettings] '{name}': особенность уровня '{feature.LevelId}' с пустым id.", this);
				else if (!seenIds.Add(feature.Id))
					Debug.LogWarning($"[IslandFeaturesSettings] '{name}': дублирующийся id особенности '{feature.Id}'.", this);

				foreach (var effector in feature.Effectors) {
					if (effector == null)
						Debug.LogWarning($"[IslandFeaturesSettings] '{name}': особенность '{feature.Id}' содержит пустой эффектор.", this);
				}
			}
		}
#endif
	}
}
