using System;
using System.Collections.Generic;
using Gameplay.LevelUpgrades;
using UnityEngine;

// Чистая раскладка деревьев апгрейдов: по одному дереву на категорию.
// Build — структура (узлы, пререквизиты, глубина), Arrange — позиции в пикселях
// относительно top-center контейнера (x центрирован, y растёт вниз от 0).
//
// Раскладка «блоками»: каждый узел занимает блок шириной не меньше hSpacing; дети-цепочки
// ставятся под родителем бок о бок, а если ВСЕ дети — листья и их достаточно (minGridChildren),
// они складываются компактной сеткой gridColumns × N на подложке-группе (Group) с одним
// ребром «родитель → группа» вместо веера рёбер к каждому листу. Так 4 башни × 4 трейта
// влезают в ширину вкладки (4 блока по 2 колонки вместо 16 узлов в ряд).
// Чисто линейное дерево (цепочка Economy1→2→3…) укладывается «змейкой» по chainColumns узлов в ряд:
// ряды идут попеременно слева направо и справа налево, поэтому рёбра остаются короткими.
// Кросс-категорийные и битые пререквизиты в раскладку не входят (OnValidate конфига о них предупреждает).
public static class UpgradeTreeLayout {
	public class Node {
		public LevelUpgradeEntry Entry;
		public int Depth;
		public int IndexInDepth;
		public List<Node> Prerequisites = new List<Node>();
		// Заполняется Arrange: дети по «главному» пререквизиту (самому глубокому) и позиция.
		public List<Node> Children = new List<Node>();
		public Vector2 Position;
		public bool InGrid; // лист, уложенный в сетку группы родителя (рёбра к нему не рисуются)
	}

	// Подложка под сеткой листьев одного родителя.
	public class Group {
		public Node Parent;
		public Vector2 Center;
		public Vector2 Size;
		public Vector2 TopCenter => Center + new Vector2(0f, Size.y * 0.5f);
	}

	public class Tree {
		public UpgradeCategory Category;
		public List<Node> Nodes = new List<Node>();
		public List<Group> Groups = new List<Group>();
		public int MaxDepth;
		public int MaxRowCount;
		// Заполняется Arrange: габарит раскладки (высота — от центра верхнего ряда до нижней кромки).
		public float Width;
		public float Height;

		public int CountAtDepth(int depth) {
			int count = 0;
			foreach (var node in Nodes) {
				if (node.Depth == depth)
					count++;
			}
			return count;
		}
	}

	[Serializable]
	public struct Settings {
		[Tooltip("Ширина блока одиночного узла / шаг между узлами в ряду")]
		public float hSpacing;
		[Tooltip("Шаг между уровнями дерева")]
		public float vSpacing;
		[Tooltip("Шаг ячеек сетки листьев по горизонтали")]
		public float gridHSpacing;
		[Tooltip("Шаг ячеек сетки листьев по вертикали")]
		public float gridVSpacing;
		[Tooltip("Колонок в сетке листьев")]
		public int gridColumns;
		[Tooltip("От скольких детей-листьев они складываются сеткой (меньше — обычный ряд)")]
		public int minGridChildren;
		[Tooltip("Зазор между соседними блоками")]
		public float blockGap;
		[Tooltip("Поле подложки вокруг сетки")]
		public float groupPadding;
		[Tooltip("Узлов в ряду у линейной цепочки, уложенной змейкой (0–1 — обычное дерево сверху вниз)")]
		public int chainColumns;

		public static Settings Default => new Settings {
			hSpacing = 135f,
			vSpacing = 150f,
			gridHSpacing = 120f,
			gridVSpacing = 120f,
			gridColumns = 2,
			minGridChildren = 3,
			blockGap = 30f,
			groupPadding = 12f,
			chainColumns = 3,
		};
	}

