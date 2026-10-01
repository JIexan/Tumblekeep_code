using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Одна ячейка ленты рулетки сундука (см. ChestRouletteStrip): рамка (у реликвий тинтится
// цветом рарности), иконка и счётчик количества. Шаблон ячейки лежит выключенным внутри
// Content ленты и клонируется в рантайме.
public class ChestRouletteCell : MonoBehaviour {
	[SerializeField] Image frame;
	[SerializeField] Image icon;
	[SerializeField] TMP_Text countText;
	[SerializeField] CanvasGroup canvasGroup;

	public RectTransform Rect => (RectTransform)transform;
	public ChestRouletteEntry Entry { get; private set; }

	public void Set(ChestRouletteEntry entry) {
		Entry = entry;
		icon.sprite = entry.Icon;
		icon.enabled = entry.Icon != null;
		frame.color = entry.FrameColor;
		// Единичное количество не подписываем: «×1» только шумит (спеллы почти всегда по одному)
		bool showCount = entry.Count > 1;
		countText.gameObject.SetActive(showCount);
		if (showCount)
			countText.text = "×" + entry.Count;
	}

	public void SetAlpha(float alpha) {
		if (canvasGroup != null)
			canvasGroup.alpha = alpha;
	}
}

// Что показывает ячейка. Строится окном из ChestItemDrop / RelicDefinition / ChestReward,
// ключи локализации нужны только победителю (название + описание под лентой).
public class ChestRouletteEntry {
	public Sprite Icon;
	public int Count;
	public Color FrameColor = Color.white;
	public string NameKey;
	public string DescKey;
	// Источник записи (ChestItemDrop / RelicDefinition) — чтобы лента не ставила
	// одну и ту же награду в соседние ячейки
	public object Source;
}
