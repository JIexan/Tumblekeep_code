using System;
using System.Collections.Generic;
using UI.UpgradeWindow;
using UnityEngine;

public class UpgradeWindow : AnimationWindow
{

	[SerializeField] StatsPanel statsPanel;
	[SerializeField] UpgradePanel[] upgradePanels;
	[SerializeField] PurchaseButton purchaseButton;

	IUpgradable target;
	UpgradePanel hoveredPanel;

	private void OnEnable() {
		InGameEventManager.Subscribe(InGameEvents.OnRerollsChanged, OnRerollsChanged);
	}

	// InGameEventManager между сценами не чистится — отписка обязательна
	private void OnDisable() {
		InGameEventManager.Unsubscribe(InGameEvents.OnRerollsChanged, OnRerollsChanged);
		ResetHover();
	}

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);

		Time.timeScale = 0f;
		SetInputLocked(false);
		ResetHover();

		target = data as IUpgradable;

		// Без цели реролить нечего — иначе на кнопке осталась бы привязка от прошлого показа
		purchaseButton.gameObject.SetActive(target != null);

		if(target == null) {
			return;
		}

		UpdateState();
		BindReroll();
	}

	// Пул бесплатных рероллов и цена в золоте — общие с торговцем (RerollsManager).
	// «Кости шулера» (-100% к UpgradeRerollPrice) делают реролл этого окна бесплатным без траты пула.
	private void BindReroll() {
		RerollButtonBinder.Bind(purchaseButton, OnRegenerateOptionsClick, Gameplay.Relics.RelicValue.UpgradeRerollPrice);
	}

	private void OnRerollsChanged(object data) {
		if (target != null)
			BindReroll();
	}

	private void UpdateState() {
		List<UpgradeData> upgradeData = GenerateUpgradeOptions();
		hoveredPanel = null;
		statsPanel.Init(target);

		for (int i = 0; i < upgradePanels.Length; i++) {
			if (i < upgradeData.Count) {
				upgradePanels[i].gameObject.SetActive(true);
				upgradePanels[i].Init(this, upgradeData[i]);
			} else {
				upgradePanels[i].gameObject.SetActive(false);
			}
		}
	}

	public void ShowUpgradePreview(UpgradePanel source, UpgradeData upgrade) {
		if (IsClosing || !isActiveAndEnabled)
			return;

		hoveredPanel = source;
		statsPanel.ShowPreview(upgrade as CharacteristicUpgradeData);
	}

	public void ClearUpgradePreview(UpgradePanel source) {
		// An exit from the previous card must not clear the newly hovered card's preview.
		if (hoveredPanel != source)
			return;
		hoveredPanel = null;
		statsPanel.ClearPreview();
	}

	private void ResetHover() {
		hoveredPanel = null;
		foreach (var panel in upgradePanels)
			panel.ResetHover();
		statsPanel.ClearPreview();
	}

	public override void Hide() {
		// base.Hide() до CanBeClosed отказывает — паузу снимаем только вместе с реальным закрытием
		if (!CanBeClosed)
			return;

		// Пока играет WindowClose, карточки и реролл нажиматься не должны
		SetInputLocked(true);
		ResetHover();
		base.Hide();
		Time.timeScale = 1f;
	}

	private List<UpgradeData> GenerateUpgradeOptions() {
		return target.GetUpgradeOptions();
	}

	public void OnRegenerateOptionsClick() {
		UpdateState();

	}

}
