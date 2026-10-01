using Cysharp.Threading.Tasks;
using Gameplay.RestorationSystem;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Окно заданий хаба: карточки ВСЕХ зон (RegionPanel) — сначала в работе, потом восстановленные,
// внизу заблокированные с подписью, когда откроются. Выполнение шага из окна: кристалл летит из счётчика
// окна в цену задания (CrystalFlight), затем ProgressStep.CompleteWithEffects и окно закрывается —
// экшены шага играют в хабе. Префаб собирает Tools/UI/Build Quests Window.
public class QuestsWindow : AnimationWindow {
	[SerializeField] RegionPanel regionPanelPrefab;
	[SerializeField] Transform regionsContainer;
	[SerializeField] ItemCounterPanel crystalsCounter;
	[SerializeField] ScrollRect scrollRect;
	[SerializeField] CrystalFlight.Settings flight = new CrystalFlight.Settings();

	private readonly List<RegionPanel> regions = new List<RegionPanel>();
	private bool completing;

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);

		completing = false;
		UpdateState();
		// В начало списка. Не через verticalNormalizedPosition: layout карточек ещё не построен,
		// и ScrollRect считает смещение от нулевой высоты контента — уезжает на высоту вьюпорта
		if (scrollRect != null) {
			scrollRect.velocity = Vector2.zero;
			scrollRect.content.anchoredPosition = Vector2.zero;
		}

		// Открыли по конкретному шагу — раскрываем его зону, подсвечиваем и подкручиваем к нему
		FocusOnStep(data as ProgressStep);
	}

	private void UpdateState() {
		var controller = PlayerProgressController.Instance;
		if (controller == null) {
			Debug.LogError("PlayerProgressController is null in QuestsWindow");
			return;
		}

		int i = 0;
		foreach (var region in SortRegions(controller.Regions)) {
			if (i >= regions.Count)
				regions.Add(Instantiate(regionPanelPrefab, regionsContainer));
			regions[i].gameObject.SetActive(true);
			regions[i].transform.SetSiblingIndex(i);
			regions[i].Init(this, region, controller.GetUnlockRule(region.Id));
			i++;
		}
		for (; i < regions.Count; i++)
			regions[i].gameObject.SetActive(false);
	}

	// В работе → восстановленные → заблокированные; внутри группы — порядок карты (список контроллера)
	private static List<ProgressRegion> SortRegions(List<ProgressRegion> source) {
		var result = new List<ProgressRegion>(source.Count);
		foreach (var state in new[] { ProgressRegion.RegionState.InProgress, ProgressRegion.RegionState.Repaired, ProgressRegion.RegionState.Destroyed }) {
			foreach (var region in source) {
				if (region.ResolveState() == state)
					result.Add(region);
			}
		}
		return result;
	}

	// ---------- Выполнение ----------

	public void RequestComplete(QuestPanel panel) {
		if (completing || panel == null || panel.Step == null || !panel.Step.CanComplete())
			return;
		completing = true;

		var from = crystalsCounter != null ? crystalsCounter.Icon : null;
		var to = panel.CurrencyIcon != null ? panel.CurrencyIcon.rectTransform : null;
		var sprite = panel.CurrencyIcon != null ? panel.CurrencyIcon.sprite : null;
		var step = panel.Step;
		CrystalFlight.Fly(from, to, sprite, flight, gameObject, () => {
			completing = false;
			step.CompleteWithEffects(); // списание, подлёт камеры и экшены шага — в хабе
			Hide();
		});
	}

	// ---------- Фокус на шаге ----------

	private void FocusOnStep(ProgressStep step) {
		if (step == null)
			return;

		foreach (var regionPanel in regions) {
			if (!regionPanel.gameObject.activeSelf)
				continue;
			QuestPanel panel = regionPanel.FindPanel(step);
			if (panel == null)
				continue;

			regionPanel.SetExpanded(true, true);
			panel.SetHighlighted(true);
			ScrollToPanelDeferred((RectTransform)panel.transform).Forget();
			return;
		}
	}

	// Панели только что пересобраны: вложенные layout group + ContentSizeFitter укладываются пару кадров,
	// сразу скроллить — попасть в старые ректы
	private async UniTaskVoid ScrollToPanelDeferred(RectTransform panel) {
		await UniTask.DelayFrame(2, PlayerLoopTiming.LastPostLateUpdate);
		if (this == null || !gameObject.activeInHierarchy || panel == null)
			return;
		ScrollToPanel(panel);
	}

	// Центрирует панель во вьюпорте; считает по краям ректов в локальных координатах контента
	private void ScrollToPanel(RectTransform panel) {
		if (scrollRect == null || panel == null)
			return;

		RectTransform content = scrollRect.content;
		LayoutRebuilder.ForceRebuildLayoutImmediate(content);
		Canvas.ForceUpdateCanvases();

		RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
		Vector2 panelCenter = content.InverseTransformPoint(panel.TransformPoint(panel.rect.center));

		float contentHeight = content.rect.height;
		float viewportHeight = viewport.rect.height;
		if (contentHeight > viewportHeight) {
			float fromTop = content.rect.yMax - panelCenter.y;
			float scrolled = Mathf.Clamp(fromTop - viewportHeight * 0.5f, 0f, contentHeight - viewportHeight);
			scrollRect.verticalNormalizedPosition = 1f - scrolled / (contentHeight - viewportHeight);
		}
	}
}
