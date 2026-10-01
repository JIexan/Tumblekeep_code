using System;
using UnityEngine;

namespace Gameplay.Rerolls {
	// Настройки рероллов катки: сколько бесплатных выдаётся на старте и как дорожают платные (за золото катки).
	// Пул и лестница цен общие на все окна с рероллом (апгрейды башен/замка, торговец).
	// Ассет: Assets/Settings/RerollSettings.asset, доступ — GameConfigs.Instance.RerollSettings;
	// создание и подвязка — Tools/Rerolls/Create Default Settings (RerollSettingsBuilder).
	[CreateAssetMenu(fileName = "RerollSettings", menuName = "Settings/RerollSettings")]
	public class RerollSettings : ScriptableObject {
		[Tooltip("Сколько бесплатных рероллов игрок получает в начале каждой катки")]
		[Min(0)] [SerializeField] int freeRerollsPerMatch = 3;
		[Tooltip("Цена первого платного реролла в монетах катки")]
		[Min(0)] [SerializeField] int startPrice = 100;
		[Tooltip("Рост цены за каждый платный реролл: startPrice * growth^paid")]
		[Min(1f)] [SerializeField] float priceGrowth = 1.5f;
		[Tooltip("Цена округляется до кратной этому шагу (1 — без шага)")]
		[Min(1)] [SerializeField] int priceRoundStep = 5;
		[Tooltip("Потолок цены — защита от переполнения на длинных катках")]
		[Min(0)] [SerializeField] int maxPrice = 99999;

		public int FreeRerollsPerMatch => freeRerollsPerMatch;

		// В отличие от сундуков (ChestsSettings.GetBasePrice усекает сам pow до int) округляется итоговая цена.
		// Math.Round с AwayFromZero: у Mathf.Round банковское округление (112.5 → 112).
		public int GetPrice(int paidCount) {
			int step = Mathf.Max(1, priceRoundStep);
			double raw = startPrice * Math.Pow(priceGrowth, Mathf.Max(0, paidCount));
			if (raw >= maxPrice)
				return maxPrice;
			int price = (int)Math.Round(raw / step, MidpointRounding.AwayFromZero) * step;
			return Mathf.Clamp(price, 0, maxPrice);
		}
	}
}
