using TMPro;
using UnityEngine;

namespace UI.Tooltips {
	// Контент «просто текст»
	public class TextTooltipContent : TooltipContent {
		[SerializeField] TMP_Text text;

		public void Set(string value) {
			text.text = value;
		}
	}
}
