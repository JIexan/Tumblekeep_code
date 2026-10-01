using System;
using System.Collections.Generic;
using UnityEngine;

public enum LevelInfoTab {
	Description,
	Upgrades,
	Leaderboard,
}

// Переключатель вкладок LevelInfoWindow (Инфо / Апгрейды / Рейтинг).
// Заголовки вкладок видны всегда, подсвечен только выбранный (см. TabHeaderButton).
public class LevelInfoTabs : MonoBehaviour {
	[Serializable]
	private class Tab {
		public TabHeaderButton button;
		public GameObject root;
	}

	[SerializeField] List<Tab> tabs = new List<Tab>(); // порядок = LevelInfoTab

	private void Awake() {
		for (int i = 0; i < tabs.Count; i++) {
			var tab = (LevelInfoTab)i;
			tabs[i].button.Button.onClick.AddListener(() => Select(tab));
		}
	}

	public void Select(LevelInfoTab selected) {
		for (int i = 0; i < tabs.Count; i++) {
			bool isSelected = i == (int)selected;
			tabs[i].root.SetActive(isSelected);
			tabs[i].button.SetSelected(isSelected);
		}
	}

	public void SelectDescription() {
		Select(LevelInfoTab.Description);
	}

	// Индикатор «!» на заголовке вкладки.
	public void SetBadge(LevelInfoTab tab, bool visible) {
		tabs[(int)tab].button.SetBadge(visible);
	}

	// У уровня без конфига апгрейдов вкладка не показывается вовсе.
	public void SetUpgradesAvailable(bool available) {
		tabs[(int)LevelInfoTab.Upgrades].button.gameObject.SetActive(available);
	}
}
