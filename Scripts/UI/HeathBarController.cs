using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeathBarController : MonoBehaviour
{
	private struct HeathBarData {
		public HeathBar heathBar;
		public Unit u;

		public HeathBarData(HeathBar heathBar, Unit u) {
			this.heathBar = heathBar;
			this.heathBar.gameObject.SetActive(false);
			this.u = u;
		}
	}

	public RectTransform canvas;
	public HeathBar heathBarPrefab;

	private List<HeathBarData> bars = new List<HeathBarData>();
	private struct DistractionBarData {
		public HeathBar bar;
		public PlayerDistraction source;
	}
	private readonly List<DistractionBarData> distractionBars = new List<DistractionBarData>();
	private Camera cam;
	
	void Start() {
		cam = Camera.main;
	}

	public void AddHeathBar(Unit u) {
		HeathBar bar = Instantiate<HeathBar>(heathBarPrefab, transform);
		bar.Init(u);
		bars.Add(new HeathBarData(bar, u));
	}

	public void AddDistractionBar(PlayerDistraction source) {
		HeathBar bar = Instantiate(heathBarPrefab, transform);
		bar.gameObject.SetActive(false);
		distractionBars.Add(new DistractionBarData { bar = bar, source = source });
	}

    // Update is called once per frame
    void Update()
    {
		for(int i = bars.Count - 1; i >= 0; i--) {
			HeathBarData b = bars[i];
			if (b.u != null && !b.u.isDead) {
				if (b.u.Damaged) {
					if (!b.heathBar.gameObject.activeSelf) {
						b.heathBar.gameObject.SetActive(true);
					}
					Vector3 target = b.u.transform.position + b.u.healthBarOffset;
					Vector3 viewportPosition = cam.WorldToViewportPoint(target);
					Vector3 canvasPosition = Vector3.Scale(viewportPosition, canvas.sizeDelta);
					(b.heathBar.transform as RectTransform).anchoredPosition = canvasPosition;
					b.heathBar.UpdateHeath(b.u.Heath);


				} else if (b.heathBar.gameObject.activeSelf) {
					b.heathBar.gameObject.SetActive(false);
				}
			} else {
				Destroy(b.heathBar.gameObject);
				bars.Remove(b);
			}
		}
		UpdateDistractionBars();
    }

	private void UpdateDistractionBars() {
		for (int i = distractionBars.Count - 1; i >= 0; i--) {
			var data = distractionBars[i];
			if (data.source == null) {
				if (data.bar != null)
					Destroy(data.bar.gameObject);
				distractionBars.RemoveAt(i);
				continue;
			}
			Vector3 viewport = cam != null ? cam.WorldToViewportPoint(data.source.HealthBarPosition) : Vector3.zero;
			bool visible = data.source.isActiveAndEnabled && !data.source.IsDepleted && viewport.z > 0f;
			data.bar.gameObject.SetActive(visible);
			if (!visible)
				continue;
			(data.bar.transform as RectTransform).anchoredPosition = Vector3.Scale(viewport, canvas.sizeDelta);
			data.bar.UpdateHeath(data.source.NormalizedHP);
		}
	}
}
