using System;
using System.Collections.Generic;
using Gameplay.RestorationSystem;
using UnityEngine;

public class TrackingPanelsController : MonoBehaviour
{
	[Serializable]
	public class PanelPrefabEntry
	{
		public TrackingPanelType type;
		public WorldTrackingPanel prefab;
	}

	[SerializeField] private List<PanelPrefabEntry> panelPrefabs;
	[SerializeField] private RectTransform canvas;

	private Dictionary<object, WorldTrackingPanel> activePanels = new ();

	private void OnEnable() {
		InGameEventManager.Subscribe(InGameEvents.OnTrackingRequested, OnTrackingRequested);
		InGameEventManager.Subscribe(InGameEvents.OnTrackingTimerRequested, OnTrackingTimerRequested);
		InGameEventManager.Subscribe(InGameEvents.OnUntrackingRequested, OnUntrackingRequested);
	}

	private void OnDisable() {
		InGameEventManager.Unsubscribe(InGameEvents.OnTrackingRequested, OnTrackingRequested);
		InGameEventManager.Unsubscribe(InGameEvents.OnTrackingTimerRequested, OnTrackingTimerRequested);
		InGameEventManager.Unsubscribe(InGameEvents.OnUntrackingRequested, OnUntrackingRequested);
	}

	// --- Event handlers ---

	private void OnTrackingRequested(object data) {
		if (data is TrackingRequest request) {
			Track(request.Key, request.PanelType, request.Target, request.OnClick);
		}
	}

	private void OnTrackingTimerRequested(object data) {
		if (data is TrackingTimerRequest request) {
			TrackWithTimer(request.Key, request.PanelType, request.Target, request.Duration, request.OnClick, request.OnTimerExpired);
		}
	}

	private void OnUntrackingRequested(object data) {
		if (data is UntrackingRequest request) {
			Untrack(request.Key);
		}
	}

	// --- Public API (для прямого вызова если нужно) ---

	public WorldTrackingPanel Track(object key, TrackingPanelType panelType, Transform target, Action onClick = null) {
		// Quest panel of the same step is kept as is: UpdateState re-requests the whole frontier after every
		// completed step, recreating would drop an open popup / crystal flight of a neighbour task
		if (key is ProgressStep trackedStep && activePanels.TryGetValue(key, out var existing) && existing != null
			&& existing is QuestTrackingPanel existingQuest && existingQuest.Step == trackedStep)
			return existing;

		// Если панель уже существует — убрать старую
		Untrack(key);

		var prefab = GetPrefab(panelType);
		if (prefab == null) {
			Debug.LogWarning($"TrackingPanelsController: no prefab for type {panelType}");
			return null;
		}

		WorldTrackingPanel panel = Instantiate(prefab, transform);
		panel.Init(target, canvas.sizeDelta, onClick);
		if (panel is QuestTrackingPanel questPanel && key is ProgressStep step)
			questPanel.SetStep(step);
		panel.Show();
		activePanels.Add(key, panel);
		return panel;
	}

	public WorldTrackingPanel TrackWithTimer(object key, TrackingPanelType panelType, Transform target, float duration, Action onClick = null, Action onTimerExpired = null) {
		var panel = Track(key, panelType, target, onClick);
		if (panel != null) {
			// При истечении таймера — автоматически удалить панель
			panel.SetTimer(duration, () => {
				onTimerExpired?.Invoke();
				Untrack(key);
			});
		}
		return panel;
	}

	public void Untrack(object key) {
		if (key != null && activePanels.TryGetValue(key, out var panel)) {
			if (panel != null) {
				panel.Hide();
				Destroy(panel.gameObject);
			}
			activePanels.Remove(key);
		}
	}

	public void UntrackAll() {
		foreach (var kvp in activePanels) {
			if (kvp.Value != null) {
				kvp.Value.Hide();
				Destroy(kvp.Value.gameObject);
			}
		}
		activePanels.Clear();
	}

	public WorldTrackingPanel GetPanel(object key) {
		activePanels.TryGetValue(key, out var panel);
		return panel;
	}

	private WorldTrackingPanel GetPrefab(TrackingPanelType type) {
		foreach (var entry in panelPrefabs) {
			if (entry.type == type)
				return entry.prefab;
		}
		// Fallback на первый префаб
		return panelPrefabs.Count > 0 ? panelPrefabs[0].prefab : null;
	}
}
