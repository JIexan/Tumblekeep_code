using System;
using System.Collections.Generic;
using UnityEngine;

// Универсальный переключатель классических вкладок: заголовки (TabHeaderButton) видны всегда,
// активен один корень контента. Окну уровня исторически служит свой LevelInfoTabs с enum-ом.
public class TabsPanel : MonoBehaviour {
	[Serializable]
	private class Tab {
		public TabHeaderButton button;
		public GameObject root;
	}

	[SerializeField] List<Tab> tabs = new List<Tab>();

	public int Selected { get; private set; } = -1;
	public event Action<int> OnTabSelected;

	private void Awake() {
		for (int i = 0; i < tabs.Count; i++) {
			int index = i;
			tabs[i].button.Button.onClick.AddListener(() => Select(index));
		}
	}

	public void Select(int index) {
		for (int i = 0; i < tabs.Count; i++) {
			bool isSelected = i == index;
			tabs[i].root.SetActive(isSelected);
			tabs[i].button.SetSelected(isSelected);
		}
		if (Selected == index)
			return;
		Selected = index;
		OnTabSelected?.Invoke(index);
	}

	public void SetBadge(int index, bool visible) {
		tabs[index].button.SetBadge(visible);
	}
}
