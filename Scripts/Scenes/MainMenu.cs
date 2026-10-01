using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : Singleton<MainMenu>
{
	[SerializeField] PlayerLocationUIController uiController;
	[SerializeField] GameObject location;
	[SerializeField] GlobalMapController globalMap;
	[SerializeField] CameraController cameraController;

	public void SwitchToMap() {
		uiController.FadeIn(1f, () => {
			globalMap.gameObject.SetActive(true);
			globalMap.CenterCamera();
			location.SetActive(false);
			uiController.ShowWindow(WindowType.GlobalMapWindow);
			uiController.FadeOut(1f, null);
			cameraController.FadeOut(1f);
		});
		cameraController.FadeIn(0.95f);

	}

	public void SwitchToLocation(Action onHidden) {
		uiController.FadeIn(1f, () => {
			globalMap.gameObject.SetActive(false);
			location.SetActive(true);
			onHidden?.Invoke();
			uiController.FadeOut(1f, null);
			cameraController.FadeOut(1f);
		});
		cameraController.FadeIn(0.95f);
	}
}
