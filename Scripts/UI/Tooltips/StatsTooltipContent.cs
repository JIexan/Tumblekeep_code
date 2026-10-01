using TMPro;
using UnityEngine;

namespace UI.Tooltips {
	// Контент «заголовок + описание + параметры» (строки «подпись — значение», как в книге).
	// Инстанс переиспользуется между показами: колбек конфигурации начинает с Set (он же
	// сбрасывает строки), затем добавляет параметры через AddRow.
	public class StatsTooltipContent : TooltipContent {
		[SerializeField] TMP_Text titleText;
		[SerializeField] TMP_Text bodyText;
		[SerializeField] Transform rowsRoot;
		[Tooltip("Неактивный шаблон строки внутри rowsRoot.")]
		[SerializeField] BookStatRow rowTemplate;

		private BookRowPool<BookStatRow> rows;

		public void Set(string title, string text) {
			titleText.text = title;
			bodyText.text = text;
			bodyText.gameObject.SetActive(!string.IsNullOrEmpty(text));
			rows ??= new BookRowPool<BookStatRow>(rowTemplate, rowsRoot);
			rows.Begin();
		}

		public void AddRow(string label, string value) {
			rows ??= new BookRowPool<BookStatRow>(rowTemplate, rowsRoot);
			rows.Next().Set(label, value);
		}

		public override void Rebuild() {
			if (rows != null)
				rows.End();
			rowsRoot.gameObject.SetActive(rows != null && rows.Used > 0);
			base.Rebuild();
		}
	}
}
