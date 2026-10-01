using DG.Tweening;
using Gameplay.TasksSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Текущий множитель награды катки (за выполненные доп. таски): чип в шапке бонусных тасков HUD и на кнопке «Уплыть».
// Сам следит за тасками — выплату считает GameController.BankRewards по той же формуле.
public class RewardMultiplierLabel : MonoBehaviour {
	[SerializeField] TMP_Text label;
	[SerializeField] Graphic back;
	[SerializeField] Color baseTextColor = Color.white;
	[SerializeField] Color boostedTextColor = Color.white;
	[SerializeField] Color baseBackColor = new Color(1f, 1f, 1f, 0.12f);
	[SerializeField] Color boostedBackColor = new Color(0.8745f, 0.7176f, 0.2078f, 1f);

	private float shownMultiplier = -1f;

	private void OnEnable() {
		InGameEventManager.Subscribe(InGameEvents.OnTasksStateChanged, OnTasksStateChanged);
		Refresh(false);
	}

	private void OnDisable() {
		InGameEventManager.Unsubscribe(InGameEvents.OnTasksStateChanged, OnTasksStateChanged);
		transform.DOKill(true);
	}

	private void OnTasksStateChanged(object _) {
		Refresh(true);
	}

	public void Refresh(bool animate) {
		var tasks = LevelTasksManager.Instance.LevelTasksCollection;
		float multiplier = MatchRewards.GetMultiplier(tasks != null ? tasks.SecondaryCompletedCount : 0);
		bool boosted = multiplier > MatchRewards.GetMultiplier(0);

		label.text = $"x{multiplier:0.#}"; // формат как в VictoryWindow
		label.color = boosted ? boostedTextColor : baseTextColor;
		if (back != null)
			back.color = boosted ? boostedBackColor : baseBackColor;

		if (animate && shownMultiplier >= 0f && multiplier > shownMultiplier) {
			transform.DOKill(true);
			transform.DOPunchScale(Vector3.one * 0.35f, 0.4f, 6).SetUpdate(true);
		}
		shownMultiplier = multiplier;
	}
}
