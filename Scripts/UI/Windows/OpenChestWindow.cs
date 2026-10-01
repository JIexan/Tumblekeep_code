using Assets.SimpleLocalization;
using DG.Tweening;
using Gameplay.Relics;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Окно награды сундука. data = ChestTierConfig открытого сундука (таблицы предметов/реликвий тира);
// без data (кнопка «Test open chest») — тир Bronze. Награда роллится ДО анимации, лента
// (ChestRouletteStrip) крутит предметы и реликвии тира и останавливается на победителе,
// затем под лентой появляются название с описанием и кнопка «Забрать».
// Пока окно открыто — timeScale = 0, все анимации на unscaled-времени.
public class OpenChestWindow : AnimationWindow {
	[SerializeField] ChestRouletteStrip roulette;
	[SerializeField] CanvasGroup rewardInfo;
	[SerializeField] TMP_Text nameText;
	[SerializeField] TMP_Text descriptionText;
	[SerializeField] GameObject claimButton;
	[SerializeField] CanvasGroup cg;
	[SerializeField] float infoFadeDuration = 0.25f;

	private ChestTierConfig tierConfig;
	// Заролленная до анимации награда; null => таблицы тира пусты, выдавать нечего
	private ChestReward pendingReward;
	// Пул «фоновых» ячеек ленты: предметы тира и реликвии, которые сейчас могут выпасть
	private readonly List<ChestItemDrop> spinItems = new List<ChestItemDrop>();
	private readonly List<RelicDefinition> spinRelics = new List<RelicDefinition>();

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);

		tierConfig = data as ChestTierConfig;
		if (tierConfig == null) {
			var settings = GameConfigs.Instance != null ? GameConfigs.Instance.ChestsSettings : null;
			tierConfig = settings != null ? settings.GetTier(ChestTier.Bronze) : null;
		}
		if (tierConfig == null)
			Debug.LogError("[OpenChestWindow] Нет конфига тира сундука — проверь ChestsSettings в GameConfigs");

		pendingReward = tierConfig?.RollReward();
		Time.timeScale = 0f;
		StartSpin();
	}

	private void StartSpin() {
		BuildSpinPool();
		cg.blocksRaycasts = false;
		rewardInfo.alpha = 0f;
		rewardInfo.gameObject.SetActive(false);
		claimButton.SetActive(false);

		var winner = pendingReward != null ? ToEntry(pendingReward) : null;
		roulette.Spin(PickSpinEntry, winner, OnSpinFinished);
	}

	// В ленте едут и предметы, и реликвии — раньше крутились только предметы, и выпавшая
	// реликвия появлялась «из ниоткуда». Реликвии берём только те, что реально могут выпасть.
	private void BuildSpinPool() {
		spinItems.Clear();
		spinRelics.Clear();
		if (tierConfig == null)
			return;
		foreach (var drop in tierConfig.items) {
			// Залоченные метой свитки в ленте не показываем — они и выпасть не могут (RollItem)
			if (drop != null && drop.weight > 0f && Gameplay.Unlocks.ContentUnlocksManager.IsSpellUnlocked(drop.itemType))
				spinItems.Add(drop);
		}
		var catalog = GameConfigs.Instance != null ? GameConfigs.Instance.RelicsSettings : null;
		if (catalog == null)
			return;
		foreach (var entry in tierConfig.relics) {
			if (entry == null || entry.weight <= 0f || string.IsNullOrEmpty(entry.relicId))
				continue;
			var relic = catalog.GetRelic(entry.relicId);
			if (relic != null && RelicsManager.CanDrop(relic))
				spinRelics.Add(relic);
		}
	}

	// Доля реликвий в ленте ≈ реальному шансу тира, чтобы лента не врала про редкость
	private ChestRouletteEntry PickSpinEntry() {
		bool pickRelic = spinRelics.Count > 0
			&& (spinItems.Count == 0 || UnityEngine.Random.value < tierConfig.relicChance);
		if (pickRelic)
			return ToEntry(spinRelics[UnityEngine.Random.Range(0, spinRelics.Count)]);
		if (spinItems.Count == 0)
			return null;
		var drop = spinItems[UnityEngine.Random.Range(0, spinItems.Count)];
		return ToEntry(drop.itemType, drop.RollCount(), drop);
	}

	private static ChestRouletteEntry ToEntry(ChestReward reward) {
		return reward.IsRelic ? ToEntry(reward.Relic) : ToEntry(reward.Item, reward.Count, null);
	}

	private static ChestRouletteEntry ToEntry(RelicDefinition relic) {
		var raritySettings = GameConfigs.Instance != null && GameConfigs.Instance.RarityConfig != null
			? GameConfigs.Instance.RarityConfig.Get(relic.Rarity) : null;
		return new ChestRouletteEntry {
			Icon = relic.Icon,
			Count = 0,
			FrameColor = raritySettings != null ? raritySettings.c : Color.white,
			NameKey = relic.NameKey,
			DescKey = relic.DescKey,
			Source = relic,
		};
	}

	private static ChestRouletteEntry ToEntry(ItemType item, int count, object source) {
		return new ChestRouletteEntry {
			Icon = ItemIconsManager.Instance.GetIcon(item),
			Count = count,
			FrameColor = Color.white,
			NameKey = ItemLocalizationKeys.GetNameKey(item),
			DescKey = ItemLocalizationKeys.GetDescKey(item),
			Source = source,
		};
	}

	private void OnSpinFinished() {
		var winner = roulette.WinnerCell != null ? roulette.WinnerCell.Entry : null;
		if (winner != null) {
			nameText.text = LocalizationManager.Localize(winner.NameKey);
			descriptionText.text = LocalizationManager.Localize(winner.DescKey);
			rewardInfo.gameObject.SetActive(true);
			rewardInfo.alpha = 0f;
			rewardInfo.DOFade(1f, infoFadeDuration).SetUpdate(true);
		}

		claimButton.SetActive(true);
		claimButton.transform.localScale = Vector3.one * 0.8f;
		claimButton.transform.DOScale(1f, infoFadeDuration).SetEase(Ease.OutBack).SetUpdate(true);
		cg.blocksRaycasts = true;
	}

	// Переролл целиком (и реликвия, и предмет) с перезапуском ленты. В UI не привязан —
	// оставлен для читов/отладки.
	public void Reroll() {
		pendingReward = tierConfig?.RollReward();
		StartSpin();
	}

	public void Claim() {
		if (roulette.IsSpinning)
			return;
		if (pendingReward != null) {
			if (pendingReward.IsRelic) {
				// Реликвия — состояние катки, профиль сохранений не трогаем
				RelicsManager.Add(pendingReward.Relic);
			} else {
				// Кристаллы сундука — в HUD-счётчик катки ДО AddItem: его событие OnItemCountChanged и обновит счётчик
				if (pendingReward.Item == ItemType.Crystal)
					GameController.Instance?.MatchRewards?.AddChestCrystals(pendingReward.Count);
				SavesManager.Profile.AddItem(pendingReward.Item, pendingReward.Count);
			}
			pendingReward = null;
		}
		Hide();
	}

	public override void Hide() {
		// base.Hide игнорирует вызов до CanBeClosed — паузу снимаем только вместе с реальным закрытием
		if (!CanBeClosed)
			return;
		base.Hide();
		Time.timeScale = 1f;
	}
}
