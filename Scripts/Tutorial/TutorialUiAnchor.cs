using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Tutorial {
	// Маркер UI-элемента, к которому может привязаться туториальная реплика.
	// Реестр по строковым id (а не пути в иерархии) — переживает реструктуризацию
	// префабов и работает в обеих сценах; неактивная цель выпадает из реестра,
	// и хинт прячется сам. Если на объекте есть Button, клик репортится
	// в OnAnchorClicked — на него смотрит AnchorClickedCondition.
	public class TutorialUiAnchor : MonoBehaviour {
		[SerializeField] string anchorId;

		public string AnchorId => anchorId;

		public static event Action<string> OnAnchorClicked;

		private static readonly Dictionary<string, TutorialUiAnchor> registry = new();

		private UnityEngine.UI.Button button;

		public static bool TryGet(string id, out RectTransform rect) {
			rect = null;
			if (string.IsNullOrEmpty(id) || !registry.TryGetValue(id, out var anchor) || anchor == null)
				return false;
			rect = (RectTransform)anchor.transform;
			return true;
		}

		// Id в рантайме — для элементов из пула, которым нельзя раздать id в префабе
		// (узел дерева апгрейдов: один инстанс в разное время показывает разные апгрейды).
		public void SetId(string id) {
			if (anchorId == id)
				return;
			Unregister();
			anchorId = id;
			if (isActiveAndEnabled)
				Register();
		}

		private void OnEnable() {
			Register();
			button = GetComponent<UnityEngine.UI.Button>();
			if (button != null)
				button.onClick.AddListener(ReportClick);
		}

		private void OnDisable() {
			Unregister();
			if (button != null)
				button.onClick.RemoveListener(ReportClick);
		}

		// Не в OnEnable: рантайм-id приходит через SetId уже после включения объекта
		private void Start() {
			if (string.IsNullOrEmpty(anchorId))
				Debug.LogWarning($"[TutorialUiAnchor] '{name}': пустой anchorId.", this);
		}

		private void Register() {
			if (string.IsNullOrEmpty(anchorId))
				return;
			if (registry.TryGetValue(anchorId, out var existing) && existing != null && existing != this)
				Debug.LogWarning($"[TutorialUiAnchor] Дублирующийся anchorId '{anchorId}' ('{existing.name}' и '{name}').", this);
			registry[anchorId] = this;
		}

		private void Unregister() {
			if (!string.IsNullOrEmpty(anchorId) && registry.TryGetValue(anchorId, out var current) && current == this)
				registry.Remove(anchorId);
		}

		private void ReportClick() {
			if (!string.IsNullOrEmpty(anchorId))
				OnAnchorClicked?.Invoke(anchorId);
		}
	}
}
