#if AUTOPLAY
using System;
using System.Collections.Generic;
using Gameplay.Cutscenes;
using UnityEngine;

namespace Autoplay {
	/// <summary>
	/// Закрывает за бота всплывающие окна: награда сундука (рулетка + Claim), апгрейд замка/башни
	/// (случайная карточка), торговец (случайный оффер по карману или закрыть), катсцены (Skip).
	/// Тикает каждый кадр (окна живут на timeScale=0), действует один раз на показ окна и только
	/// после CanBeClosed — иначе UpgradePanel.OnButtonClick применит апгрейд, а окно не закроется.
	/// </summary>
	public class AutoplayWindowHandler {
		private readonly AutoplayBot bot;
		private readonly RunStats stats;

		private BaseWindow acted;
		private BaseWindow observed;
		private int observedFrames;

		public AutoplayWindowHandler(AutoplayBot bot, RunStats stats) {
			this.bot = bot;
			this.stats = stats;
		}

		public void Tick() {
			if (CutsceneController.IsPlaying) {
				var controller = CutsceneController.Instance;
				if (controller != null) {
					controller.Skip();
					bot.Log("cutscene skipped");
				}
				return;
			}

			var windows = bot.Windows;
			if (windows == null)
				return;
			if (acted != null && !acted.gameObject.activeSelf)
				acted = null;

			if (TryHandle(windows, WindowType.OpenChestWindow, HandleChest))
				return;
			if (TryHandle(windows, WindowType.UpgradeWindow, HandleUpgrade))
				return;
			TryHandle(windows, WindowType.MerchantWindow, HandleMerchant);
		}

		private bool TryHandle(WindowsManager windows, WindowType type, Func<AnimationWindow, bool> action) {
			if (!windows.IsWindowActive(type))
				return false;
			var window = windows.GetWindow(type) as AnimationWindow;
			if (window == null || acted == window)
				return true;

			// Пара кадров после показа + CanBeClosed: Show отработал, панели проинициализированы
			if (observed != window) {
				observed = window;
				observedFrames = 0;
			}
			observedFrames++;
			if (observedFrames < 2 || !window.CanBeClosed || window.IsClosing)
				return true;

			if (action(window))
				acted = window;
			return true;
		}

		private bool HandleChest(AnimationWindow window) {
			var chestWindow = window as OpenChestWindow;
			if (chestWindow == null)
				return false;
			// Лента крутится 4.5 c unscaled — глушим и сразу забираем награду
			var strip = chestWindow.GetComponentInChildren<ChestRouletteStrip>(true);
			if (strip != null)
				strip.Kill();
			chestWindow.Claim();
			bot.Log("chest reward claimed");
			return true;
		}

		private bool HandleUpgrade(AnimationWindow window) {
			var panels = window.GetComponentsInChildren<UpgradePanel>(false);
			if (panels == null || panels.Length == 0) {
				window.Hide();
				bot.Log("upgrade window without options closed");
				return true;
			}
			// Окно апгрейда во время открытия сундука — это апгрейд замка
			bool castle = bot.ChestBusy;
			var panel = panels[bot.Rng.Next(panels.Length)];
			panel.OnButtonClick();
			if (castle)
				stats.castleUpgrades++;
			bot.Log(castle ? "castle upgrade picked" : "tower upgrade picked (window)");
			return true;
		}

		private bool HandleMerchant(AnimationWindow window) {
			var merchant = window as MerchantWindow;
			if (merchant == null)
				return false;
			var affordable = new List<MerchantItemPanel>();
			foreach (var panel in merchant.GetComponentsInChildren<MerchantItemPanel>(false)) {
				if (panel.Offer != null && CoinsManager.Instance.CanPay(panel.Offer.Price))
					affordable.Add(panel);
			}
			if (affordable.Count > 0) {
				var pick = affordable[bot.Rng.Next(affordable.Count)];
				bot.Log($"merchant: buy {pick.Offer.ItemType} x{pick.Offer.Count} for {pick.Offer.Price}");
				pick.Purchase();
			} else {
				bot.Log("merchant: nothing affordable, closing");
				merchant.OnCloseButtonClick();
			}
			return true;
		}
	}
}
#endif
