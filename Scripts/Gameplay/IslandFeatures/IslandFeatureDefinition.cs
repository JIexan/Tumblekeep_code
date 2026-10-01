using System.Collections.Generic;
using Gameplay.Relics;
using UnityEngine;

namespace Gameplay.IslandFeatures {
	// Особенность острова: пассивный модификатор уровня на эффекторах реликвий
	// (те же BaseRelicEffector + условия, исполняет RelicsManager как источник в один стак).
	// Ключи локализации выводятся из id: IslandFeatureName_/Desc_/Hint_{id}.
	// Desc — короткая механика для ленты окна уровня, Hint — развёрнутая подсказка по наведению.
	[System.Serializable]
	public class IslandFeatureDefinition {
		// Совпадает с Level.Id из LevelSettings
		[SerializeField] string levelId;
		[SerializeField] string id;
		[SerializeField] Sprite icon;
		[SerializeReference] List<BaseRelicEffector> effectors = new();

		public string LevelId => levelId;
		public string Id => id;
		public Sprite Icon => icon;
		public List<BaseRelicEffector> Effectors => effectors;

		public string NameKey => "IslandFeatureName_" + id;
		public string DescKey => "IslandFeatureDesc_" + id;
		public string HintKey => "IslandFeatureHint_" + id;

#if UNITY_EDITOR
		// Программное наполнение (IslandFeaturesBuilder); в рантайме не используется.
		public void EditorInit(string levelId, string id, Sprite icon, List<BaseRelicEffector> effectors) {
			this.levelId = levelId;
			this.id = id;
			this.icon = icon;
			this.effectors = effectors ?? new List<BaseRelicEffector>();
		}
#endif
	}
}
