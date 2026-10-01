#if CHEATS
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cheats {
	/// <summary>
	/// Вспомогательный класс для обработки перетаскивания
	/// </summary>
	public class DragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler {
		private RectTransform windowRect;
		private Vector2 originalPointerPosition;
		private Vector3 originalWindowPosition;

		public void Initialize(RectTransform window) {
			windowRect = window;
		}

		public void OnBeginDrag(PointerEventData eventData) {
			originalPointerPosition = eventData.position;
			originalWindowPosition = windowRect.position;
		}

		public void OnDrag(PointerEventData eventData) {
			Vector2 delta = eventData.position - originalPointerPosition;
			windowRect.position = originalWindowPosition + (Vector3)delta;
		}
	}
}
#endif