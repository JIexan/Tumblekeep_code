using UnityEngine;
using UnityEngine.UI;

// Ребро между узлами дерева апгрейдов: один растянутый и повёрнутый Image.
// Pivot (0, 0.5), якоря совпадают с якорями узлов (top-center контейнера).
public class UpgradeEdgeView : MonoBehaviour {
	[SerializeField] Image line;
	[SerializeField] float dimmedAlpha = 0.35f;

	public void Init(Vector2 from, Vector2 to, float thickness) {
		var rectTransform = (RectTransform)transform;
		var delta = to - from;
		rectTransform.anchoredPosition = from;
		rectTransform.sizeDelta = new Vector2(delta.magnitude, thickness);
		rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
	}

	// Ребро яркое, когда его узел-источник (пререквизит) куплен — «путь открыт».
	public void SetState(bool prereqPurchased) {
		if (line == null)
			return;
		var color = line.color;
		color.a = prereqPurchased ? 1f : dimmedAlpha;
		line.color = color;
	}
}
