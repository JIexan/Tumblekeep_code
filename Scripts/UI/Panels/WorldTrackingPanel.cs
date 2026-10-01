using System;
using UnityEngine;
using UnityEngine.UI;

public class WorldTrackingPanel : MonoBehaviour
{
	[SerializeField] protected Transform directionArrow;
	[SerializeField] protected Image timerImage;

	protected Transform trackTransform;
	protected Camera c;

	protected Rect panelRect;
	protected Vector2 screenSize;
	protected Vector2 parentOffsetMin;

	private Action onClick;
	private Action onTimerExpired;

	private float totalTime;
	private float timer;
	private bool timerActive;
	private bool timerExpiredFired;

	protected virtual void Awake() {
		c = Camera.main;
	}

	public virtual void Init(Transform targetTransform, Vector2 screenSize, Action onClick) {
		// Панель могли создать под выключенным родителем (HUD хаба скрыт окном/катсценой) — Awake ещё не вызывался
		if (c == null)
			c = Camera.main;

		trackTransform = targetTransform;
		this.onClick = onClick;

		RectTransform parentRect = transform.parent as RectTransform;
		this.screenSize = screenSize;
		parentOffsetMin = parentRect.offsetMin;
		panelRect = new Rect(Vector3.zero, new Vector2(
			screenSize.x - parentRect.offsetMin.x + parentRect.offsetMax.x,
			screenSize.y - parentRect.offsetMin.y + parentRect.offsetMax.y));

		// Сброс таймера
		timerActive = false;
		timerExpiredFired = false;
		if (timerImage != null)
			timerImage.gameObject.SetActive(false);

		UpdatePosition();
	}

	/// <summary>
	/// Запустить таймер обратного отсчёта
	/// </summary>
	public void SetTimer(float duration, Action onExpired = null) {
		totalTime = duration;
		timer = 0f;
		timerActive = true;
		timerExpiredFired = false;
		onTimerExpired = onExpired;

		if (timerImage != null) {
			timerImage.gameObject.SetActive(true);
			timerImage.fillAmount = 1f;
		}
	}

	/// <summary>
	/// Текущий прогресс таймера (0..1), где 0 = только начал, 1 = истёк
	/// </summary>
	public float TimerProgress => totalTime > 0f ? Mathf.Clamp01(timer / totalTime) : 0f;

	/// <summary>
	/// Оставшееся время
	/// </summary>
	public float TimeRemaining => Mathf.Max(0f, totalTime - timer);

	public bool IsTimerActive => timerActive;

	protected virtual void Update() {
		UpdatePosition();
		UpdateTimer();
	}

	protected virtual void UpdatePosition() {
		if (trackTransform == null) return;

		Vector2 targetViewportPos = WorldToScreenPointProjected(c, trackTransform.position);
		Vector2 newPos = (targetViewportPos * screenSize) - parentOffsetMin;

		if (!panelRect.Contains(newPos)) {
			(transform as RectTransform).anchoredPosition = new Vector2(
				Mathf.Clamp(newPos.x, panelRect.xMin, panelRect.xMax),
				Mathf.Clamp(newPos.y, panelRect.yMin, panelRect.yMax));

			directionArrow.gameObject.SetActive(true);
			directionArrow.up = (newPos - (transform as RectTransform).anchoredPosition).normalized;
		} else {
			(transform as RectTransform).anchoredPosition = newPos;
			directionArrow.gameObject.SetActive(false);
		}
	}

	protected virtual void UpdateTimer() {
		if (!timerActive) return;

		timer += Time.deltaTime;

		if (timerImage != null)
			timerImage.fillAmount = 1f - TimerProgress;

		if (timer >= totalTime && !timerExpiredFired) {
			timerExpiredFired = true;
			timerActive = false;
			OnTimerExpired();
		}
	}

	protected virtual void OnTimerExpired() {
		onTimerExpired?.Invoke();
	}

	protected Vector2 WorldToScreenPointProjected(Camera camera, Vector3 worldPos) {
		Vector3 camNormal = camera.transform.forward;
		Vector3 vectorFromCam = worldPos - camera.transform.position;
		float camNormDot = Vector3.Dot(camNormal, vectorFromCam);
		if (camNormDot <= 0) {
			Vector3 proj = (camNormal * camNormDot * 1.01f);
			worldPos = camera.transform.position + (vectorFromCam - proj);
		}

		return camera.WorldToViewportPoint(worldPos);
	}

	public virtual void Show() {
		gameObject.SetActive(true);
	}

	public virtual void Hide() {
		timerActive = false;
		gameObject.SetActive(false);
	}

	public virtual void OnPanelClicked() {
		onClick?.Invoke();
	}
}
