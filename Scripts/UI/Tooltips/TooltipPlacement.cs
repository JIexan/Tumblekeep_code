using System;
using UnityEngine;

namespace UI.Tooltips {
	// Сторона, в которую пузырь раскрывается от якоря. Выбранная — предпочтительная:
	// не влезает в экран — пузырь переворачивается на противоположную по той же оси.
	public enum TooltipDirection {
		Up,
		Down,
		Left,
		Right,
	}

	// Куда и как вешать подсказку
	[Serializable]
	public struct TooltipPlacement {
		[Tooltip("Рект, к которому привязана подсказка. Пусто — подсказка идёт за курсором.")]
		public RectTransform target;
		[Tooltip("Точка привязки внутри target: (0;0) — левый нижний угол, (1;1) — правый верхний.")]
		public Vector2 anchor;
		public TooltipDirection direction;

		public static TooltipPlacement At(RectTransform target, Vector2 anchor, TooltipDirection direction = TooltipDirection.Up) {
			return new TooltipPlacement { target = target, anchor = anchor, direction = direction };
		}

		// Якорь — середина стороны target, от которой раскрывается пузырь
		public static TooltipPlacement At(RectTransform target, TooltipDirection direction = TooltipDirection.Up) {
			return At(target, DefaultAnchor(direction), direction);
		}

		public static TooltipPlacement AtCursor(TooltipDirection direction = TooltipDirection.Up) {
			return new TooltipPlacement { direction = direction };
		}

		public static Vector2 DefaultAnchor(TooltipDirection direction) {
			switch (direction) {
				case TooltipDirection.Up: return new Vector2(0.5f, 1f);
				case TooltipDirection.Down: return new Vector2(0.5f, 0f);
				case TooltipDirection.Left: return new Vector2(0f, 0.5f);
				default: return new Vector2(1f, 0.5f);
			}
		}

		public static bool IsVertical(TooltipDirection direction) {
			return direction == TooltipDirection.Up || direction == TooltipDirection.Down;
		}

		public static TooltipDirection Opposite(TooltipDirection direction) {
			switch (direction) {
				case TooltipDirection.Up: return TooltipDirection.Down;
				case TooltipDirection.Down: return TooltipDirection.Up;
				case TooltipDirection.Left: return TooltipDirection.Right;
				default: return TooltipDirection.Left;
			}
		}
	}
}
