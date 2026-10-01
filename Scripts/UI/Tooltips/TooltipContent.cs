using UnityEngine;
using UnityEngine.UI;

namespace UI.Tooltips {
	// База контента подсказки. Контент — префаб с наследником этого класса на корне;
	// менеджер держит по одному инстансу на префаб и перед показом отдаёт его в колбек
	// конфигурации. Вёрстка корня — LayoutGroup + ContentSizeFitter (vertical = Preferred):
	// ширину подбирает Rebuild, высоту — фиттер.
	public abstract class TooltipContent : MonoBehaviour {
		[Tooltip("Предел ширины контента: до него текст идёт в одну строку, дальше переносится.")]
		[SerializeField] float maxWidth = 360f;

		public RectTransform Rect => (RectTransform)transform;

		// Зовётся менеджером сразу после колбека конфигурации: к возврату рект контента
		// обязан иметь итоговый размер — по нему строится пузырь
		public virtual void Rebuild() {
			var rect = Rect;
			// Первый проход считает preferred-ширину лэйаута (TMP отдаёт её без переносов),
			// второй — раскладывает уже в выбранной ширине и даёт высоту с переносами
			LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
			// +1 — страховка от переноса последнего слова на погрешности float
			float width = Mathf.Min(Mathf.Ceil(LayoutUtility.GetPreferredWidth(rect)) + 1f, maxWidth);
			rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
			LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
		}
	}
}
