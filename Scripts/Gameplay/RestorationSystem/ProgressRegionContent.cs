using Gameplay.RestorationSystem;
using System.Collections.Generic;
using UnityEngine;

// Lives on the root of the in-progress region prefab. Exposes the ordered list of
// steps so the runtime ProgressRegion can drive them after the prefab is instantiated.
public class ProgressRegionContent : MonoBehaviour {
	[SerializeField] List<ProgressStep> steps = new();

	public List<ProgressStep> Steps => steps;
}
