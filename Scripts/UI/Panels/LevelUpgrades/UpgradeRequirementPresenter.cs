using Assets.SimpleLocalization;
using Gameplay.LevelUpgrades;
using Gameplay.RestorationSystem;
using System.Collections.Generic;
using UnityEngine;

// Единственное место, где тип требования превращается в отображаемые данные.
// Новый тип требования — новый case здесь.
public static class UpgradeRequirementPresenter {
	public enum RequirementKind {
		Unknown,
		Item,
		Region,
		IslandLevel,
		Chain
	}

	public struct RequirementViewModel {
		public RequirementKind Kind;
		public Sprite ItemIcon; // только для Kind.Item, остальные виды берут спрайт из префаба строки
		public string Text;
		public int Current;
		public int Required;
		public string ProgressSuffix; // "%" у процентных требований
		public bool Met;
	}

	public static RequirementViewModel Map(BaseUpgradeRequirement requirement, string levelId) {
		var (current, required) = requirement.GetProgress(levelId);
		var viewModel = new RequirementViewModel {
			Kind = RequirementKind.Unknown,
			Text = requirement.GetType().Name,
			Current = current,
			Required = required,
			Met = requirement.IsMet(levelId)
		};

		switch (requirement) {
			case ItemUpgradeRequirement item:
				viewModel.Kind = RequirementKind.Item;
				viewModel.ItemIcon = GetItemIcon(item.Item);
				viewModel.Text = LocalizationManager.Localize("ItemName_" + item.Item);
				break;

			case RegionProgressUpgradeRequirement region:
				viewModel.Kind = RequirementKind.Region;
				viewModel.Text = LocalizeFormat("UpgradeReq_Region", LocalizationManager.Localize("RegionTitle" + region.RegionId.ToSaveId()));
				viewModel.ProgressSuffix = "%";
				break;

			case IslandLevelUpgradeRequirement:
				viewModel.Kind = RequirementKind.IslandLevel;
				viewModel.Text = LocalizationManager.Localize("UpgradeReq_IslandLevel");
				break;

			case PurchasedUpgradesUpgradeRequirement chain:
				viewModel.Kind = RequirementKind.Chain;
				viewModel.Text = LocalizeFormat("UpgradeReq_Chain", JoinUpgradeNames(chain.UpgradeIds));
				break;
		}

		return viewModel;
	}

	// Напрямую из GameConfigs: в хабе нет сценового синглтона ItemIconsManager.
	private static Sprite GetItemIcon(ItemType item) {
		var itemSettings = GameConfigs.Instance != null ? GameConfigs.Instance.ItemSettings : null;
		var data = itemSettings != null ? itemSettings.GetItemData(item) : null;
		return data != null ? data.icon : null;
	}

	private static string JoinUpgradeNames(List<string> upgradeIds) {
		if (upgradeIds == null || upgradeIds.Count == 0)
			return "";
		var names = new List<string>(upgradeIds.Count);
		foreach (var id in upgradeIds)
			names.Add(LocalizationManager.Localize("UpgradeName_" + id));
		return string.Join(", ", names);
	}

	// Отсутствующий ключ локализации возвращается как "{key}" — string.Format на нём
	// кинул бы FormatException, поэтому форматируем только при наличии "{0}".
	private static string LocalizeFormat(string key, params object[] args) {
		string format = LocalizationManager.Localize(key);
		return format.Contains("{0}") ? string.Format(format, args) : format;
	}
}
