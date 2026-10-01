using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerPanel : MonoBehaviour
{
	public List<TowerPanelButton> buttons;
	public CanvasGroup cg;

	public AnimationCurve alphaCurve;
	public AnimationCurve scaleCurve;
	public AnimationCurve moveCurve;
	public float openAnimationTime;
	public float buttonsOffset;

	private int buttonCount;

	private struct ButtonSetup {
		public Vector2 anchorMin;
		public Vector2 anchorMax;
		public Vector2 pivot;
		public Vector3 rotation;
	}

	public void ClearButtons() {
		foreach(var button in buttons) {
			button.gameObject.SetActive(false);
		}
		buttonCount = 0;
	}

	public void AddBuyButton(int buttonId, Action<int> clickCallback, Action<int> confirmCallback, string iconName, int price, TowerButtonType type, int level = 0) {
		if (buttonCount == buttons.Count)
			return;
		buttons[buttonCount].InitBuyButton(buttonId, clickCallback, confirmCallback, iconName, price, type, level);
		buttonCount++;
	}

	public void ResetButtonsConfirmMode(int exception) {
		foreach (var button in buttons) {
			button.ResetConfirm(exception);
		}
	}

    private void UpdatePurchaseAvailability(object boj)
    {
        for (int i = 0; i < buttonCount; i++)
            buttons[i].UpdatePurchaseAvailability();
    }

    public void Show() {
		if (buttons.Count == 0)
			return;

        InGameEventManager.Subscribe(InGameEvents.OnCoinsAmountChanged, UpdatePurchaseAvailability);

		Vector2 buttonSize = (buttons[0].transform as RectTransform).rect.size;
		float xOffset = buttonsOffset / buttonSize.x;
		float yOffset = buttonsOffset / buttonSize.y;

		ButtonSetup[] buttonsPositions;
		switch(buttonCount) {
			case 1:
				buttonsPositions = new ButtonSetup[1] {
					new ButtonSetup() {
						anchorMin = new Vector2(0.5f, 1f),
						anchorMax = new Vector2(0.5f, 1f),
						pivot = new Vector2(0.5f, 0.5f)
					}
				};
				break;
			case 2:
				buttonsPositions = new ButtonSetup[2] {
					new ButtonSetup() {
						anchorMin = new Vector2(0f, 0.5f),
						anchorMax = new Vector2(0f, 0.5f),
						pivot = new Vector2(1f - xOffset, 0.5f)
					},
					new ButtonSetup() { 
						anchorMin = new Vector2(1f, 0.5f),
						anchorMax = new Vector2(1f, 0.5f),
						pivot = new Vector2(xOffset, 0.5f)
					}
				};
				break;
			case 3:
				buttonsPositions = new ButtonSetup[3] {
					new ButtonSetup() {
						anchorMin = new Vector2(0f, 1f),
						anchorMax = new Vector2(0f, 1f),
						pivot = new Vector2(0.3f, yOffset),
						rotation = new Vector3(0f,0f,14)
					},
					new ButtonSetup() {
						anchorMin = new Vector2(1f, 1f),
						anchorMax = new Vector2(1f, 1f),
						pivot = new Vector2(0.7f, yOffset),
						rotation = new Vector3(0f,0f,-13)
					},
					new ButtonSetup() {
						anchorMin = new Vector2(0.5f, 0f),
						anchorMax = new Vector2(0.5f, 0f),
						pivot = new Vector2(0.5f, 1f - yOffset)
					}
				};
				break;
			case 4:
				buttonsPositions = new ButtonSetup[4] {
					new ButtonSetup() {
						anchorMin = new Vector2(0f, 1f),
						anchorMax = new Vector2(0f, 1f),
						pivot = new Vector2(0.3f, yOffset),
						rotation = new Vector3(0f,0f,14)
					},
					new ButtonSetup() {
						anchorMin = new Vector2(1f, 1f),
						anchorMax = new Vector2(1f, 1f),
						pivot = new Vector2(0.7f, yOffset),
						rotation = new Vector3(0f,0f,-13)
					},
					new ButtonSetup() {
						anchorMin = new Vector2(1f, 0f),
						anchorMax = new Vector2(1f, 0f),
						pivot = new Vector2(0.7f, 1f - yOffset),
						rotation = new Vector3(0f,0f,15)
					},
					new ButtonSetup() {
						anchorMin = new Vector2(0f, 0f),
						anchorMax = new Vector2(0f, 0f),
						pivot = new Vector2(0.3f, 1f - yOffset),
						rotation = new Vector3(0f,0f,-14)
					}
				};
				break;
			default:
				return;
		}

		//Vector2 panelSize;

		for (int i = 0; i < buttonCount; i++) {
			//Vector3 anchor = (Quaternion.Euler(0, 0, angle * buttonsPositions[i].pos) * new Vector3(0f,0.5f,0f)) + new Vector3(0.5f,0.5f,0f);
			(buttons[i].transform as RectTransform).anchorMin = buttonsPositions[i].anchorMin;
			(buttons[i].transform as RectTransform).anchorMax = buttonsPositions[i].anchorMax;
			(buttons[i].transform as RectTransform).pivot = buttonsPositions[i].pivot;
			(buttons[i].transform as RectTransform).anchoredPosition3D = Vector3.zero;
			buttons[i].transform.localRotation = Quaternion.Euler(buttonsPositions[i].rotation);

			buttons[i].gameObject.SetActive(true);
		}
		//cg.interactable = false;
		cg.alpha = 0f;
		//LeanTween.move((buttons[i].transform as RectTransform), Vector3.zero, openAnimationTime).setEase(moveCurve);
		transform.localScale = Vector3.zero;
		transform.DOScale(Vector3.one, openAnimationTime).SetEase(scaleCurve);
		cg.DOFade(1f, openAnimationTime).SetEase(alphaCurve);//.setOnComplete(() => { cg.interactable = true; });

		gameObject.SetActive(true);
	}

	public void Hide() {
        InGameEventManager.Unsubscribe(InGameEvents.OnCoinsAmountChanged, UpdatePurchaseAvailability);
		ClearButtons();
		gameObject.SetActive(false);
	}

}
