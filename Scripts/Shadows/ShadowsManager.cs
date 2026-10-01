using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowsManager : Singleton<ShadowsManager>
{
	public GameObject shadowPrefab;
	public Color shadowColor;

	private class ShadowData {
		public Transform trackingObject;
		public Transform shadowObject;

		public float minSize;
		public float maxSize;

		public float minY;
		public float maxY;
	}

	private float scale;
	private Vector3 trackingPos;

	private List<ShadowData> shadows = new List<ShadowData>();

	public void AddShadow(Transform trackingObject, float minSize, float maxSize, float minY, float maxY) {
		ShadowData newShadow = new ShadowData();
		newShadow.trackingObject = trackingObject;
		newShadow.shadowObject = Instantiate(shadowPrefab, transform).transform;
		newShadow.shadowObject.gameObject.layer = gameObject.layer;
		SpriteRenderer r = newShadow.shadowObject.GetComponent<SpriteRenderer>();
		if(r != null) {
			r.color = shadowColor;
		}
		newShadow.minSize = minSize;
		newShadow.maxSize = maxSize;
		newShadow.minY = minY;
		newShadow.maxY = maxY;

		shadows.Add(newShadow);
	}

    // Update is called once per frame
    void Update()
    {
		for(int i = shadows.Count - 1; i >= 0; i--) {
			if(shadows[i].trackingObject == null || !shadows[i].trackingObject.gameObject.activeInHierarchy) {
				Destroy(shadows[i].shadowObject.gameObject);
				shadows.RemoveAt(i);
				continue;
			}

			Vector3 trackingPos = shadows[i].trackingObject.position;

			if(trackingPos.y < shadows[i].minY) {
				scale = Mathf.Lerp(0f, shadows[i].minSize, trackingPos.y / shadows[i].minY);
			} else {
				scale = Mathf.Lerp(shadows[i].minSize, shadows[i].maxSize, Mathf.InverseLerp(shadows[i].minY, shadows[i].maxY, trackingPos.y));
			}
			shadows[i].shadowObject.localScale = new Vector3(scale, scale, 1f);

			trackingPos.y = 0f;
			shadows[i].shadowObject.localPosition = trackingPos;
			
		}
    }
}
