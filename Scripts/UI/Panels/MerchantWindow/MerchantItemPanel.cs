using Assets.SimpleLocalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Панель отображения одного товара в окне торговца
/// </summary>
public class MerchantItemPanel : MonoBehaviour
{
	[SerializeField] private Image iconImage;
	[SerializeField] private TMP_Text itemNameText;
	[SerializeField] private TMP_Text countText;
	[SerializeField] private PurchaseButton purchaseButton;

	private MerchantOffer offer;
	private MerchantWindow parentWindow;

	public MerchantOffer Offer => offer;

	/// <summary>Программная покупка (AUTOPLAY): тот же путь, что и клик по кнопке — проверка монет внутри</summary>
	public void Purchase() {
		purchaseButton.OnButtonClick();
	}

	public void Init(MerchantWindow window, MerchantOffer merchantOffer) {
		parentWindow = window;
		offer = merchantOffer;

		iconImage.sprite = offer.SourceData.icon;
		itemNameText.text = LocalizationManager.Localize(ItemLocalizationKeys.GetNameKey(offer.ItemType));
		countText.text = "x" + offer.Count.ToString();

		purchaseButton.Init(
			offer.Price,
			OnPurchaseSuccess
		);
	}

	private void OnPurchaseSuccess() {
		// Монеты уже списаны в PurchaseButton — начисляем товар
		SavesManager.Profile.AddItem(offer.ItemType, offer.Count);

		if (parentWindow != null) {
			parentWindow.OnItemPurchased(offer);
		}
	}
}