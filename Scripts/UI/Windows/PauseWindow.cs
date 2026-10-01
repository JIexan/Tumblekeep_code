using System;
using UnityEngine;

/// <summary>
/// Окно паузы (бывший PopupPause). Стандартное окно на AnimationWindow:
/// открывается через WindowsManager (WindowType.PauseWindow), ставит timeScale = 0,
/// откатывает его при любом пути закрытия (Hide/CloseImmediate/HideAllWindows).
/// </summary>
public class PauseWindow : AnimationWindow
{
	bool pausedGame;

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);

		Time.timeScale = 0f;
		pausedGame = true;
	}

	public override void Hide() {
		if (!CanBeClosed)
			return;
		base.Hide();
		Unpause();
	}

	// Покрывает все пути закрытия, включая HideAllWindows и смену сцены
	void OnDisable() {
		Unpause();
	}

	void Unpause() {
		if (!pausedGame)
			return;
		pausedGame = false;
		Time.timeScale = 1f;
	}

	protected override void Update() {
		base.Update();
		// Esc закрывает паузу, но не из-под открытых поверх неё настроек
		if (Input.GetKeyDown(KeyCode.Escape) && CanBeClosed && !IsClosing && !IsSettingsOpen())
			Hide();
	}

	// Тот же WindowsManager, что показал паузу (UICanvas в бою)
	private WindowsManager Windows => GetComponentInParent<WindowsManager>(true);

	private bool IsSettingsOpen() {
		var windows = Windows;
		return windows != null && windows.IsWindowActive(WindowType.Settings);
	}

	// ---- Кнопки ----

	public void OnContinue() {
		Hide();
	}

	// Настройки открываются поверх паузы, игра остаётся на timeScale = 0
	public void OnSettings() {
		if (!CanBeClosed || IsClosing)
			return;
		var windows = Windows;
		if (windows != null)
			windows.ShowWindow(WindowType.Settings);
	}

	public void OnMainMenu() {
		Unpause();
		GameController.Instance.OnMainMenu();
	}
}
