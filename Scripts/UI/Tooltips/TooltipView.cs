using UnityEngine;

namespace UI.Tooltips {
	// Пузырь подсказки: рамка по размеру контента + стрелка на кромке, смотрящая в якорь.
	// Вся геометрия — в единицах канваса с началом в его левом нижнем углу: пузырь и
	// стрелка заякорены в (0;0), поэтому anchoredPosition = координата в этой системе.
	public class TooltipView : MonoBehaviour {
		[SerializeField] RectTransform bubble;
		[Tooltip("Спрайт в нулевом повороте смотрит вниз, пивот — середина основания (0.5; 1).")]
		[SerializeField] RectTransform arrow;
		[SerializeField] CanvasGroup group;

		[Header("Вёрстка")]
		[Tooltip("Поля между рамкой и контентом: по горизонтали / по вертикали.")]
		[SerializeField] Vector2 padding = new Vector2(16f, 12f);
		[Tooltip("Зазор между остриём стрелки и якорем.")]
		[SerializeField] float anchorGap = 4f;
		[Tooltip("На сколько основание стрелки заходит под рамку (у спрайта рамки прозрачная кромка).")]
		[SerializeField] float arrowOverlap = 3f;
		[Tooltip("Стрелка не подходит к углу пузыря ближе этого — там скругление рамки.")]
		[SerializeField] float arrowCornerInset = 14f;
		[Tooltip("Минимальный отступ пузыря от края экрана.")]
		[SerializeField] float screenMargin = 12f;

		public bool IsActive => bubble.gameObject.activeSelf;

		public void SetActive(bool active) {
			if (bubble.gameObject.activeSelf != active)
				bubble.gameObject.SetActive(active);
		}

		public void SetAlpha(float alpha) {
			group.alpha = alpha;
		}

		// Контент живёт внутри пузыря от его левого верхнего угла — собственные якоря префаба не важны
		public void Attach(TooltipContent content) {
			var rect = content.Rect;
			rect.SetParent(bubble, false);
			rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
			rect.anchoredPosition = new Vector2(padding.x, -padding.y);
		}

		public void Fit(TooltipContent content) {
			bubble.sizeDelta = content.Rect.rect.size + padding * 2f;
		}

		// anchorPos — якорь для предпочтительной стороны, flippedAnchorPos — для противоположной
		// (зеркальная точка target: перевёрнутый пузырь не должен накрывать сам элемент)
		public void Place(Vector2 anchorPos, Vector2 flippedAnchorPos, Vector2 canvasSize, TooltipDirection preferred) {
			Vector2 size = bubble.rect.size;
			float reach = arrow.rect.height - arrowOverlap + anchorGap;
			Vector2 areaMin = new Vector2(screenMargin, screenMargin);
			Vector2 areaMax = canvasSize - areaMin;

			// Основная ось: не влезли в предпочтительную сторону — переворачиваемся
			var direction = ResolveDirection(preferred, anchorPos, flippedAnchorPos, size, reach, areaMin, areaMax);
			Vector2 anchor = direction == preferred ? anchorPos : flippedAnchorPos;

			Vector2 min;
			switch (direction) {
				case TooltipDirection.Up: min = new Vector2(anchor.x - size.x * 0.5f, anchor.y + reach); break;
				case TooltipDirection.Down: min = new Vector2(anchor.x - size.x * 0.5f, anchor.y - reach - size.y); break;
				case TooltipDirection.Left: min = new Vector2(anchor.x - reach - size.x, anchor.y - size.y * 0.5f); break;
				default: min = new Vector2(anchor.x + reach, anchor.y - size.y * 0.5f); break;
			}

			// Поперечная ось: сдвигаемся ровно настолько, чтобы влезть. По основной зажимаем
			// тоже — на случай, когда пузырь не помещается ни с одной стороны от якоря
			Vector2 clamped = new Vector2(
				ClampAxis(min.x, size.x, areaMin.x, areaMax.x),
				ClampAxis(min.y, size.y, areaMin.y, areaMax.y));
			bool vertical = TooltipPlacement.IsVertical(direction);
			bool pushedOntoAnchor = vertical ? !Mathf.Approximately(clamped.y, min.y) : !Mathf.Approximately(clamped.x, min.x);
			min = new Vector2(Mathf.Round(clamped.x), Mathf.Round(clamped.y));
			bubble.anchoredPosition = min;

			PlaceArrow(direction, anchor - min, size, !pushedOntoAnchor);
		}

		private static TooltipDirection ResolveDirection(TooltipDirection preferred, Vector2 anchorPos, Vector2 flippedAnchorPos,
			Vector2 size, float reach, Vector2 areaMin, Vector2 areaMax) {
			float need = (TooltipPlacement.IsVertical(preferred) ? size.y : size.x) + reach;
			float preferredSpace = GetSpace(preferred, anchorPos, areaMin, areaMax);
			if (need <= preferredSpace)
				return preferred;
			var opposite = TooltipPlacement.Opposite(preferred);
			float oppositeSpace = GetSpace(opposite, flippedAnchorPos, areaMin, areaMax);
			// Не влезает никуда — берём сторону, где места больше
			return need <= oppositeSpace || oppositeSpace > preferredSpace ? opposite : preferred;
		}

		private static float GetSpace(TooltipDirection direction, Vector2 anchor, Vector2 areaMin, Vector2 areaMax) {
			switch (direction) {
				case TooltipDirection.Up: return areaMax.y - anchor.y;
				case TooltipDirection.Down: return anchor.y - areaMin.y;
				case TooltipDirection.Left: return anchor.x - areaMin.x;
				default: return areaMax.x - anchor.x;
			}
		}

		private static float ClampAxis(float min, float size, float areaMin, float areaMax) {
			// Пузырь шире зоны (узкий экран) — встаём по центру оси
			if (size > areaMax - areaMin)
				return (areaMin + areaMax - size) * 0.5f;
			return Mathf.Clamp(min, areaMin, areaMax - size);
		}

		// anchorLocal — якорь относительно левого нижнего угла пузыря. Стрелка едет вдоль
		// кромки за якорем, пока не упрётся в скругление угла.
		private void PlaceArrow(TooltipDirection direction, Vector2 anchorLocal, Vector2 size, bool visible) {
			if (arrow.gameObject.activeSelf != visible)
				arrow.gameObject.SetActive(visible);
			if (!visible)
				return;

			float inset = arrowCornerInset + arrow.rect.width * 0.5f;
			float alongX = ClampAlong(anchorLocal.x, inset, size.x);
			float alongY = ClampAlong(anchorLocal.y, inset, size.y);
			Vector2 position;
			float angle;
			switch (direction) {
				case TooltipDirection.Up: position = new Vector2(alongX, arrowOverlap); angle = 0f; break;
				case TooltipDirection.Down: position = new Vector2(alongX, size.y - arrowOverlap); angle = 180f; break;
				case TooltipDirection.Left: position = new Vector2(size.x - arrowOverlap, alongY); angle = 90f; break;
				default: position = new Vector2(arrowOverlap, alongY); angle = -90f; break;
			}
			arrow.anchoredPosition = position;
			arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
		}

		private static float ClampAlong(float value, float inset, float length) {
			return inset * 2f <= length ? Mathf.Clamp(value, inset, length - inset) : length * 0.5f;
		}
	}
}
