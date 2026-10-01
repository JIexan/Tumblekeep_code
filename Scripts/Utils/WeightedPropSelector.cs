using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class WeightedPropSelector : MonoBehaviour
{
	[System.Serializable]
	public struct WeightedProp {
		public Mesh m;
		public float weight;
	}

	public MeshFilter filter;
	[SerializeField] List<WeightedProp> props;

    // Start is called before the first frame update
    void Awake()
    {
		int i = GetRandomWeightedIndex(props);
		//ноль значит ничего
		if (i < 0) {
			filter.gameObject.SetActive(false);
		} else {
			filter.mesh = props[i].m;
		}
	}

	private int GetRandomWeightedIndex(List<WeightedProp> weights) {
		if (weights == null || weights.Count == 0) return -1;

		float w;
		float t = 0;
		int i;
		for (i = 0; i < weights.Count; i++) {
			w = weights[i].weight;

			if (float.IsPositiveInfinity(w)) {
				return i;
			} else if (w >= 0f && !float.IsNaN(w)) {
				t += weights[i].weight;
			}
		}

		float r = Random.value;
		float s = 0f;

		for (i = 0; i < weights.Count; i++) {
			w = weights[i].weight;
			if (float.IsNaN(w) || w <= 0f) continue;

			s += w / t;
			if (s >= r) return i;
		}

		return -1;
	}
}
