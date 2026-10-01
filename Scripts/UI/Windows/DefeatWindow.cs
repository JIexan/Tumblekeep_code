using System;
using TMPro;
using UnityEngine;

// Поражение сжигает все награды катки (выплата только по кнопке «Уплыть»), показывать нечего.
public class DefeatWindow : AnimationWindow {

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);
		Time.timeScale = 0f;
	}

	public void OnRestart()
	{
		Time.timeScale = 1f;
		GameController.Instance.OnRestart();
	}

	public void OnMainMenu()
	{
		Time.timeScale = 1f;
		GameController.Instance.OnMainMenu();
	}

	/// <summary>
	/// Продолжить за премиум валюту.
	/// </summary>
	public void OnContinue() {
		Time.timeScale = 1f;
		// TODO: списать премиум валюту и продолжить игру
		Hide();
	}

	override public void Hide() {
		Time.timeScale = 1f;
		base.Hide();
	}
}
