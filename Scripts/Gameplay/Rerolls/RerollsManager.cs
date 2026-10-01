using UnityEngine;

namespace Gameplay.Rerolls {
	// Состояние рероллов катки (паттерн RelicsManager): общий пул бесплатных рероллов и общий счётчик
	// платных, от которого растёт цена. ResetRun вызывается из GameController.Awake и ResetLevel;
	// «Продолжить» после победы сцену не перезагружает — пул и лестница цен переживают.
	// Об изменениях сообщает InGameEvents.OnRerollsChanged — окна по нему перерисовывают кнопку.
	public static class RerollsManager {
		private static RerollSettings settings;

		public static int FreeLeft { get; private set; }
		public static int PaidCount { get; private set; }

		private static RerollSettings Settings {
			get {
				if (settings == null) {
					Debug.LogWarning("[RerollsManager] RerollSettings не подвязан в GameConfigs — работаю на дефолтах (Tools/Rerolls/Create Default Settings).");
					settings = ScriptableObject.CreateInstance<RerollSettings>();
				}
				return settings;
			}
		}

		public static void Init(RerollSettings rerollSettings) {
			settings = rerollSettings;
		}

		public static void ResetRun() {
			FreeLeft = Settings.FreeRerollsPerMatch;
			PaidCount = 0;
			InGameEventManager.SendEvent(InGameEvents.OnRerollsChanged);
		}

		// Цена следующего платного реролла (без учёта реликвий — их накладывает потребитель).
		public static int GetCurrentPrice() {
			return Settings.GetPrice(PaidCount);
		}

		public static bool ConsumeFree() {
			if (FreeLeft <= 0)
				return false;
			FreeLeft--;
			InGameEventManager.SendEvent(InGameEvents.OnRerollsChanged);
			return true;
		}

		// Золото списывает кнопка (PurchaseButton) — здесь только сдвиг лестницы цен.
		public static void RegisterPaid() {
			PaidCount++;
			InGameEventManager.SendEvent(InGameEvents.OnRerollsChanged);
		}

		// Задел под реликвии/апгрейды острова/читы.
		public static void AddFree(int amount) {
			if (amount <= 0)
				return;
			FreeLeft += amount;
			InGameEventManager.SendEvent(InGameEvents.OnRerollsChanged);
		}
	}
}
