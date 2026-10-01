using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Relics {
	// Описание одной реликвии. Ключи локализации выводятся из id:
	// RelicName_{id} / RelicDesc_{id} (см. Assets/Resources/localizations.json).
	[System.Serializable]
	public class RelicDefinition {
		[SerializeField] string id;
		[SerializeField] RarityType rarity = RarityType.Common;
		[SerializeField] bool stackable;
		// 0 = без лимита; имеет смысл только для стакаемых.
		[SerializeField] int maxStacks;
		[SerializeField] Sprite icon;
		[SerializeReference] List<BaseRelicEffector> effectors = new();

		public string Id => id;
		public RarityType Rarity => rarity;
		public bool Stackable => stackable;
		public int MaxStacks => maxStacks;
		public Sprite Icon => icon;
		public List<BaseRelicEffector> Effectors => effectors;

		public string NameKey => "RelicName_" + id;
		public string DescKey => "RelicDesc_" + id;

#if UNITY_EDITOR
		// Программное наполнение каталога (RelicsCatalogBuilder); в рантайме не используется.
		public void EditorInit(string id, RarityType rarity, bool stackable, int maxStacks,
			Sprite icon, List<BaseRelicEffector> effectors) {
			this.id = id;
			this.rarity = rarity;
			this.stackable = stackable;
			this.maxStacks = maxStacks;
			this.icon = icon;
			this.effectors = effectors ?? new List<BaseRelicEffector>();
		}
#endif
	}
}
