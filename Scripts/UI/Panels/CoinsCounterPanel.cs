using TMPro;
using UnityEngine;

// Счётчик золота катки для окон, которые затемняют HUD (апгрейды, торговец): цены в них — в золоте,
// а HUD-счётчик CoinsManager под затемнением не читается. Аналог ItemCounterPanel, но монеты
// живут не в профиле, а в CoinsManager.
public class CoinsCounterPanel : MonoBehaviour
{
	[SerializeField] TMP_Text counter;

	private void OnEnable() {
		InGameEventManager.Subscribe(InGameEvents.OnCoinsAmountChanged, OnCoinsChanged);
		UpdateCounter();
	}

	private void OnDisable() {
		InGameEventManager.Unsubscribe(InGameEvents.OnCoinsAmountChanged, OnCoinsChanged);
	}

	private void OnCoinsChanged(object delta) {
		UpdateCounter();
	}

	private void UpdateCounter() {
		if (counter == null || CoinsManager.Instance == null)
			return;
		counter.text = CoinsManager.Instance.Amount.ToString();
	}
}
