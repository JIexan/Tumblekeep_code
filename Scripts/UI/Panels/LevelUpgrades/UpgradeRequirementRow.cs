using Gameplay.LevelUpgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Одна строка требования в инфо-окне апгрейда: иконка + текст + прогресс + чекмарк.
public class UpgradeRequirementRow : MonoBehaviour {
	[SerializeField] Image icon;
	[SerializeField] TMP_Text label;
	[SerializeField] TMP_Text progress;
	[SerializeField] GameObject checkmark;

	[Header("Иконки не-предметных требований")]
	[SerializeField] Sprite regionSprite;
	[SerializeField] Sprite islandLevelSprite;
	[SerializeField] Sprite chainSprite;

	[SerializeField] Color metColor = new Color(0.35f, 0.8f, 0.35f);
	[SerializeField] Color notMetColor = new Color(0.85f, 0.3f, 0.3f);

	private BaseUpgradeRequirement requirement;
	private string levelId;

	public void Init(BaseUpgradeRequirement requirement, string levelId) {
		this.requirement = requirement;
		this.levelId = levelId;
		Refresh();
	}

	public void Refresh() {
		if (requirement == null)
			return;

		var viewModel = UpgradeRequirementPresenter.Map(requirement, levelId);

		var sprite = viewModel.Kind switch {
			UpgradeRequirementPresenter.RequirementKind.Item => viewModel.ItemIcon,
			UpgradeRequirementPresenter.RequirementKind.Region => regionSprite,
			UpgradeRequirementPresenter.RequirementKind.IslandLevel => islandLevelSprite,
			UpgradeRequirementPresenter.RequirementKind.Chain => chainSprite,
			_ => null
		};
		icon.sprite = sprite;
		icon.enabled = sprite != null;

		label.text = viewModel.Text;
		progress.text = $"{viewModel.Current}/{viewModel.Required}{viewModel.ProgressSuffix}";
		progress.color = viewModel.Met ? metColor : notMetColor;

		if (checkmark != null)
			checkmark.SetActive(viewModel.Met);
	}
}
