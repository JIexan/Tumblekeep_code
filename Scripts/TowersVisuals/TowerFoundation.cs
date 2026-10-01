using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TraitsDictionary : SerializableDictionary<TowerTraitType, GameObject> { } 

public class TowerFoundation: MonoBehaviour
{
	[SerializeField] Transform infoRoot;
	[SerializeField] TraitsDictionary traitsVisuals;
	[SerializeField] Transform weaponRoot;

	public Vector3 WeaponOffset => weaponRoot != null ? weaponRoot.localPosition : Vector3.zero;

	public void Init(TowerInfoCanvas canvas, List<AbstractTrait> traits) {
		canvas.transform.position = infoRoot.position;

		foreach(var visual in traitsVisuals) {
			visual.Value.gameObject.SetActive(false);
		}

		for (int i = 0; i < traits.Count; i++) {
			if (traitsVisuals.ContainsKey(traits[i].Type)) {
				traitsVisuals[traits[i].Type].SetActive(true);
			}
		}
	}
}
