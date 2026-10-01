using System.Collections.Generic;
using UnityEngine;

// Ряд иконок полученных в катке реликвий на HUD (низ-лево, под окнами).
// Перестраивается по OnRelicAdded; описание реликвии — подсказка слота по наведению.
public class RelicsPanel : MonoBehaviour {
	[SerializeField] RelicSlot slotPrefab;
	[SerializeField] Transform slotsRoot;

	private readonly List<RelicSlot> slots = new();

	private void Start() {
		Rebuild();
		InGameEventManager.Subscribe(InGameEvents.OnRelicAdded, OnRelicAdded);
	}

	private void OnDestroy() {
		InGameEventManager.Unsubscribe(InGameEvents.OnRelicAdded, OnRelicAdded);
	}

	private void OnRelicAdded(object param) {
		Rebuild();
	}

	private void Rebuild() {
		foreach (var slot in slots) {
			if (slot != null)
				Destroy(slot.gameObject);
		}
		slots.Clear();

		if (slotPrefab == null || slotsRoot == null)
			return;

		var rarityConfig = GameConfigs.Instance != null ? GameConfigs.Instance.RarityConfig : null;
		foreach (var owned in Gameplay.Relics.RelicsManager.Owned) {
			var definition = owned.Definition;
			var slot = Instantiate(slotPrefab, slotsRoot);
			var raritySettings = rarityConfig != null ? rarityConfig.Get(definition.Rarity) : null;
			slot.Init(definition, owned.Stacks, raritySettings != null ? raritySettings.c : Color.white);
			slots.Add(slot);
		}
	}
}
