using Gameplay.LevelUpgrades;
using UnityEngine;
using UnityEngine.UI;

// Узел дерева апгрейдов: иконка + три состояния (куплен / доступен / заблокирован).
// Клик открывает UpgradeInfoWindow — в том числе на заблокированном узле,
// чтобы игрок видел требования.
public class UpgradeNodeView : MonoBehaviour {
	[SerializeField] Image icon;
	[SerializeField] GameObject lockOverlay;
	[SerializeField] GameObject checkmark;
	[SerializeField] GameObject highlightFrame;
	[SerializeField] Color lockedTint = new Color(0.45f, 0.45f, 0.45f);

	// Id якоря туториала для узла конкретного апгрейда (реплика «купи вот это»)
	public static string TutorialAnchorId(string upgradeId) => "upgrade." + upgradeId;

	private string levelId;
	private LevelUpgradeEntry entry;
	private Gameplay.Tutorial.TutorialUiAnchor tutorialAnchor;

	public LevelUpgradeEntry Entry => entry;

	public void Init(string levelId, LevelUpgradeEntry entry) {
		this.levelId = levelId;
		this.entry = entry;
		icon.sprite = entry.Icon;
		icon.enabled = entry.Icon != null;

		// Узлы пулятся и меняют апгрейд — id якоря переназначается на каждый Init
		// (компонент на префаб вешает Tools/Tutorial/Build Hub Tutorial Steps)
		if (tutorialAnchor == null)
			tutorialAnchor = GetComponent<Gameplay.Tutorial.TutorialUiAnchor>();
		if (tutorialAnchor != null)
			tutorialAnchor.SetId(TutorialAnchorId(entry.Id));

		Refresh();
	}

	public void Refresh() {
		if (entry == null)
			return;

		bool purchased = LevelUpgradesManager.IsPurchased(levelId, entry.Id);
		bool available = !purchased && LevelUpgradesManager.CanPurchase(levelId, entry.Id);

		icon.color = purchased || available ? Color.white : lockedTint;
		if (lockOverlay != null)
			lockOverlay.SetActive(!purchased && !available);
		if (checkmark != null)
			checkmark.SetActive(purchased);
		if (highlightFrame != null)
			highlightFrame.SetActive(available);
	}

	// Вызывается кнопкой узла (onClick в префабе).
	public void OnClick() {
		if (entry == null)
			return;
		PlayerLocationUIController.Instance.ShowWindow(WindowType.UpgradeInfoWindow, new UpgradeInfoData(levelId, entry.Id));
	}
}
