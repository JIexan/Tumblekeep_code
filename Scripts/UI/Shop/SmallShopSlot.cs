using Assets.SimpleLocalization;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class SmallShopSlot : BaseShopSlot
{
#pragma warning disable 0649
	[Header("Product count")]
	[SerializeField] TMP_Text productCountText;
	[SerializeField] TMP_Text productTitleText;

	[Header("Sale badge")]
	[SerializeField] GameObject saleBadge;
	[SerializeField] TMP_Text saleBadgeText;

	[Header("Icons")]
	[SerializeField] Image packIcon;
	[SerializeField] Sprite[] packImages;

	[Header("Price")]
	[SerializeField] TMP_Text priceText;
#pragma warning restore 0649

	private Shop shop;
	private ItemPackSettings packSettings;

	public void Init(Shop shop, ItemPackSettings packConfiguration, string productPrice, int positionId) {
		this.shop = shop;
		packSettings = packConfiguration;

		packIcon.sprite = packImages[positionId];

		priceText.text = "$" + packConfiguration.price.ToString("0.00");

		saleBadge.SetActive(positionId == 2 ||  positionId == 5);

		cloud.gameObject.SetActive(positionId == 0 || positionId == 3);

		productCountText.text = packConfiguration.rewards[0].count.ToString();
		productTitleText.text = LocalizationManager.Localize(packConfiguration.name);
	}

	public void OnPurchaseClick() {
		SavesManager.Profile.AddItem(packSettings.rewards[0].item, packSettings.rewards[0].count);
	}

}
