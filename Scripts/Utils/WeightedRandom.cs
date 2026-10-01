using System;
using System.Collections.Generic;

// Общий взвешенный случайный выбор (cumulative sum, как ItemSettings.SelectWeightedRandom).
// Веса <= 0 в розыгрыше не участвуют; при нулевой сумме — -1 / fallback.
public static class WeightedRandom {
	public static int PickIndex<T>(IReadOnlyList<T> items, Func<T, float> weightOf) {
		if (items == null || items.Count == 0)
			return -1;

		float total = 0f;
		for (int i = 0; i < items.Count; i++) {
			float w = weightOf(items[i]);
			if (w > 0f)
				total += w;
		}
		if (total <= 0f)
			return -1;

		float roll = UnityEngine.Random.Range(0f, total);
		float acc = 0f;
		int last = -1;
		for (int i = 0; i < items.Count; i++) {
			float w = weightOf(items[i]);
			if (w <= 0f)
				continue;
			acc += w;
			last = i;
			if (roll <= acc)
				return i;
		}
		return last;
	}

	public static T Pick<T>(IReadOnlyList<T> items, Func<T, float> weightOf, T fallback = default) {
		int index = PickIndex(items, weightOf);
		return index >= 0 ? items[index] : fallback;
	}
}
