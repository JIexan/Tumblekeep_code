using System;
using Assets.SimpleLocalization;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Tooltips {
	// Показывает подсказку, пока курсор над элементом (нужен raycast-таргет на нём или детях).
	// Контент — либо ключи локализации из инспектора, либо задаётся из кода через Set*;
	// строки и колбек вычисляются в момент показа, так что смена языка подхватывается сама.
	public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
		[Tooltip("Рект, к которому привязана подсказка. Пусто — собственный рект.")]
		[SerializeField] RectTransform target;
		[Tooltip("Вести подсказку за курсором вместо привязки к ректу.")]
		[SerializeField] bool followCursor;
		[Tooltip("Точка привязки внутри ректа: (0;0) — левый нижний угол, (1;1) — правый верхний.")]
		[SerializeField] Vector2 anchor = new Vector2(0.5f, 1f);
		[SerializeField] TooltipDirection direction = TooltipDirection.Up;

		[Header("Контент из инспектора (код может заменить через Set*)")]
		[Tooltip("Ключ локализации заголовка. Пусто — подсказка без заголовка.")]
		[SerializeField] string titleKey;
		[SerializeField] string textKey;

		private Action<TooltipPlacement> showRequest;
		private bool hovered;
		private bool suppressed;

		public void SetText(string text) {
			SetText(() => text);
		}

		public void SetText(Func<string> text) {
			SetRequest(placement => TooltipManager.ShowText(this, text(), placement));
		}

		public void SetTitled(string title, string text) {
			SetTitled(() => title, () => text);
		}

		public void SetTitled(Func<string> title, Func<string> text) {
			SetRequest(placement => TooltipManager.ShowTitled(this, title(), text(), placement));
		}

		// Стандартный контент типа T из списка менеджера
		public void SetContent<T>(Action<T> configure) where T : TooltipContent {
			SetRequest(placement => TooltipManager.Show(this, configure, placement));
		}

		// Свой префаб контента
		public void SetContent<T>(T contentPrefab, Action<T> configure) where T : TooltipContent {
			SetRequest(placement => TooltipManager.Show(this, contentPrefab, configure, placement));
		}

		// Вернуться к контенту из инспектора (если ключей нет — подсказка не показывается)
		public void ClearContent() {
			SetRequest(null);
		}

		// Спрятать до следующего наведения: владелец зовёт на начале драга/клика,
		// когда курсор остаётся над элементом, а подсказка уже мешает
		public void Hide() {
			suppressed = true;
			TooltipManager.Hide(this);
		}

		public void OnPointerEnter(PointerEventData eventData) {
			hovered = true;
			// Курсор тащит что-то через элемент (свиток над соседними слотами) — не всплываем
			suppressed = eventData.dragging;
			Show();
		}

		public void OnPointerExit(PointerEventData eventData) {
			hovered = false;
			TooltipManager.Hide(this);
		}

		// Элемент выключили/уничтожили под курсором — PointerExit не придёт
		private void OnDisable() {
			hovered = false;
			TooltipManager.Hide(this);
		}

		private void SetRequest(Action<TooltipPlacement> request) {
			showRequest = request;
			// Контент сменили под курсором — перепоказываем
			if (hovered)
				Show();
		}

		private void Show() {
			if (suppressed)
				return;

			var placement = followCursor
				? TooltipPlacement.AtCursor(direction)
				: TooltipPlacement.At(target != null ? target : transform as RectTransform, anchor, direction);

			if (showRequest != null) {
				showRequest(placement);
			} else if (!string.IsNullOrEmpty(textKey)) {
				if (string.IsNullOrEmpty(titleKey))
					TooltipManager.ShowText(this, LocalizationManager.Localize(textKey), placement);
				else
					TooltipManager.ShowTitled(this, LocalizationManager.Localize(titleKey), LocalizationManager.Localize(textKey), placement);
			} else {
				TooltipManager.Hide(this);
			}
		}
	}
}
