using Gameplay.LevelUpgrades;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Корень вкладки апгрейдов в LevelInfoWindow: строит деревья категорий
// в контент скролла. Ленивый билд — дерево собирается при первом открытии вкладки.
// Широкие категории (fullWidthCategories) занимают строку целиком, остальные встают рядом
// по columnsPerRow в строке — так нижние категории видны краешком сразу под верхней.
public class LevelUpgradesTab : MonoBehaviour {
	[SerializeField] RectTransform content;
	[SerializeField] UpgradeCategoryTreePanel treePanelPrefab;
	[SerializeField] ScrollRect scroll;

	[Header("Раскладка категорий")]
	[SerializeField] List<UpgradeCategory> fullWidthCategories = new List<UpgradeCategory> { UpgradeCategory.Towers };
	[SerializeField] int columnsPerRow = 2;
	[SerializeField] float rowSpacing = 20f;

	private readonly List<UpgradeCategoryTreePanel> panels = new List<UpgradeCategoryTreePanel>();
	private readonly List<RectTransform> rows = new List<RectTransform>();
	private string levelId;
	private bool dirty;

	public void Setup(string levelId) {
		if (this.levelId != levelId)
			dirty = true;
		this.levelId = levelId;
		if (dirty && isActiveAndEnabled)
			Rebuild();
	}

	private void OnEnable() {
		InGameEventManager.Subscribe(InGameEvents.OnLevelUpgradePurchased, OnStateChanged);
		InGameEventManager.Subscribe(InGameEvents.OnItemCountChanged, OnStateChanged);
		InGameEventManager.Subscribe(InGameEvents.OnIslandXPChanged, OnStateChanged);

		if (dirty)
			Rebuild();
		else
			RefreshStates(); // состояние могло измениться, пока вкладка была скрыта
	}

	private void OnDisable() {
		InGameEventManager.Unsubscribe(InGameEvents.OnLevelUpgradePurchased, OnStateChanged);
		InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnStateChanged);
		InGameEventManager.Unsubscribe(InGameEvents.OnIslandXPChanged, OnStateChanged);
	}

	private void OnStateChanged(object _) {
		RefreshStates();
	}

	private void Rebuild() {
		dirty = false;
		var trees = UpgradeTreeLayout.Build(LevelUpgradesManager.GetConfig(levelId));

		int panelIndex = 0, rowIndex = 0, siblingIndex = 0, inRow = 0;
		RectTransform row = null;
		foreach (var tree in trees) {
			UpgradeCategoryTreePanel panel;
			if (panelIndex < panels.Count) {
				panel = panels[panelIndex];
				panel.gameObject.SetActive(true);
			} else {
				panel = Instantiate(treePanelPrefab, content);
				panels.Add(panel);
			}

			if (fullWidthCategories.Contains(tree.Category) || columnsPerRow < 2) {
				panel.transform.SetParent(content, false);
				panel.transform.SetSiblingIndex(siblingIndex++);
				row = null;
			} else {
				if (row == null || inRow >= columnsPerRow) {
					row = GetRow(rowIndex++);
					row.SetSiblingIndex(siblingIndex++);
					inRow = 0;
				}
				panel.transform.SetParent(row, false);
				panel.transform.SetSiblingIndex(inRow++);
			}

			panel.Init(levelId, tree);
			panelIndex++;
		}
		for (int i = panelIndex; i < panels.Count; i++)
			panels[i].gameObject.SetActive(false);
		for (int i = rowIndex; i < rows.Count; i++)
			rows[i].gameObject.SetActive(false);

		if (scroll != null)
			scroll.verticalNormalizedPosition = 1f;
	}

	// Строка для категорий, стоящих рядом: делит ширину поровну, высота — по самой высокой панели.
	private RectTransform GetRow(int index) {
		if (index < rows.Count) {
			rows[index].gameObject.SetActive(true);
			return rows[index];
		}

		var go = new GameObject("CategoriesRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
		go.layer = content.gameObject.layer;
		var row = (RectTransform)go.transform;
		row.SetParent(content, false);
		var layout = go.GetComponent<HorizontalLayoutGroup>();
		layout.spacing = rowSpacing;
		layout.childAlignment = TextAnchor.UpperCenter;
		layout.childControlWidth = layout.childForceExpandWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandHeight = false;
		rows.Add(row);
		return row;
	}

	private void RefreshStates() {
		foreach (var panel in panels) {
			if (panel.gameObject.activeSelf)
				panel.RefreshStates();
		}
	}
}
