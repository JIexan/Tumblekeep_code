using UnityEngine;

namespace Gameplay.LevelUpgrades {
	[System.Serializable]
	public class ItemUpgradeRequirement : BaseUpgradeRequirement {
		[SerializeField] ItemType item = ItemType.Crystal;
		[SerializeField] int count = 1;

		public ItemType Item => item;
		public int Count => count;

		public override bool IsMet(string levelId) {
			return SavesManager.Profile.HasItem(item, count);
		}

		public override void Consume(string levelId) {
			SavesManager.Profile.SpendItem(item, count);
		}

		public override (int current, int required) GetProgress(string levelId) {
			return (SavesManager.Profile.GetItem(item), count);
		}

#if UNITY_EDITOR
		public void EditorInit(ItemType item, int count) {
			this.item = item;
			this.count = count;
		}
#endif
	}
}
