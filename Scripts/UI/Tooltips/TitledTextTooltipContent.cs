using TMPro;
using UnityEngine;

namespace UI.Tooltips {
	// Контент «заголовок + текст»: заголовок крупнее, между ними полоска-разделитель
	public class TitledTextTooltipContent : TooltipContent {
		[SerializeField] TMP_Text titleText;
		[SerializeField] GameObject divider;
		[SerializeField] TMP_Text bodyText;

		public void Set(string title, string text) {
			titleText.text = title;
			bodyText.text = text;
			// Без текста остаётся один заголовок — разделителю нечего разделять
			bool hasBody = !string.IsNullOrEmpty(text);
			bodyText.gameObject.SetActive(hasBody);
			if (divider != null)
				divider.SetActive(hasBody);
		}
	}
}
