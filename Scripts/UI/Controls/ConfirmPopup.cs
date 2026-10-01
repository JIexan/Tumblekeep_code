using Assets.SimpleLocalization;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Попап подтверждения поверх окна: заголовок, пояснение, две кнопки и необязательный таймер.
// С таймером работает как «сохранить изменения?»: не подтвердили вовремя — срабатывает отмена.
public class ConfirmPopup : MonoBehaviour {
	[SerializeField] TMP_Text title;
	[SerializeField] TMP_Text message;
	[SerializeField] TMP_Text timer;
	[SerializeField] Button confirmButton;
	[SerializeField] TMP_Text confirmLabel;
	[SerializeField] Button cancelButton;
	[SerializeField] TMP_Text cancelLabel;

	private Action onConfirm;
	private Action onCancel;
	private string messageKey;
	private float timeLeft;
	private bool timed;
	private int shownSeconds;

	public bool IsShown => gameObject.activeSelf;

	private void Awake() {
		confirmButton.onClick.AddListener(Confirm);
		cancelButton.onClick.AddListener(Cancel);
	}

	// timeout > 0 — обратный отсчёт; в тексте messageKey секунды подставляются в {0}
	public void Show(string titleKey, string messageKey, string confirmKey, string cancelKey, float timeout, Action onConfirm, Action onCancel) {
		this.onConfirm = onConfirm;
		this.onCancel = onCancel;
		this.messageKey = messageKey;
		timed = timeout > 0f;
		timeLeft = timeout;
		shownSeconds = -1;

		title.text = LocalizationManager.Localize(titleKey);
		confirmLabel.text = LocalizationManager.Localize(confirmKey);
		cancelLabel.text = LocalizationManager.Localize(cancelKey);
		timer.gameObject.SetActive(timed);
		RefreshMessage();
		gameObject.SetActive(true);
	}

	private void Update() {
		if (!timed)
			return;
		timeLeft -= Time.unscaledDeltaTime;
		if (timeLeft <= 0f)
			Cancel();
		else
			RefreshMessage();
	}

	private void RefreshMessage() {
		if (!timed) {
			message.text = LocalizationManager.Localize(messageKey);
			return;
		}
		int seconds = Mathf.CeilToInt(timeLeft);
		if (seconds == shownSeconds)
			return;
		shownSeconds = seconds;
		message.text = LocalizationManager.Localize(messageKey, seconds);
		timer.text = seconds.ToString();
	}

	public void Confirm() {
		Close(onConfirm);
	}

	public void Cancel() {
		Close(onCancel);
	}

	private void Close(Action callback) {
		if (!IsShown)
			return;
		timed = false;
		onConfirm = onCancel = null;
		gameObject.SetActive(false);
		callback?.Invoke();
	}
}
