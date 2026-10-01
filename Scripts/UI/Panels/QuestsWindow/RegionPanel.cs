using Assets.SimpleLocalization;
using DG.Tweening;
using Gameplay.LevelUpgrades;
using Gameplay.RestorationSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Карточка зоны хаба в окне заданий: название и состояние (в работе / восстановлена / заблокирована + когда
// откроется), прогресс-бар готовности в процентах с засечками порогов апгрейдов уровней, и фолд «Задания»
// с доступными шагами (только у зоны в работе). Префаб собирает Tools/UI/Build Quests Window.
public class RegionPanel : MonoBehaviour {
	[SerializeField] TMP_Text regionTitle;
	[SerializeField] TMP_Text stateText;
	[SerializeField] CanvasGroup group;
	[SerializeField] float lockedAlpha = 0.6f;

	[Header("Прогресс")]
	[SerializeField] Slider progressBar;
	[SerializeField] TMP_Text progressText;
	[SerializeField] RectTransform markersRoot;
	[SerializeField] RegionProgressMarker markerPrefab;

	[Header("Фолд заданий")]
	[SerializeField] GameObject foldRoot;
	[SerializeField] Button foldButton;
	[SerializeField] RectTransform foldArrow;
	[SerializeField] TMP_Text foldLabel;
	[SerializeField] Transform questsContainer;
	[SerializeField] QuestPanel questPanelPrefab;
	[SerializeField] float arrowRotateDuration = 0.15f;

	private readonly List<QuestPanel> questPanels = new List<QuestPanel>();
	private readonly List<RegionProgressMarker> markers = new List<RegionProgressMarker>();
	private ProgressRegion region;
	private bool expanded;

	public ProgressRegion Region => region;
	public bool IsExpanded => expanded;

	private void Awake() {
		if (foldButton != null)
			foldButton.onClick.AddListener(() => SetExpanded(!expanded));
	}

	public void Init(QuestsWindow window, ProgressRegion region, RegionUnlockSettings.Rule unlockRule) {
		this.region = region;
		var state = region.ResolveState();
		int percent = RegionProgressUtility.GetPercent(region.Id);

		regionTitle.text = LocalizationManager.Localize(region.Title);
		stateText.text = StateLabel(state, unlockRule);
		stateText.gameObject.SetActive(!string.IsNullOrEmpty(stateText.text));
		if (group != null)
			group.alpha = state == ProgressRegion.RegionState.Destroyed ? lockedAlpha : 1f;

		progressBar.SetValueWithoutNotify(percent / 100f);
		progressText.text = percent + "/100";
		BuildMarkers(percent);

		// Задания есть только у зоны в работе; пул строк — по шагам фронтира
		var steps = state == ProgressRegion.RegionState.InProgress ? region.GetFrontierSteps() : System.Array.Empty<ProgressStep>();
		int i = 0;
		for (; i < steps.Count; i++) {
			if (i >= questPanels.Count)
				questPanels.Add(Instantiate(questPanelPrefab, questsContainer));
			questPanels[i].gameObject.SetActive(true);
			questPanels[i].Init(window, steps[i]);
		}
		for (; i < questPanels.Count; i++)
			questPanels[i].gameObject.SetActive(false);

		foldRoot.SetActive(steps.Count > 0);
		foldLabel.text = LocalizationManager.Localize("Quests_Fold_Tasks") + " (" + steps.Count + ")";
		SetExpanded(false, true);
	}

	private static string StateLabel(ProgressRegion.RegionState state, RegionUnlockSettings.Rule unlockRule) {
		switch (state) {
			case ProgressRegion.RegionState.Repaired:
				return LocalizationManager.Localize("Quests_Restored");
			case ProgressRegion.RegionState.Destroyed:
				string locked = LocalizationManager.Localize("Quests_Locked");
				if (unlockRule == null || unlockRule.Conditions.Count == 0)
					return locked + " · " + LocalizationManager.Localize("Quests_UnlocksLater");
				var conditions = new List<string>();
				foreach (var condition in unlockRule.Conditions) {
					if (condition != null)
						conditions.Add(LocalizationManager.Localize("RegionTitle" + condition.RequiredRegion.ToSaveId()) + " " + condition.MinPercent + "%");
				}
				return locked + " · " + LocalizationManager.Localize("Quests_UnlocksAt", string.Join(", ", conditions));
			default:
				return string.Empty;
		}
	}

	// Одна засечка на порог: сколько апгрейдов (всех уровней) снимают лок на этом проценте зоны
	private void BuildMarkers(int percent) {
		var byPercent = new SortedDictionary<int, int>();
		foreach (var gated in LevelUpgradesManager.GetRegionGatedUpgrades(region.Id)) {
			byPercent.TryGetValue(gated.MinPercent, out int count);
			byPercent[gated.MinPercent] = count + 1;
		}
		BuildMarkers(percent, byPercent);
	}

	private void BuildMarkers(int percent, IDictionary<int, int> byPercent) {
		int i = 0;
		foreach (var pair in byPercent) {
			if (i >= markers.Count)
				markers.Add(Instantiate(markerPrefab, markersRoot));
			markers[i].gameObject.SetActive(true);
			markers[i].Set(pair.Key, pair.Value, percent >= pair.Key);
			i++;
		}
		for (; i < markers.Count; i++)
			markers[i].gameObject.SetActive(false);
	}

	public void SetExpanded(bool value, bool instant = false) {
		expanded = value;
		questsContainer.gameObject.SetActive(value);
		if (foldArrow != null) {
			foldArrow.DOKill();
			var angle = new Vector3(0f, 0f, value ? -90f : 0f);
			if (instant)
				foldArrow.localEulerAngles = angle;
			else
				foldArrow.DOLocalRotate(angle, arrowRotateDuration).SetUpdate(true);
		}
	}

#if UNITY_EDITOR
	// Заполнение примером для оффскрин-снимков (в edit mode нет ни сейва, ни контента зон)
	public void EditorPreview(string title, string state, int percent, IDictionary<int, int> markersByPercent, int tasks, bool expanded) {
		regionTitle.text = title;
		stateText.text = state;
		stateText.gameObject.SetActive(!string.IsNullOrEmpty(state));
		if (group != null)
			group.alpha = state.StartsWith("Lock") || state.StartsWith("Забл") ? lockedAlpha : 1f;
		progressBar.SetValueWithoutNotify(percent / 100f);
		progressText.text = percent + "/100";
		BuildMarkers(percent, markersByPercent);
		for (int i = questPanels.Count; i < tasks; i++)
			questPanels.Add(Instantiate(questPanelPrefab, questsContainer));
		for (int i = 0; i < questPanels.Count; i++) {
			questPanels[i].gameObject.SetActive(i < tasks);
			if (i < tasks)
				questPanels[i].EditorPreview("Repair the pier " + (i + 1), i + 2, i == 0);
		}
		foldRoot.SetActive(tasks > 0);
		foldLabel.text = "Tasks (" + tasks + ")";
		SetExpanded(expanded, true);
	}
#endif

	// Видимая строка шага (фокус на задании, по которому открыли окно)
	public QuestPanel FindPanel(ProgressStep step) {
		foreach (var panel in questPanels) {
			if (panel.gameObject.activeSelf && panel.Step == step)
				return panel;
		}
		return null;
	}
}