	public static List<Tree> Build(LevelUpgradesConfig config) {
		var trees = new List<Tree>();
		if (config == null || config.Upgrades == null)
			return trees;

		// Группировка по категориям в порядке объявления enum, внутри — порядок списка.
		foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory))) {
			var entries = new List<LevelUpgradeEntry>();
			foreach (var entry in config.Upgrades) {
				if (entry != null && !string.IsNullOrEmpty(entry.Id) && entry.Category == category)
					entries.Add(entry);
			}
			if (entries.Count == 0)
				continue;

			trees.Add(BuildTree(category, entries));
		}
		return trees;
	}

	private static Tree BuildTree(UpgradeCategory category, List<LevelUpgradeEntry> entries) {
		var tree = new Tree { Category = category };
		var nodesById = new Dictionary<string, Node>();

		foreach (var entry in entries) {
			if (nodesById.ContainsKey(entry.Id))
				continue; // дубли id уже отловлены OnValidate конфига
			var node = new Node { Entry = entry };
			nodesById[entry.Id] = node;
			tree.Nodes.Add(node);
		}

		// Рёбра: только существующие в этой же категории, не self.
		foreach (var node in tree.Nodes) {
			foreach (var requirement in node.Entry.Requirements) {
				if (requirement is not PurchasedUpgradesUpgradeRequirement chain || chain.UpgradeIds == null)
					continue;
				foreach (var refId in chain.UpgradeIds) {
					if (string.IsNullOrEmpty(refId) || refId == node.Entry.Id)
						continue;
					if (nodesById.TryGetValue(refId, out var prereq) && !node.Prerequisites.Contains(prereq))
						node.Prerequisites.Add(prereq);
				}
			}
		}

		// Глубина: мемоизированный DFS; цикличное ребро даёт вклад 0 —
		// гарантированное завершение даже на битых данных.
		var depths = new Dictionary<Node, int>();
		var visiting = new HashSet<Node>();
		foreach (var node in tree.Nodes)
			ResolveDepth(node, depths, visiting);

		var countPerDepth = new Dictionary<int, int>();
		foreach (var node in tree.Nodes) {
			node.Depth = depths[node];
			countPerDepth.TryGetValue(node.Depth, out int index);
			node.IndexInDepth = index;
			countPerDepth[node.Depth] = index + 1;

			if (node.Depth > tree.MaxDepth)
				tree.MaxDepth = node.Depth;
		}
		foreach (var count in countPerDepth.Values) {
			if (count > tree.MaxRowCount)
				tree.MaxRowCount = count;
		}
		return tree;
	}

	private static int ResolveDepth(Node node, Dictionary<Node, int> depths, HashSet<Node> visiting) {
		if (depths.TryGetValue(node, out int cached))
			return cached;
		if (!visiting.Add(node))
			return 0; // цикл — считаем корнем, OnValidate конфига уже предупредил

		int depth = 0;
		foreach (var prereq in node.Prerequisites)
			depth = Math.Max(depth, ResolveDepth(prereq, depths, visiting) + 1);

		visiting.Remove(node);
		depths[node] = depth;
		return depth;
	}

	// ---------------------------------------------------------------------
	// Позиции
	// ---------------------------------------------------------------------

	public static void Arrange(Tree tree, Settings settings) {
		if (settings.hSpacing <= 0f)
			settings = Settings.Default;
		settings.gridColumns = Math.Max(1, settings.gridColumns);
		settings.minGridChildren = Math.Max(2, settings.minGridChildren);

		tree.Groups.Clear();
		foreach (var node in tree.Nodes) {
			node.Children.Clear();
			node.InGrid = false;
		}

		// Родитель в раскладке — самый глубокий пререквизит (именно он задаёт Depth узла).
		// Проверка Depth == parent.Depth + 1 отсекает узлы из циклов: они остаются корнями.
		var roots = new List<Node>();
		foreach (var node in tree.Nodes) {
			Node parent = null;
			foreach (var prereq in node.Prerequisites) {
				if (parent == null || prereq.Depth > parent.Depth)
					parent = prereq;
			}
			if (parent != null && node.Depth == parent.Depth + 1)
				parent.Children.Add(node);
			else
				roots.Add(node);
		}

		if (settings.chainColumns > 1 && roots.Count == 1 && TryArrangeChain(tree, roots[0], settings))
			return;

		var widths = new Dictionary<Node, float>();
		float total = 0f;
		foreach (var root in roots)
			total += Measure(root, settings, widths);
		total += Math.Max(0, roots.Count - 1) * settings.blockGap;

		float x = -total * 0.5f;
		foreach (var root in roots) {
			float width = widths[root];
			Place(root, x + width * 0.5f, 0f, settings, widths, tree);
			x += width + settings.blockGap;
		}

		float bottom = 0f;
		foreach (var node in tree.Nodes)
			bottom = Math.Max(bottom, -node.Position.y);
		foreach (var group in tree.Groups)
			bottom = Math.Max(bottom, -(group.Center.y - group.Size.y * 0.5f) - settings.gridVSpacing * 0.5f);
		tree.Width = total;
		tree.Height = bottom;
	}

	// Змейка для линейной цепочки; false — дерево ветвится (или состоит из одного узла), нужна обычная раскладка.
	private static bool TryArrangeChain(Tree tree, Node root, Settings settings) {
		var chain = new List<Node>();
		for (var node = root; node != null; node = node.Children.Count == 1 ? node.Children[0] : null) {
			if (node.Children.Count > 1)
				return false;
			chain.Add(node);
		}
		if (chain.Count < 2 || chain.Count != tree.Nodes.Count)
			return false;

		int columns = Math.Min(settings.chainColumns, chain.Count);
		int rows = (chain.Count + columns - 1) / columns;
		for (int i = 0; i < chain.Count; i++) {
			int row = i / columns, column = i % columns;
			if (row % 2 == 1)
				column = columns - 1 - column;
			chain[i].Position = new Vector2((column - (columns - 1) * 0.5f) * settings.hSpacing, -row * settings.vSpacing);
		}
		tree.Width = columns * settings.hSpacing;
		tree.Height = (rows - 1) * settings.vSpacing;
		return true;
	}

	private static bool IsGrid(Node node, Settings settings) {
		if (node.Children.Count < settings.minGridChildren)
			return false;
		foreach (var child in node.Children) {
			if (child.Children.Count > 0)
				return false;
		}
		return true;
	}

	private static float Measure(Node node, Settings settings, Dictionary<Node, float> widths) {
		float width;
		if (node.Children.Count == 0) {
			width = settings.hSpacing;
		} else if (IsGrid(node, settings)) {
			int columns = Math.Min(settings.gridColumns, node.Children.Count);
			width = Math.Max(settings.hSpacing, columns * settings.gridHSpacing + 2f * settings.groupPadding);
		} else {
			width = 0f;
			foreach (var child in node.Children)
				width += Measure(child, settings, widths);
			width += (node.Children.Count - 1) * settings.blockGap;
			width = Math.Max(width, settings.hSpacing);
		}
		widths[node] = width;
		return width;
	}

	private static void Place(Node node, float cx, float y, Settings settings, Dictionary<Node, float> widths, Tree tree) {
		node.Position = new Vector2(cx, y);
		if (node.Children.Count == 0)
			return;

		if (IsGrid(node, settings)) {
			int columns = Math.Min(settings.gridColumns, node.Children.Count);
			int rows = (node.Children.Count + columns - 1) / columns;
			float top = y - settings.vSpacing;
			for (int i = 0; i < node.Children.Count; i++) {
				int column = i % columns, row = i / columns;
				var child = node.Children[i];
				child.InGrid = true;
				child.Position = new Vector2(cx + (column - (columns - 1) * 0.5f) * settings.gridHSpacing, top - row * settings.gridVSpacing);
			}
			tree.Groups.Add(new Group {
				Parent = node,
				Center = new Vector2(cx, top - (rows - 1) * settings.gridVSpacing * 0.5f),
				Size = new Vector2(columns * settings.gridHSpacing + 2f * settings.groupPadding, rows * settings.gridVSpacing + 2f * settings.groupPadding),
			});
			return;
		}

		float childrenWidth = 0f;
		foreach (var child in node.Children)
			childrenWidth += widths[child];
		childrenWidth += (node.Children.Count - 1) * settings.blockGap;

		float x = cx - childrenWidth * 0.5f;
		foreach (var child in node.Children) {
			float width = widths[child];
			Place(child, x + width * 0.5f, y - settings.vSpacing, settings, widths, tree);
			x += width + settings.blockGap;
		}
	}
}
