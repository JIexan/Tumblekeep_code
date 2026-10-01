using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Данные для инициализации окна торговца
/// </summary>
public class MerchantWindowData
{
	public MerchantShip Ship { get; set; }
	public int PurchaseCount { get; set; }
}

/// <summary>
/// Окно торговца с набором товаров
/// </summary>
public class MerchantWindow : AnimationWindow
{
	[SerializeField] private MerchantItemPanel[] itemPanels;
	[SerializeField] private PurchaseButton rerollButton;

	private ItemSettings itemSettings => GameConfigs.Instance.ItemSettings;
	private MerchantShip currentShip;
	private int purchaseCount;
	private bool purchaseMade;

	private void OnEnable() {
		InGameEventManager.Subscribe(InGameEvents.OnRerollsChanged, OnRerollsChanged);
	}

	// InGameEventManager между сценами не чистится — отписка обязательна
	private void OnDisable() {
		InGameEventManager.Unsubscribe(InGameEvents.OnRerollsChanged, OnRerollsChanged);
	}

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);

		Time.timeScale = 0f;
		purchaseMade = false;
		SetInputLocked(false);

		var windowData = data as MerchantWindowData;
		if (windowData != null) {
			currentShip = windowData.Ship;
			purchaseCount = windowData.PurchaseCount;
			// Приостанавливаем таймер ожидания, пока открыто окно
			currentShip.PauseWaiting();
		} else {
			currentShip = null;
			purchaseCount = 0;
		}

		DisplayOffers();
		BindReroll();
	}

	/// <summary>
	/// Показать предложения (генерируются при первом открытии, затем переиспользуются)
	/// </summary>
	private void DisplayOffers() {
		if (currentShip != null && !currentShip.HasOffers) {
			GenerateOffers();
		}

		var offers = currentShip?.Offers;

		for (int i = 0; i < itemPanels.Length; i++) {
			if (offers != null && i < offers.Count) {
				itemPanels[i].gameObject.SetActive(true);
				itemPanels[i].Init(this, offers[i]);
			} else {
				itemPanels[i].gameObject.SetActive(false);
			}
		}
	}

	/// <summary>
	/// Сгенерировать предложения и сохранить в корабле.
	/// avoid — товары прежнего прилавка (реролл старается их не повторять)
	/// </summary>
	private void GenerateOffers(ICollection<ItemType> avoid = null) {
		var selectedItems = itemSettings.SelectRandomMerchantItems(itemSettings.merchantItemsToShow, avoid);
		var offers = new List<MerchantOffer>();

		for (int i = 0; i < selectedItems.Count; i++) {
			offers.Add(new MerchantOffer(selectedItems[i], purchaseCount, itemSettings));
		}

		currentShip.SetOffers(offers);
	}

	// Пул бесплатных рероллов и цена в золоте — общие с окном апгрейдов (RerollsManager).
	// Если торговцу нечем заменить прилавок (товаров в пуле не больше, чем показано) — кнопку прячем.
	private void BindReroll() {
		if (rerollButton == null)
			return;

		bool canReroll = currentShip != null && itemSettings.GetMerchantPoolCount() > itemSettings.merchantItemsToShow;
		rerollButton.gameObject.SetActive(canReroll);
		if (canReroll)
			RerollButtonBinder.Bind(rerollButton, OnRerollClick);
	}

	private void OnRerollsChanged(object data) {
		if (currentShip != null)
			BindReroll();
	}

	// Реролл — не покупка: purchaseMade не трогаем, корабль остаётся ждать.
	// Новый прилавок сохраняется на корабле, повторное открытие окна покажет его же.
	private void OnRerollClick() {
		if (currentShip == null)
			return;

		var previous = currentShip.Offers?.Select(o => o.ItemType).ToList();
		GenerateOffers(previous);
		DisplayOffers();
	}

	/// <summary>
	/// Вызывается при покупке товара
	/// </summary>
	public void OnItemPurchased(MerchantOffer offer) {
		purchaseCount++;
		purchaseMade = true;

		// Счётчик покупок катки живёт в ShipsController: от него дорожают товары следующих торговцев
		// (ItemSettings.CalculateMerchantPrice). Раньше его никто не инкрементил — цены не росли вовсе.
		var ships = GameController.Instance != null ? GameController.Instance.Ships : null;
		if (ships != null)
			ships.IncrementPurchaseCount();

		// Отправляем событие о покупке
		InGameEventManager.SendEvent(InGameEvents.OnMerchantPurchase);

		// Закрываем окно после покупки
		Hide();
	}

	/// <summary>
	/// Кнопка закрытия окна без покупки
	/// </summary>
	public void OnCloseButtonClick() {
		Hide();
	}

	public override void Hide() {
		// base.Hide() в первые мгновения после открытия отказывает (CanBeClosed) — без этого гейта
		// хвост всё равно отпускал корабль и обнулял currentShip при открытом окне
		if (!CanBeClosed)
			return;

		// Пока играет WindowClose, товары и реролл нажиматься не должны (двойной клик покупал второй товар)
		SetInputLocked(true);
		base.Hide();
		Time.timeScale = 1f;

		if (currentShip != null) {
			if (purchaseMade) {
				// Была покупка — корабль сразу уплывает
				currentShip.FinishTrading();
			} else {
				// Ничего не купили — таймер ожидания продолжает идти
				currentShip.ResumeWaiting();
			}
			currentShip = null;
		}
	}
}
