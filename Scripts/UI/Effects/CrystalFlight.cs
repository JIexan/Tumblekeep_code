using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

// Полёт UI-кристалла по дуге из одного RectTransform в другой (счётчик → цена таска и т.п.).
// Спрайт создаётся на корневом канвасе последним сиблингом — поверх любых окон; цель может ехать
// (панелька за камерой) — конец дуги пересчитывается каждый кадр. Используют QuestTrackingPanel и QuestsWindow.
public static class CrystalFlight {
	[Serializable]
	public class Settings {
		public Vector2 flySize = new Vector2(56f, 56f);
		public float flyDuration = 0.65f;
		public Ease flyEase = Ease.InOutSine;
		[Tooltip("Подъём дуги, доля от дистанции до цели")]
		public float curveHeight = 0.25f;
		public float iconPunch = 0.45f;
		public float punchDuration = 0.3f;
	}

	// link — объект, со смертью которого твин гасится (спрайт убирается вместе с ним)
	public static Tween Fly(RectTransform from, RectTransform to, Sprite sprite, Settings settings, GameObject link, Action onArrived) {
		var canvas = to != null ? to.GetComponentInParent<Canvas>() : null;
		if (from == null || to == null || canvas == null || sprite == null) {
			onArrived?.Invoke();
			return null;
		}

		var go = new GameObject("CrystalFly", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		var flying = (RectTransform)go.transform;
		flying.SetParent(canvas.rootCanvas.transform, false);
		flying.SetAsLastSibling();
		flying.sizeDelta = settings.flySize;
		var image = go.GetComponent<Image>();
		image.sprite = sprite;
		image.preserveAspect = true;
		image.raycastTarget = false;

		Vector3 start = from.position;
		flying.position = start;
		from.DOKill(true);
		from.DOPunchScale(Vector3.one * settings.iconPunch, settings.punchDuration, 1, 0f);

		float endScale = to.rect.width / Mathf.Max(1f, settings.flySize.x);
		return DOVirtual.Float(0f, 1f, settings.flyDuration, t => {
			if (flying == null || to == null)
				return;
			Vector3 end = to.position;
			Vector3 control = (start + end) * 0.5f + Vector3.up * (Vector3.Distance(start, end) * settings.curveHeight);
			float u = 1f - t;
			flying.position = u * u * start + 2f * u * t * control + t * t * end;
			flying.localScale = Vector3.one * Mathf.Lerp(1f, endScale, t);
		}).SetEase(settings.flyEase).SetLink(link).OnKill(() => {
			if (flying != null)
				UnityEngine.Object.Destroy(flying.gameObject);
		}).OnComplete(() => {
			if (to != null)
				to.DOPunchScale(Vector3.one * settings.iconPunch, settings.punchDuration, 1, 0f).SetLink(link);
			onArrived?.Invoke();
		});
	}
}
