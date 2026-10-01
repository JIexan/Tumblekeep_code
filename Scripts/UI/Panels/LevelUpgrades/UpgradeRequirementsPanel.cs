using Gameplay.LevelUpgrades;
using System.Collections.Generic;
using UnityEngine;

// Вертикальный список требований апгрейда (пул строк по паттерну TasksPanel).
public class UpgradeRequirementsPanel : MonoBehaviour {
	[SerializeField] UpgradeRequirementRow rowPrefab;

	private readonly List<UpgradeRequirementRow> rows = new List<UpgradeRequirementRow>();

	// skip — требование, показанное вне списка (цена на PurchaseButton окна), в строки не попадает.
	public void Init(LevelUpgradeEntry entry, string levelId, BaseUpgradeRequirement skip = null) {
		int rowIndex = 0;
		if (entry != null && entry.Requirements != null) {
			foreach (var requirement in entry.Requirements) {
				if (requirement == null || requirement == skip)
					continue;

				UpgradeRequirementRow row;
				if (rowIndex < rows.Count) {
					row = rows[rowIndex];
					row.gameObject.SetActive(true);
				} else {
					row = Instantiate(rowPrefab, transform);
					rows.Add(row);
				}
				row.Init(requirement, levelId);
				rowIndex++;
			}
		}

		for (int i = rowIndex; i < rows.Count; i++)
			rows[i].gameObject.SetActive(false);
	}

	public void Refresh() {
		foreach (var row in rows) {
			if (row.gameObject.activeSelf)
				row.Refresh();
		}
	}
}
