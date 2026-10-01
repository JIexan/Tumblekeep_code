using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.RestorationSystem {
	// Pure graph math over ProgressStep.Prerequisites. Shared by runtime (restore order,
	// frontier) and editor (preview flattening, cycle checks). Defensive by design:
	// null registry entries, null/duplicate/foreign prerequisites and even cycles must
	// never soft-lock the player — broken content is the editor validation's job to surface.
	public static class ProgressGraphUtility {

		// Kahn's algorithm. Ready steps are picked by ascending registry index, so the
		// result is deterministic for a given registry order. Steps stuck in a cycle are
		// appended at the end in registry order (with an error logged).
		public static List<ProgressStep> TopologicalOrder(IReadOnlyList<ProgressStep> registry) {
			var order = TopologicalOrderInternal(registry, out bool hadCycle);
			if (hadCycle)
				Debug.LogError("[ProgressGraph] Цикл в графе шагов — часть шагов применена вне порядка зависимостей.");
			return order;
		}

		public static bool HasCycle(IReadOnlyList<ProgressStep> registry) {
			TopologicalOrderInternal(registry, out bool hadCycle);
			return hadCycle;
		}

		// Steps that are not completed but have every prerequisite completed.
		// Prerequisites that are null or missing from the registry count as satisfied.
		public static List<ProgressStep> GetFrontier(IReadOnlyList<ProgressStep> registry,
		                                             Func<ProgressStep, bool> isCompleted) {
			var frontier = new List<ProgressStep>();
			if (registry == null)
				return frontier;

			var known = new HashSet<ProgressStep>();
			foreach (var step in registry) {
				if (step != null)
					known.Add(step);
			}

			var seen = new HashSet<ProgressStep>();
			foreach (var step in registry) {
				if (step == null || !seen.Add(step) || isCompleted(step))
					continue;

				bool ready = true;
				var prereqs = step.Prerequisites;
				if (prereqs != null) {
					foreach (var prereq in prereqs) {
						if (prereq == null || !known.Contains(prereq))
							continue;
						if (!isCompleted(prereq)) {
							ready = false;
							break;
						}
					}
				}
				if (ready)
					frontier.Add(step);
			}
			return frontier;
		}

		// True if step transitively depends on candidate through prerequisites.
		// Used by the editor to refuse edges that would close a cycle.
		public static bool DependsOn(ProgressStep step, ProgressStep candidate) {
			if (step == null || candidate == null)
				return false;

			var visited = new HashSet<ProgressStep>();
			var stack = new Stack<ProgressStep>();
			stack.Push(step);
			while (stack.Count > 0) {
				var current = stack.Pop();
				if (!visited.Add(current) || current.Prerequisites == null)
					continue;
				foreach (var prereq in current.Prerequisites) {
					if (prereq == null)
						continue;
					if (prereq == candidate)
						return true;
					stack.Push(prereq);
				}
			}
			return false;
		}

		private static List<ProgressStep> TopologicalOrderInternal(IReadOnlyList<ProgressStep> registry, out bool hadCycle) {
			hadCycle = false;
			var order = new List<ProgressStep>();
			if (registry == null)
				return order;

			// Deduplicated node list preserving registry order.
			var nodes = new List<ProgressStep>();
			var indexOf = new Dictionary<ProgressStep, int>();
			foreach (var step in registry) {
				if (step == null || indexOf.ContainsKey(step))
					continue;
				indexOf.Add(step, nodes.Count);
				nodes.Add(step);
			}

			// In-degrees and dependent lists; only edges between known nodes count.
			var remaining = new int[nodes.Count];
			var dependents = new List<int>[nodes.Count];
			for (int i = 0; i < nodes.Count; i++) {
				var prereqs = nodes[i].Prerequisites;
				if (prereqs == null)
					continue;
				var counted = new HashSet<int>();
				foreach (var prereq in prereqs) {
					if (prereq == null || prereq == nodes[i] || !indexOf.TryGetValue(prereq, out int p) || !counted.Add(p))
						continue;
					remaining[i]++;
					(dependents[p] ??= new List<int>()).Add(i);
				}
			}

			var ready = new SortedSet<int>();
			for (int i = 0; i < nodes.Count; i++) {
				if (remaining[i] == 0)
					ready.Add(i);
			}

			var placed = new bool[nodes.Count];
			while (ready.Count > 0) {
				int i = ready.Min;
				ready.Remove(i);
				placed[i] = true;
				order.Add(nodes[i]);
				if (dependents[i] == null)
					continue;
				foreach (int dep in dependents[i]) {
					if (--remaining[dep] == 0)
						ready.Add(dep);
				}
			}

			if (order.Count < nodes.Count) {
				hadCycle = true;
				for (int i = 0; i < nodes.Count; i++) {
					if (!placed[i])
						order.Add(nodes[i]);
				}
			}
			return order;
		}
	}
}
