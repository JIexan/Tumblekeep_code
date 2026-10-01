using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoinsManager : Singleton<CoinsManager>
{
	[SerializeField] Graphic coinsText;
	private int coinsAmount = 0;

	public int Amount => coinsAmount;

	private float coinMultiplier = 1f;

	public void SetMultiplier(float multiplier) {
		coinMultiplier = multiplier;
	}

	public void ResetMultiplier() {
		coinMultiplier = 1f;
	}

    public bool CanPay(int amount) {
		return coinsAmount >= amount;
	}

	public void Add(int amount) {
		int finalAmount = Mathf.RoundToInt((amount + Castle.Instance.GetCharacteristicValue(CharacteristicType.GoldGain)) * coinMultiplier);
		coinsAmount += finalAmount;
		UpdateCoinsText();

        InGameEventManager.SendEvent(InGameEvents.OnCoinsAmountChanged, finalAmount);
	}

	public void Substract(int amount) {
		coinsAmount -= amount;
		UpdateCoinsText();

		InGameEventManager.SendEvent(InGameEvents.OnCoinsAmountChanged, -amount);
	}

	private void UpdateCoinsText() {
		// Older scenes still bind a legacy Text to this shared manager.
		if (coinsText is TMP_Text tmpText)
			tmpText.text = coinsAmount.ToString();
		else if (coinsText is Text legacyText)
			legacyText.text = coinsAmount.ToString();
	}
}
