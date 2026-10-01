using Gameplay.Tutorial;
using UnityEngine;

namespace UI.Tutorial {
	// Туториальная реплика над объектом мира. Проекция цели и зона — из базового
	// WorldTrackingPanel (та же, что у панелек тасков в хабе), но кламп свой:
	// у тасков в зону зажимается точка-иконка, а здесь — весь рект пузыря, иначе
	// у края экрана половина реплики уезжает за кадр. Цель вне зоны — включается
	// стрелка на кромке пузыря в сторону цели.
	public class TutorialHintPanel : WorldTrackingPanel {
		[SerializeField] TutorialHintView view;
		[Tooltip("Сдвиг пузыря от цели в пикселях референсного разрешения.")]
		[SerializeField] Vector2 screenOffset = new Vector2(0f, 120f);
		[Tooltip("Поле между пузырём и краем зоны — в нём живёт стрелка, когда цель за кадром.")]
		[SerializeField] float edgeMargin = 64f;
		[Tooltip("Зазор между кромкой пузыря и центром стрелки.")]
		[SerializeField] float arrowGap = 26f;

		public void Setup(TutorialStep step, System.Action onOk) {
			screenOffset = step.Offset;
			// Пивот пузыря: какая точка Bubble встаёт в спроецированную цель+сдвиг
			// (например (1,0) — пузырь растёт влево-вверх от цели у правого края экрана)
			((RectTransform)view.transform).pivot = step.BubblePivot;
			view.Setup(step, onOk);
		}

		protected override void UpdatePosition() {
			// Неактивная цель (например, маркер на выключенной глобальной карте) —
			// пузырь прячется и ждёт её возвращения, не дёргая трекинг
			bool targetVisible = trackTransform == null || trackTransform.gameObject.activeInHierarchy;
			if (view.gameObject.activeSelf != targetVisible)
				view.gameObject.SetActive(targetVisible);
			if (!targetVisible || trackTransform == null) {
				SetArrowActive(false);
				return;
			}

			Vector2 targetViewportPos = WorldToScreenPointProjected(c, trackTransform.position);
			Vector2 targetPos = (targetViewportPos * screenSize) - parentOffsetMin;

			// Пивот Bubble стоит в позиции корня, так что рект пузыря в пространстве
			// родителя = [pos - pivot*size, pos + (1-pivot)*size] — его и зажимаем в зону
			var bubble = (RectTransform)view.transform;
			Vector2 size = bubble.rect.size;
			Vector2 pivot = bubble.pivot;
			Vector2 min = panelRect.min + Vector2.one * edgeMargin + Vector2.Scale(pivot, size);
			Vector2 max = panelRect.max - Vector2.one * edgeMargin - Vector2.Scale(Vector2.one - pivot, size);
			Vector2 pos = targetPos + screenOffset;
			// Зона уже пузыря (узкий экран) — встаём по центру оси
			pos.x = min.x <= max.x ? Mathf.Clamp(pos.x, min.x, max.x) : (min.x + max.x) * 0.5f;
			pos.y = min.y <= max.y ? Mathf.Clamp(pos.y, min.y, max.y) : (min.y + max.y) * 0.5f;
			((RectTransform)transform).anchoredPosition = pos;

			bool offscreen = !panelRect.Contains(targetPos);
			SetArrowActive(offscreen);
			if (!offscreen)
				return;

			// Стрелка — на луче из центра пузыря к цели, сразу за кромкой его ректа
			Vector2 centerLocal = Vector2.Scale(new Vector2(0.5f, 0.5f) - pivot, size);
			Vector2 toTarget = targetPos - (pos + centerLocal);
			if (toTarget.sqrMagnitude < 0.0001f)
				return;
			Vector2 dir = toTarget.normalized;
			float exitX = Mathf.Abs(dir.x) > 0.0001f ? size.x * 0.5f / Mathf.Abs(dir.x) : float.MaxValue;
			float exitY = Mathf.Abs(dir.y) > 0.0001f ? size.y * 0.5f / Mathf.Abs(dir.y) : float.MaxValue;
			((RectTransform)directionArrow).anchoredPosition = centerLocal + dir * (Mathf.Min(exitX, exitY) + arrowGap);
			directionArrow.up = dir;
		}

		private void SetArrowActive(bool active) {
			if (directionArrow != null && directionArrow.gameObject.activeSelf != active)
				directionArrow.gameObject.SetActive(active);
		}
	}
}
