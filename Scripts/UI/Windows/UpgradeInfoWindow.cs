using Assets.SimpleLocalization;
using Gameplay.LevelUpgrades;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Инфо-окно конкретного апгрейда: имя/описание/иконка, строки требований, покупка.
// Data = UpgradeInfoData (levelId + upgradeId), состояние резолвится свежим на каждый Refresh.
// После успешной покупки окно остаётся открытым в состоянии «куплено».
public class UpgradeInfoWindow : AnimationWindow {
	[SerializeField] Image imgIcon;
	[SerializeField] TMP_Text txtName;
	[SerializeField] TMP_Text txtDesc;
	[SerializeField] UpgradeRequirementsPanel requirementsPanel;
	[SerializeField] PurchaseButton buyButton;
	[SerializeField] GameObject purchasedMark;

	private string levelId;
	private string upgradeId;

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);

		if (data is UpgradeInfoData info) {
			levelId = info.LevelId;
			upgradeId = info.UpgradeId;
			RefreshAll();
		}
	}

	private void OnEnable() {
		InGameEventManager.Subscribe(InGameEvents.OnLevelUpgradePurchased, OnStateChanged);
		InGameEventManager.Subscribe(InGameEvents.OnItemCountChanged, OnStateChanged);
		InGameEventManager.Subscribe(InGameEvents.OnIslandXPChanged, OnStateChanged);
	}

	private void OnDisable() {
		InGameEventManager.Unsubscribe(InGameEvents.OnLevelUpgradePurchased, OnStateChanged);
		InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnStateChanged);
		InGameEventManager.Unsubscribe(InGameEvents.OnIslandXPChanged, OnStateChanged);
	}

	private void OnStateChanged(object _) {
		// OnEnable срабатывает раньше Show — до инициализации данных ничего не обновляем.
		if (!string.IsNullOrEmpty(upgradeId))
			RefreshAll();
	}

	private void RefreshAll() {
		var entry = LevelUpgradesManager.GetConfig(levelId)?.GetUpgrade(upgradeId);
		if (entry == null)
			return;

		if (imgIcon != null) {
			imgIcon.sprite = entry.Icon;
			imgIcon.enabled = entry.Icon != null;
		}
		txtName.text = LocalizationManager.Localize("UpgradeName_" + upgradeId);
		txtDesc.text = LocalizationManager.Localize("UpgradeDesc_" + upgradeId);

		// Первое предметное требование показывается ценой на кнопке, а не строкой в списке.
		var primaryCost = FindPrimaryCost(entry);
		requirementsPanel.Init(entry, levelId, primaryCost);

		bool purchased = LevelUpgradesManager.IsPurchased(levelId, upgradeId);
		buyButton.gameObject.SetActive(!purchased);
		if (!purchased) {
			// Кнопка — только витрина: цена, наличие предмета, гейт CanPurchase.
			// Само списание делает TryPurchase в OnBuyClick (иначе расход задвоился бы).
			if (primaryCost != null)
				buyButton.InitExternal(primaryCost.Item, primaryCost.Count, CanPurchaseCurrent, OnBuyClick);
			else
				buyButton.InitExternal(CanPurchaseCurrent, OnBuyClick);
		}
		if (purchasedMark != null)
			purchasedMark.SetActive(purchased);
	}

	private static ItemUpgradeRequirement FindPrimaryCost(LevelUpgradeEntry entry) {
		foreach (var requirement in entry.Requirements) {
			if (requirement is ItemUpgradeRequirement item)
				return item;
		}
		return null;
	}

	private bool CanPurchaseCurrent() {
		return LevelUpgradesManager.CanPurchase(levelId, upgradeId);
	}

	// Вызывается PurchaseButton'ом через InitExternal (списание всех требований — внутри TryPurchase).
	public void OnBuyClick() {
		if (LevelUpgradesManager.TryPurchase(levelId, upgradeId))
			RefreshAll(); // событие покупки обновит и дерево позади окна
	}

	// Кнопка закрытия (onClick в префабе).
	public void Close() {
		Hide();
	}
}
