using DG.Tweening;
using Gameplay.RestorationSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLocationUIController : Singleton<PlayerLocationUIController> 
{
	[SerializeField] GameObject mainHud;
	[SerializeField] WindowsManager windowsManager;
	[SerializeField] Image fader;

	private int openWindowsCount = 0;

	void Start() 
	{
		if (windowsManager == null) {
			windowsManager = GetComponent<WindowsManager>();
		}

		windowsManager.OnBeforeWindowOpen += OnBeforeWindowOpen;
		windowsManager.OnAfterWindowClose += OnAfterWindowClose;

		InGameEventManager.Subscribe(InGameEvents.OnQuestClicked, OnQuestClicked);

#if CHEATS
		// Создаем читовое окно при старте игры (для тестирования)
		var cheatsManager = gameObject.AddComponent<Cheats.CheatsManager>();
		cheatsManager.GenerateMetaCheats();
#endif
	}



	private void OnDestroy() {
		InGameEventManager.Unsubscribe(InGameEvents.OnQuestClicked, OnQuestClicked);
		windowsManager.OnBeforeWindowOpen -= OnBeforeWindowOpen;
		windowsManager.OnAfterWindowClose -= OnAfterWindowClose;
	}

	private void OnBeforeWindowOpen(BaseWindow window) {
		openWindowsCount++;
		mainHud.SetActive(false);
	}

	private void OnAfterWindowClose(BaseWindow window) {
		openWindowsCount--;
		if (openWindowsCount <= 0)
			mainHud.SetActive(true);
	}

	// Конверт катсцены реюзает оконный ref-count — катсцена и окно не дерутся за HUD.
	public void HideHudForCutscene() {
		openWindowsCount++;
		mainHud.SetActive(false);
	}

	public void RestoreHudAfterCutscene() {
		openWindowsCount--;
		if (openWindowsCount <= 0)
			mainHud.SetActive(true);
	}
	private void OnQuestClicked(object obj) {
		// Во время катсцены окна не открываем — конверт катсцены сам управляет HUD
		if (Gameplay.Cutscenes.CutsceneController.IsPlaying)
			return;
		if (obj is ProgressStep quest) {
			ShowWindow(WindowType.QuestsWindow, quest);
		}
	}

	public void OnTasksClick() {
		ShowWindow(WindowType.QuestsWindow);
	}

	public void ShowShop() {
		ShowWindow(WindowType.ShopWindow);
	}

	public void ShowWindow(WindowType type, object data = null, Action onBeforeShow = null, Action onAfterHide = null) {
		windowsManager.ShowWindow(type, data, onBeforeShow, onAfterHide);
	}

	public void FadeIn(float duration, Action onComplete = null) {
		fader.gameObject.SetActive(true);
		fader.color = new Color(1f, 1f, 1f, 0f);
		fader.DOFade(1f, duration).OnComplete(() => onComplete?.Invoke());
	}

	public void FadeOut(float duration, Action onComplete = null) {
		fader.DOFade(0f, duration).OnComplete(() => {
			fader.gameObject.SetActive(false);
			onComplete?.Invoke();
		});
	}

}
