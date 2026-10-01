using System;
using Assets.SimpleLocalization;
using DG.Tweening;
using Gameplay.Unlocks;
using TMPro;
using UnityEngine;

// Попап «контент открыт» (хаб): показывается после завершения шага восстановления с Unlock*Action —
// после катсцены шага, если она есть. Макет — окно сундука без ленты: одна ячейка (иконка + рамка
// цвета рарности), имя и описание из книги, подпись «добавлено в пул», кнопка OK.
// data = Gameplay.Unlocks.ContentUnlock. Собирается Tools/Unlocks/Build Content Unlock Window.
public class ContentUnlockWindow : AnimationWindow {
	[SerializeField] TMP_Text titleText;
	[SerializeField] LocalizedTextTMP titleLoc; // ключ шапки зависит от вида анлока (находка / новая зона)
	[SerializeField] ChestRouletteCell cell;
	[SerializeField] CanvasGroup rewardInfo;
	[SerializeField] TMP_Text nameText;
	[SerializeField] TMP_Text descriptionText;
	[SerializeField] TMP_Text noteText;
	[SerializeField] GameObject okButton;
	[SerializeField] CanvasGroup cg;
	[SerializeField] float infoFadeDuration = 0.25f;

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);

		var view = data is ContentUnlock unlock ? Resolve(unlock) : null;
		if (view == null) {
			Debug.LogWarning("[ContentUnlockWindow] Нет данных анлока — окно закрывается.");
			view = new View { NameKey = "", DescKey = "", NoteKey = "" };
		}

		if (titleLoc != null)
			titleLoc.LocalizationKey = view.TitleKey;
		if (titleText != null)
			titleText.text = LocalizationManager.Localize(view.TitleKey);
		if (cell != null) {
			cell.gameObject.SetActive(view.Icon != null);
			if (view.Icon != null)
				cell.Set(new ChestRouletteEntry { Icon = view.Icon, Count = view.Count, FrameColor = view.FrameColor, NameKey = view.NameKey, DescKey = view.DescKey });
		}
		nameText.text = LocalizationManager.Localize(view.NameKey);
		descriptionText.text = LocalizationManager.Localize(view.DescKey);
		if (noteText != null)
			noteText.text = LocalizationManager.Localize(view.NoteKey);

		if (rewardInfo != null) {
			rewardInfo.alpha = 0f;
			rewardInfo.DOFade(1f, infoFadeDuration).SetUpdate(true);
		}
		if (okButton != null) {
			okButton.transform.localScale = Vector3.one * 0.8f;
			okButton.transform.DOScale(1f, infoFadeDuration).SetEase(Ease.OutBack).SetUpdate(true);
		}
		if (cg != null)
			cg.blocksRaycasts = true;
	}

	// Кнопка OK (persistent listener в префабе)
	public void Ok() {
		Hide();
	}

	private class View {
		public Sprite Icon;
		public int Count; // «×N» на ячейке (показывается от 2), только для предметов
		public Color FrameColor = Color.white;
		public string TitleKey = "ContentUnlockTitle";
		public string NameKey;
		public string DescKey;
		public string NoteKey;
	}

	// Тексты — те же ключи, что у книги и окна сундука; иконка предмета напрямую из GameConfigs
	// (в хабе нет сценового ItemIconsManager).
	private static View Resolve(ContentUnlock unlock) {
		switch (unlock.Kind) {
			case UnlockKind.Relic: {
				var catalog = GameConfigs.Instance != null ? GameConfigs.Instance.RelicsSettings : null;
				var relic = catalog != null ? catalog.GetRelic(unlock.Key) : null;
				if (relic == null)
					return null;
				var rarity = GameConfigs.Instance.RarityConfig != null ? GameConfigs.Instance.RarityConfig.Get(relic.Rarity) : null;
				return new View {
					Icon = relic.Icon,
					FrameColor = rarity != null ? rarity.c : Color.white,
					NameKey = relic.NameKey,
					DescKey = relic.DescKey,
					NoteKey = "ContentUnlock_PoolNote",
				};
			}
			case UnlockKind.Spell: {
				var settings = GameConfigs.Instance != null ? GameConfigs.Instance.ItemSettings : null;
				var item = settings != null && Enum.TryParse(unlock.Key, out ItemType type) ? settings.GetItemData(type) : null;
				return new View {
					Icon = item != null ? item.icon : null,
					NameKey = "Book_" + unlock.Key + "_Name",
					DescKey = "Book_" + unlock.Key + "_Desc",
					NoteKey = "ContentUnlock_PoolNote",
				};
			}
			case UnlockKind.Faction:
				return new View { NameKey = "FactionName_" + unlock.Key, DescKey = "ContentUnlock_Faction_Desc", NoteKey = "ContentUnlock_EnemyNote" };
			case UnlockKind.Tier:
				return new View { NameKey = "UnitTierName_" + unlock.Key, DescKey = "ContentUnlock_Tier_Desc", NoteKey = "ContentUnlock_EnemyNote" };
			case UnlockKind.Region: // key = имя RegionId (см. PlayerProgressController.AnnounceRegionUnlock)
				return new View { TitleKey = "ContentUnlock_RegionTitle", NameKey = "RegionTitle" + unlock.Key, DescKey = "ContentUnlock_Region_Desc", NoteKey = "ContentUnlock_RegionNote" };
			case UnlockKind.Level: // key = Level.Id — на карте открылся новый остров
				return new View { TitleKey = "ContentUnlock_LevelTitle", NameKey = "LevelName_" + unlock.Key, DescKey = "ContentUnlock_Level_Desc", NoteKey = "ContentUnlock_LevelNote" };
			case UnlockKind.Item: { // key = "ItemType:count" (ContentUnlock.ForItem) — предметы уже начислены, окно только сообщает
				var parts = unlock.Key.Split(':');
				if (parts.Length != 2 || !Enum.TryParse(parts[0], out ItemType type) || !int.TryParse(parts[1], out int count))
					return null;
				var settings = GameConfigs.Instance != null ? GameConfigs.Instance.ItemSettings : null;
				var item = settings != null ? settings.GetItemData(type) : null;
				return new View {
					Icon = item != null ? item.icon : null,
					Count = count,
					NameKey = "ItemName_" + type,
					DescKey = "ContentUnlock_" + type + "_Desc",
					NoteKey = "ContentUnlock_ItemNote",
				};
			}
			default:
				return null;
		}
	}
}
