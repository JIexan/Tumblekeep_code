using Assets.SimpleLocalization;
using TMPro;
using UnityEngine;

// Лесенка множителя награды в окне уровня: ступень i — множитель за i выполненных доп. тасков.
// Цифры берутся из MatchRewards, чтобы окно не разъехалось с фактической выплатой.
public class RewardMultiplierLadder : MonoBehaviour {
	[System.Serializable]
	private struct Step {
		public TMP_Text multiplier;
		public TMP_Text caption;
	}

	[SerializeField] Step[] steps;
	[SerializeField] string baseCaptionKey = "LevelInfo_RewardBase";
	[SerializeField] string bonusCaptionKey = "LevelInfo_RewardBonus"; // {0} — число доп. тасков

	private void OnEnable() {
		LocalizationManager.LocalizationChanged += Refresh;
		Refresh();
	}

	private void OnDisable() {
		LocalizationManager.LocalizationChanged -= Refresh;
	}

	public void Refresh() {
		for (int i = 0; i < steps.Length; i++) {
			if (steps[i].multiplier != null)
				steps[i].multiplier.text = $"x{MatchRewards.GetMultiplier(i):0.#}"; // формат как в VictoryWindow
			if (steps[i].caption != null)
				steps[i].caption.text = i == 0 ? LocalizationManager.Localize(baseCaptionKey) : LocalizationManager.Localize(bonusCaptionKey, i);
		}
	}
}
