using System;
using System.Collections.Generic;
using UnityEngine;

// Дроп, покупка и открытие наградных сундуков. Все настройки (префабы тиров, цены, таблицы дропа)
// живут в ChestsSettings (GameConfigs.Instance.ChestsSettings).
// Флоу: DropChest (зачищенная волна / чит) → клик по сундуку (GameController.OnClick) → панель покупки →
// списание монет → анимация открытия → окно рулетки предметов/реликвий или апгрейда замка →
// сундук удаляется после закрытия окна.
public class ChestsManager : MonoBehaviour
{
	private readonly List<RewardChest> activeChests = new List<RewardChest>();
	// Счётчик открытых сундуков общий на все тиры — от него растёт цена (ChestsSettings.priceExponent).
	private int opennedChests = 0;
	// Сундук, который сейчас открывается или показывает окно награды. Окно одно — вторая покупка
	// в это время перезапустила бы ролл, поэтому клики по другим сундукам до закрытия окна блокируются.
	private RewardChest openingChest;

	private static ChestsSettings Settings => GameConfigs.Instance != null ? GameConfigs.Instance.ChestsSettings : null;

	// Сундук открывается / показывает окно награды — новые покупки блокируются
	public bool IsBusy => openingChest != null;
	public IReadOnlyList<RewardChest> ActiveChests => activeChests;

	// Тир выбирается по весам с учётом удачи замка (ChestsSettings.RollTier).
	public void DropChest(Vector3 position, Vector3 direction) {
		var settings = Settings;
		if (settings == null) {
			Debug.LogError("[ChestsManager] ChestsSettings не подключён в GameConfigs — сундук не выпал");
			return;
		}
		DropChest(position, direction, settings.RollTier());
	}

	public void DropChest(Vector3 position, Vector3 direction, ChestTier tier) {
		var settings = Settings;
		if (settings == null)
			return;
		DropChest(position, direction, settings.GetTier(tier));
	}

	public void DropChest(Vector3 position, Vector3 direction, ChestTierConfig tierConfig) {
		if (tierConfig == null || tierConfig.prefab == null) {
			Debug.LogError($"[ChestsManager] У тира {(tierConfig != null ? tierConfig.tier.ToString() : "null")} нет префаба — сундук не выпал");
			return;
		}
		position.y = Settings.DropHeight;
		RewardChest newChest = Instantiate(tierConfig.prefab, position, Quaternion.identity);
		if (direction.sqrMagnitude > 0.0001f)
			newChest.transform.forward = direction;
		newChest.BeginWorldIdle();
		activeChests.Add(newChest);
		InGameEventManager.SendEvent(InGameEvents.OnChestDropped, newChest);
	}

	public void OnChestClick(RewardChest chest) {
		if (chest == null || chest.IsOpening || openingChest != null)
			return;
		var settings = Settings;
		var tierConfig = settings != null ? settings.GetTier(chest.Tier) : null;
		if (tierConfig == null) {
			Debug.LogError($"[ChestsManager] В ChestsSettings нет тира {chest.Tier}");
			return;
		}

		int price = GetChestPrice(tierConfig);
		GameController.Instance.uiController.ShowChestPanel(chest, price, () => TryBuyAndOpen(chest));
		InGameEventManager.SendEvent(InGameEvents.OnChestClicked, chest);
	}

	/// <summary>
	/// Покупка и открытие сундука (то, что делает кнопка «купить» на панели сундука).
	/// Возвращает false, если сундук занят, нет настроек тира или не хватает монет.
	/// </summary>
	public bool TryBuyAndOpen(RewardChest chest) {
		if (chest == null || chest.IsOpening || openingChest != null)
			return false;
		var settings = Settings;
		var tierConfig = settings != null ? settings.GetTier(chest.Tier) : null;
		if (tierConfig == null)
			return false;

		int price = GetChestPrice(tierConfig);
		if (!CoinsManager.Instance.CanPay(price))
			return false;

		CoinsManager.Instance.Substract(price);
		// Раньше панель гасла сама, потому что сундук тут же уничтожался; теперь он ещё играет открытие.
		GameController.Instance.uiController.HideTowerPanel();
		activeChests.Remove(chest);
		openingChest = chest;
		opennedChests++;
		InGameEventManager.SendEvent(InGameEvents.OnChestOpenned);
		chest.Open(() => ShowReward(chest, tierConfig));
		return true;
	}

	// Окно показывается только после того, как доиграла анимация открытия; сундук стоит открытым,
	// пока окно не закроют, и удаляется в onAfterHide.
	private void ShowReward(RewardChest chest, ChestTierConfig tierConfig) {
		Action onHide = () => {
			if (openingChest == chest)
				openingChest = null;
			if (chest != null)
				Destroy(chest.gameObject);
		};
		var ui = GameController.Instance.uiController;
		if (UnityEngine.Random.value < tierConfig.castleUpgradeChance)
			ui.ShowWindow(WindowType.UpgradeWindow, Castle.Instance, null, onHide);
		else
			ui.ShowWindow(WindowType.OpenChestWindow, tierConfig, null, onHide);
	}

	public int GetChestPrice(ChestTierConfig tierConfig) {
		int price = Settings.GetBasePrice(tierConfig, opennedChests);
		return Gameplay.Relics.RelicsManager.ApplyPrice(Gameplay.Relics.RelicValue.ChestPrice, price);
	}
}
