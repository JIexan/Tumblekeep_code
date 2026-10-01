using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.LevelUpgrades {
	// Цепочки апгрейдов: все перечисленные апгрейды ЭТОГО ЖЕ уровня должны быть куплены.
	// Битые/пустые id никогда не проходят проверку — ошибка конфига видна как met=false
	// (плюс warning в LevelUpgradesConfig.OnValidate).
	[System.Serializable]
	public class PurchasedUpgradesUpgradeRequirement : BaseUpgradeRequirement {
		[SerializeField] List<string> upgradeIds = new();

		public List<string> UpgradeIds => upgradeIds;

		public override bool IsMet(string levelId) {
			foreach (var upgradeId in upgradeIds) {
				if (!LevelUpgradesManager.IsPurchased(levelId, upgradeId))
					return false;
			}
			return true;
		}

		public override (int current, int required) GetProgress(string levelId) {
			int purchased = 0;
			foreach (var upgradeId in upgradeIds) {
				if (LevelUpgradesManager.IsPurchased(levelId, upgradeId))
					purchased++;
			}
			return (purchased, upgradeIds.Count);
		}

#if UNITY_EDITOR
		public void EditorInit(List<string> upgradeIds) {
			this.upgradeIds = upgradeIds ?? new List<string>();
		}
#endif
	}
}
