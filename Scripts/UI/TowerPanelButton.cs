using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TowerButtonType {
	Buy,
	Sell,
	Upgrade,
	PowerUp
}

public class TowerPanelButton : MonoBehaviour
{
	public Sprite[] towerTypeSprites;
	public Sprite[] upgradeSprites;

	public Button button;
	public GameObject pricePanel;
	public TMP_Text priceText;

	public GameObject checkMark;
	public GameObject contentPanel;

	public Color activeColor;
	public Color tooExpensiveColor;

	public Image icon;
	public Image smallIcon;

    private int priceConst;
	private Action<int> onClickCallback;
	private Action<int> onConfirmCallback;
	private int buttonId;

	private bool waitingConfirm;

	private TowerButtonType type;

    public void OnClick() {
		if (button == null || !button.interactable)
			return;
		if (!waitingConfirm) {
			onClickCallback?.Invoke(buttonId);
			waitingConfirm = true;
			contentPanel.SetActive(false);
			checkMark.SetActive(true);
		} else {
			onConfirmCallback?.Invoke(buttonId);
			waitingConfirm = false;
		}
	}

	public void ResetConfirm(int skipId) {
		if(waitingConfirm && buttonId != skipId) {
			waitingConfirm = false;
			contentPanel.SetActive(true);
			checkMark.SetActive(false);
		}
	}

	/*public void Init(Action callback, string text, bool interactible, string priceText, Color priceColor) {
		onClickCallback = callback;
		this.text.text = text;
		if (priceText == "") {
			pricePanel.SetActive(false);
		} else {
			pricePanel.SetActive(true);
			this.priceText.text = priceText;
			this.priceText.color = priceColor;
			button.interactable = interactible;
		}
	}*/

    // Общая панель башен и сундуков проверяет только цену.
    // Лимит башен проверяется в GameController при выборе площадки и постройке.
    public void UpdatePurchaseAvailability()
    {
		if (type == TowerButtonType.Sell || button == null)
			return;

        if (CoinsManager.Instance.CanPay(priceConst))
        {
            button.interactable = true;
            priceText.color = activeColor;
        }
        else
        {
            button.interactable = false;
            priceText.color = tooExpensiveColor;
        }
    }

    public void InitBuyButton(int buttonId, Action<int> clickCallback, Action<int> confirmCallback, string iconName, int price, TowerButtonType type, int level) {
		this.type = type;
		this.buttonId = buttonId;

        priceConst = price;
		pricePanel.SetActive(true);

		onClickCallback = clickCallback;
		onConfirmCallback = confirmCallback;
		priceText.text = price.ToString();

		icon.sprite = GameController.Instance.uiController.GetIcon(iconName);

		waitingConfirm = false;
		contentPanel.SetActive(true);
		checkMark.SetActive(false);
		
		switch(type) {
			case TowerButtonType.Buy:
				smallIcon.gameObject.SetActive(true);
				TowerType t = (TowerType)buttonId;
				switch(t) {
					case TowerType.Laser:
						smallIcon.sprite = towerTypeSprites[0];
						break;
					case TowerType.Arrow:
						smallIcon.sprite = towerTypeSprites[1];
						break;
					case TowerType.Mortar:
						smallIcon.sprite = towerTypeSprites[2];
						break;
					case TowerType.Electric:
						smallIcon.sprite = towerTypeSprites[3];
						break;
				}
				
				break;
			case TowerButtonType.Sell:
				smallIcon.gameObject.SetActive(false);
				break;
			case TowerButtonType.Upgrade:
				smallIcon.gameObject.SetActive(true);
				if(level > 2) {
					level = 0;
				}
				smallIcon.sprite = upgradeSprites[level];
				break;
		}


		UpdatePurchaseAvailability();
    }
}
