using Assets.SimpleLocalization;
using Gameplay.LevelUpgrades;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Секция одной категории на вкладке апгрейдов: заголовок + дерево узлов с рёбрами.
// Порядок отрисовки: подложки групп → рёбра (оба в edgesContainer) → узлы (nodesContainer).
// Позиции считает UpgradeTreeLayout.Arrange; здесь только инстансы и состояния.
public class UpgradeCategoryTreePanel : MonoBehaviour {
	[SerializeField] TMP_Text title;
	[SerializeField] RectTransform edgesContainer;
	[SerializeField] RectTransform nodesContainer;
	[SerializeField] UpgradeNodeView nodePrefab;
	[SerializeField] UpgradeEdgeView edgePrefab;
	[SerializeField] LayoutElement layoutElement;

	[Header("Раскладка")]
	[SerializeField] UpgradeTreeLayout.Settings layout = UpgradeTreeLayout.Settings.Default;
	[SerializeField] float headerHeight = 60f;
	[SerializeField] float topPadding = 67.5f;
	[SerializeField] float bottomPadding = 67.5f;
	[SerializeField] float edgeThickness = 36f;

	[Header("Подложка группы листьев")]
	[SerializeField] Sprite groupSprite;
	[SerializeField] Color groupColor = new Color(0f, 0f, 0f, 0.35f);
	[Tooltip("Насколько ребро «родитель → группа» заходит внутрь подложки")]
	[SerializeField] float groupEdgeInset = 10f;

	private readonly List<UpgradeNodeView> nodeViews = new List<UpgradeNodeView>();
	private readonly List<UpgradeEdgeView> edgeViews = new List<UpgradeEdgeView>();
	private readonly List<Image> groupViews = new List<Image>();
	// Источник каждого ребра (пререквизит) — для обновления яркости после покупок.
	private readonly List<UpgradeTreeLayout.Node> edgeSources = new List<UpgradeTreeLayout.Node>();

	private string levelId;

	public void Init(string levelId, UpgradeTreeLayout.Tree tree) {
		this.levelId = levelId;

		if (title != null)
			title.text = LocalizationManager.Localize("UpgradeCategory_" + tree.Category);

		UpgradeTreeLayout.Arrange(tree, layout);
		var offset = new Vector2(0f, -topPadding);

		// Узлы.
		int nodeIndex = 0;
		foreach (var node in tree.Nodes) {
			UpgradeNodeView view;
			if (nodeIndex < nodeViews.Count) {
				view = nodeViews[nodeIndex];
				view.gameObject.SetActive(true);
			} else {
				view = Instantiate(nodePrefab, nodesContainer);
				nodeViews.Add(view);
			}
			((RectTransform)view.transform).anchoredPosition = node.Position + offset;
			view.Init(levelId, node.Entry);
			nodeIndex++;
		}
		for (int i = nodeIndex; i < nodeViews.Count; i++)
			nodeViews[i].gameObject.SetActive(false);

		// Подложки групп — первыми в edgesContainer, чтобы лежать под рёбрами.
		int groupIndex = 0;
		foreach (var group in tree.Groups) {
			Image view;
			if (groupIndex < groupViews.Count) {
				view = groupViews[groupIndex];
				view.gameObject.SetActive(true);
			} else {
				view = CreateGroupView();
				groupViews.Add(view);
			}
			var rect = (RectTransform)view.transform;
			rect.anchoredPosition = group.Center + offset;
			rect.sizeDelta = group.Size;
			rect.SetSiblingIndex(groupIndex);
			groupIndex++;
		}
		for (int i = groupIndex; i < groupViews.Count; i++)
			groupViews[i].gameObject.SetActive(false);

		// Рёбра: по одному на пару (пререквизит -> узел), от центра к центру;
		// листья в сетке получают одно общее ребро «родитель -> подложка».
		int edgeIndex = 0;
		edgeSources.Clear();
		foreach (var node in tree.Nodes) {
			if (node.InGrid)
				continue;
			foreach (var prereq in node.Prerequisites)
				AddEdge(ref edgeIndex, prereq, prereq.Position + offset, node.Position + offset);
		}
		foreach (var group in tree.Groups)
			AddEdge(ref edgeIndex, group.Parent, group.Parent.Position + offset, group.TopCenter + offset + new Vector2(0f, -groupEdgeInset));
		for (int i = edgeIndex; i < edgeViews.Count; i++)
			edgeViews[i].gameObject.SetActive(false);

		if (layoutElement != null)
			layoutElement.preferredHeight = headerHeight + topPadding + tree.Height + bottomPadding;

		RefreshStates();
	}

	public void RefreshStates() {
		foreach (var view in nodeViews) {
			if (view.gameObject.activeSelf)
				view.Refresh();
		}
		for (int i = 0; i < edgeSources.Count; i++)
			edgeViews[i].SetState(LevelUpgradesManager.IsPurchased(levelId, edgeSources[i].Entry.Id));
	}

	private void AddEdge(ref int edgeIndex, UpgradeTreeLayout.Node source, Vector2 from, Vector2 to) {
		UpgradeEdgeView edge;
		if (edgeIndex < edgeViews.Count) {
			edge = edgeViews[edgeIndex];
			edge.gameObject.SetActive(true);
		} else {
			edge = Instantiate(edgePrefab, edgesContainer);
			edgeViews.Add(edge);
		}
		edge.Init(from, to, edgeThickness);
		edgeSources.Add(source);
		edgeIndex++;
	}

	// Подложка — простой Image с якорями узлов (top-center контейнера); без спрайта — плоский прямоугольник.
	private Image CreateGroupView() {
		var go = new GameObject("Group", typeof(RectTransform), typeof(Image));
		var rect = (RectTransform)go.transform;
		rect.SetParent(edgesContainer, false);
		rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		var image = go.GetComponent<Image>();
		image.sprite = groupSprite;
		image.type = groupSprite != null && groupSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
		image.color = groupColor;
		image.raycastTarget = false;
		return image;
	}
}
