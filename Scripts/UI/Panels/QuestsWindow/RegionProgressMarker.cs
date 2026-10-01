using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Засечка на прогресс-баре зоны: «на этом проценте открываются апгрейды уровня».
// Стоит по anchor.x = процент, засечка уходит вниз на сам бар, иконка — над ним; несколько апгрейдов
// на одном пороге — одна засечка со счётчиком ×N. Достигнутая — золотая, остальные приглушены.
public class RegionProgressMarker : MonoBehaviour {
	[SerializeField] Image notch;
	[SerializeField] Image iconBack;
	[SerializeField] Image icon;
	[SerializeField] TMP_Text count;
	[SerializeField] Color reachedColor = new Color(0.8745f, 0.7176f, 0.2078f, 1f);
	[SerializeField] Color pendingColor = new Color(0.55f, 0.58f, 0.64f, 1f);
	[SerializeField] Color reachedIconColor = new Color(0.3412f, 0.2824f, 0.0784f, 1f);
	[SerializeField] Color pendingIconColor = new Color(0.17f, 0.19f, 0.235f, 1f);

	public int Percent { get; private set; }

	public void Set(int percent, int upgradesCount, bool reached) {
		Percent = percent;
		var rect = (RectTransform)transform;
		float x = Mathf.Clamp01(percent / 100f);
		rect.anchorMin = new Vector2(x, rect.anchorMin.y);
		rect.anchorMax = new Vector2(x, rect.anchorMax.y);
		rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);

		var color = reached ? reachedColor : pendingColor;
		if (notch != null)
			notch.color = color;
		if (iconBack != null)
			iconBack.color = color;
		if (icon != null)
			icon.color = reached ? reachedIconColor : pendingIconColor;
		if (count != null) {
			count.gameObject.SetActive(upgradesCount > 1);
			count.text = "×" + upgradesCount;
			count.color = color;
		}
	}
}
