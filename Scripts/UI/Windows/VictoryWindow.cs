using DG.Tweening;
using Gameplay.IslandExperience;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct VictoryWindowData
{
	public string LevelId;
	public int BankedXP;        // суммарно выплаченный за катку island XP (уже в сейве)
	public int AccruedCrystals; // реально добытые кристаллы (без множителя)
	public int BankedCrystals;  // итог с множителем (уже в сейве)
	public int SecondaryCount;  // выполнено доп. тасков (0..2)
}

public class VictoryWindow : AnimationWindow
{
	[SerializeField] Animator anim;

	[Header("Island XP")]
	[SerializeField] TMP_Text txtIslandLevel;
	[SerializeField] Image xpProgressFill;
	[SerializeField] TMP_Text txtXPProgress;

	[Header("Crystals")]
	[SerializeField] GameObject crystalsPanel;
	[SerializeField] TMP_Text txtCrystals;
	[SerializeField] TMP_Text txtMultiplier;

	[Header("Animation")]
	[SerializeField] float xpFillDuration = 1.5f;
	[SerializeField] float crystalsCountDuration = 0.4f;

	private VictoryWindowData data;
	private int xpTotalBefore;
	private int xpTotalAfter;
	private int shownCrystals;
	private Tween xpTween;
	private Tween crystalsTween;
	private Tween multiplierTween;

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);

		if (data is VictoryWindowData victoryData) {
			this.data = victoryData;
			Time.timeScale = 0f;
			gameObject.SetActive(true);
			anim.SetTrigger("Play");

			KillTweens();

			// Стартовое состояние XP: как было до наград этой катки.
			if (!string.IsNullOrEmpty(victoryData.LevelId)) {
				xpTotalAfter = IslandExperienceManager.GetTotalXP(victoryData.LevelId);
				xpTotalBefore = Mathf.Max(0, xpTotalAfter - victoryData.BankedXP);
			} else {
				xpTotalAfter = xpTotalBefore = 0;
			}
			UpdateXPBar(xpTotalBefore);

			crystalsPanel.gameObject.SetActive(false);
		}
	}

	// Анимационные события victory.anim (бывшие звёзды):
	// 0 — старт анимации XP-бара и показ добытых кристаллов (x1),
	// 1, 2 — апгрейд множителя и кристаллов до фактических значений.
	public void OnStarShow(int i) {
		if (i == 0) {
			crystalsPanel.gameObject.SetActive(true);
			SetCrystals(data.AccruedCrystals);
			SetMultiplierText(MatchRewards.GetMultiplier(0));
			StartXPBarAnimation();
		} else if (data.SecondaryCount >= i) {
			AnimateRewardStep(i);
		}
	}

	private void StartXPBarAnimation() {
		if (xpTotalAfter <= xpTotalBefore)
			return;

		xpTween?.Kill();
		xpTween = DOVirtual.Float(xpTotalBefore, xpTotalAfter, xpFillDuration, v => UpdateXPBar(Mathf.RoundToInt(v)))
			.SetEase(Ease.InOutSine)
			.SetUpdate(true);
	}

	// Прогресс-бар пересчитывается от виртуального totalXP: при пересечении порога
	// уровня заполнение само "сбрасывается" и ползёт заново — каскад левелапов бесплатно.
	private void UpdateXPBar(int totalXP) {
		int level = IslandExperienceManager.GetLevelForTotalXP(totalXP);
		int into = IslandExperienceManager.GetXPIntoLevel(totalXP);
		int toNext = IslandExperienceManager.GetXPToNextLevel(totalXP);

		if (txtIslandLevel != null)
			txtIslandLevel.text = level.ToString();

		if (toNext <= 0) {
			if (xpProgressFill != null)
				xpProgressFill.fillAmount = 1f;
			if (txtXPProgress != null)
				txtXPProgress.text = "MAX";
		} else {
			if (xpProgressFill != null)
				xpProgressFill.fillAmount = into / (float)toNext;
			if (txtXPProgress != null)
				txtXPProgress.text = $"{into}/{toNext}";
		}
	}

	private void AnimateRewardStep(int step) {
		float multiplier = MatchRewards.GetMultiplier(step);

		// Последний применимый шаг показывает ровно то, что выплачено в профиль.
		int targetCrystals = step >= data.SecondaryCount
			? data.BankedCrystals
			: Mathf.RoundToInt(data.AccruedCrystals * multiplier);

		crystalsTween?.Kill();
		int from = shownCrystals;
		shownCrystals = targetCrystals;
		crystalsTween = DOVirtual.Float(from, targetCrystals, crystalsCountDuration, v => SetCrystalsText(Mathf.RoundToInt(v)))
			.SetUpdate(true);

		SetMultiplierText(multiplier);
		if (txtMultiplier != null) {
			multiplierTween?.Kill();
			txtMultiplier.transform.localScale = Vector3.one;
			multiplierTween = txtMultiplier.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f).SetUpdate(true);
		}
	}

	private void SetCrystals(int amount) {
		shownCrystals = amount;
		SetCrystalsText(amount);
	}

	private void SetCrystalsText(int amount) {
		if (txtCrystals != null)
			txtCrystals.text = $"+{amount}";
	}

	private void SetMultiplierText(float multiplier) {
		if (txtMultiplier != null)
			txtMultiplier.text = $"x{multiplier:0.#}";
	}

	private void KillTweens() {
		xpTween?.Kill();
		crystalsTween?.Kill();
		multiplierTween?.Kill();
		xpTween = crystalsTween = multiplierTween = null;
	}

	// Окно победы финальное: игрок уже нажал «Уплыть» в HUD, продолжить катку нельзя.
	public void OnMainMenu()
	{
		GameController.Instance.OnMainMenu();
		Hide();
	}

	override public void Hide()
	{
		KillTweens();
		Time.timeScale = 1f;
		base.Hide();
	}

	private void OnDestroy() {
		KillTweens();
	}
}
