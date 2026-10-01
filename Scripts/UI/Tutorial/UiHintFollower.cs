using Gameplay.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Tutorial {
	// Туториальная реплика, висящая над UI-элементом. Цель ищется каждый кадр
	// в реестре TutorialUiAnchor по id: элемент может пересоздаваться/скрываться —
	// пока цели нет, пузырь спрятан. У шага с пустым uiAnchorId цель — рантайм-рект
	// от условия показа (динамические элементы вроде строки свитка в HUD, которым
	// id в префабе не раздать); уничтожили/выключили его — пузырь так же прячется.
	// Цель внутри ScrollRect: при появлении скролл подкручивается, чтобы она была видна,
	// а если игрок увёл её под маску — пузырь прячется (иначе висел бы над пустым местом).
	// Позиционирование через world-координаты (в Screen Space Overlay они совпадают
	// с экранными) — работает и когда цель в другом канвасе; offset задаётся
	// в пикселях референсного разрешения.
	public class UiHintFollower : MonoBehaviour {
		[SerializeField] TutorialHintView view;
		[Tooltip("Минимальный зазор между пузырём и краем экрана, пиксели референсного разрешения.")]
		[SerializeField] float screenMargin = 16f;
		[Tooltip("Поле вокруг цели при автопрокрутке ScrollRect, единицы канваса.")]
		[SerializeField] float scrollPadding = 24f;

		private string anchorId;
		private RectTransform runtimeTarget;
		private Vector2 offset;

		// Цель, к которой уже подкрутили скролл: смена/возврат цели — повод подкрутить заново
		private RectTransform scrolledTarget;
		private ScrollRect targetScroll;

		private readonly Vector3[] corners = new Vector3[4];

		public void Init(TutorialStep step, RectTransform runtimeTarget, System.Action onOk) {
			anchorId = step.UiAnchorId;
			this.runtimeTarget = runtimeTarget;
			offset = step.Offset;
			// Пивот пузыря: какая точка Bubble встаёт в цель+сдвиг (например (1,0) —
			// пузырь растёт влево-вверх, чтобы не вылезать за экран у правого края)
			((RectTransform)view.transform).pivot = step.BubblePivot;
			view.Setup(step, onOk);
			UpdatePosition();
		}

		private void LateUpdate() {
			UpdatePosition();
		}

		private bool TryGetTarget(out RectTransform target) {
			if (string.IsNullOrEmpty(anchorId)) {
				target = runtimeTarget;
				return target != null;
			}
			return TutorialUiAnchor.TryGet(anchorId, out target);
		}

		private void UpdatePosition() {
			if (!TryGetTarget(out var target) || !target.gameObject.activeInHierarchy) {
				scrolledTarget = null;
				SetViewActive(false);
				return;
			}

			if (target != scrolledTarget) {
				scrolledTarget = target;
				targetScroll = target.GetComponentInParent<ScrollRect>();
				if (targetScroll != null)
					ScrollIntoView(targetScroll, target);
			}

			if (targetScroll != null && !IsInsideViewport(targetScroll, target)) {
				SetViewActive(false);
				return;
			}

			SetViewActive(true);

			var canvas = GetComponentInParent<Canvas>();
			float scale = canvas != null ? canvas.scaleFactor : 1f;
			Vector3 targetCenter = target.TransformPoint(target.rect.center);
			transform.position = targetCenter + (Vector3)(offset * scale);
			ClampToScreen(scale);
		}

		private void SetViewActive(bool active) {
			if (view.gameObject.activeSelf != active)
				view.gameObject.SetActive(active);
		}

		private static RectTransform GetViewport(ScrollRect scroll) {
			return scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
		}

		// Сдвигаем контент ровно настолько, чтобы рект цели (с полем) целиком попал во вьюпорт.
		// Раскладку форсим: вкладка могла включиться в этом же кадре и ещё не построиться.
		private void ScrollIntoView(ScrollRect scroll, RectTransform target) {
			if (scroll.content == null)
				return;
			Canvas.ForceUpdateCanvases();

			var viewport = GetViewport(scroll);
			Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
			Rect rect = viewport.rect;

			Vector2 delta = Vector2.zero;
			if (scroll.vertical)
				delta.y = AxisShift(bounds.min.y, bounds.max.y, rect.yMin, rect.yMax);
			if (scroll.horizontal)
				delta.x = AxisShift(bounds.min.x, bounds.max.x, rect.xMin, rect.xMax);
			if (delta == Vector2.zero)
				return;

			scroll.StopMovement();
			// delta — в пространстве вьюпорта; контент — его ребёнок без масштаба, единицы совпадают
			scroll.content.anchoredPosition += delta;
		}

		private float AxisShift(float min, float max, float viewMin, float viewMax) {
			// Цель больше вьюпорта — не дёргаемся
			if (max - min > viewMax - viewMin - scrollPadding * 2f)
				return 0f;
			if (min < viewMin + scrollPadding)
				return viewMin + scrollPadding - min;
			if (max > viewMax - scrollPadding)
				return viewMax - scrollPadding - max;
			return 0f;
		}

		private static bool IsInsideViewport(ScrollRect scroll, RectTransform target) {
			var viewport = GetViewport(scroll);
			Vector2 center = viewport.InverseTransformPoint(target.TransformPoint(target.rect.center));
			return viewport.rect.Contains(center);
		}

		// Пузырь растёт по контенту (приписка, кнопка) — у края экрана сдвигаем
		// его целиком внутрь. Экран уже пузыря — не дёргаемся, остаёмся у цели.
		private void ClampToScreen(float scale) {
			((RectTransform)view.transform).GetWorldCorners(corners);
			Vector2 min = corners[0];
			Vector2 max = corners[2];
			float margin = screenMargin * scale;

			Vector3 shift = Vector3.zero;
			if (max.x - min.x <= Screen.width - margin * 2f)
				shift.x = Mathf.Max(0f, margin - min.x) - Mathf.Max(0f, max.x - (Screen.width - margin));
			if (max.y - min.y <= Screen.height - margin * 2f)
				shift.y = Mathf.Max(0f, margin - min.y) - Mathf.Max(0f, max.y - (Screen.height - margin));
			transform.position += shift;
		}
	}
}
