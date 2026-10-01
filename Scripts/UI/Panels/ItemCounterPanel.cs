using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemCounterPanel : MonoBehaviour
{
	[SerializeField] ItemType itemType;
	[SerializeField] TMP_Text counter;
	[Tooltip("Иконка валюты — точка вылета/прилёта FX. Пусто — первая дочерняя Image")]
	[SerializeField] RectTransform icon;

	private static readonly List<ItemCounterPanel> activePanels = new();

	public RectTransform Icon {
		get {
			if (icon == null) {
				foreach (var image in GetComponentsInChildren<Image>(true)) {
					if (image.transform != transform) {
						icon = image.rectTransform;
						break;
					}
				}
				if (icon == null)
					icon = (RectTransform)transform;
			}
			return icon;
		}
	}

	// Видимый сейчас счётчик предмета (в хабе их несколько: HUD, окно тасков, магазин)
	public static ItemCounterPanel GetActive(ItemType type) {
		for (int i = activePanels.Count - 1; i >= 0; i--) {
			var panel = activePanels[i];
			if (panel != null && panel.itemType == type)
				return panel;
		}
		return null;
	}

	private void OnEnable() {
		activePanels.Add(this);
	}

	private void OnDisable() {
		activePanels.Remove(this);
	}

	private void Start() {
		InGameEventManager.Subscribe(InGameEvents.OnItemCountChanged, OnItemAdded);
		UpdateCounter();
	}

	private void OnDestroy() {
		InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnItemAdded);
	}

	private void OnItemAdded(object obj) {
		if(obj is ItemType changedItemType && changedItemType == itemType) {
			UpdateCounter();
		}
	}

	private void UpdateCounter() {
		var count = SavesManager.Profile.GetItem(itemType);
		counter.text = count.ToString();
	}
}
