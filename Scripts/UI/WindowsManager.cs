using System;
using System.Collections.Generic;
using UnityEngine;

public enum WindowType {
	UpgradeWindow,
	OpenChestWindow,
	VictoryWindow,
	DefeatWindow,
	QuestsWindow,
	GlobalMapWindow,
	LevelInfoWindow,
	Settings,
	MerchantWindow,
	ShopWindow,
	UpgradeInfoWindow,
	Book,
	PauseWindow,
	ContentUnlockWindow // попап «контент открыт» (хаб), см. ContentUnlockAnnouncer
}

public class WindowsManager : MonoBehaviour {

	[System.Serializable]
	public class WindowsDictionary : SerializableDictionary<WindowType, BaseWindow> { }

	[SerializeField] WindowsDictionary windows;

	public delegate void WindowHandler(BaseWindow window);

	private WindowHandler onBeforeWindowOpen;
	private WindowHandler onAfterWindowClose;

	public event WindowHandler OnBeforeWindowOpen {
		add {
			onBeforeWindowOpen -= value;
			onBeforeWindowOpen += value; 
		}
		remove { onBeforeWindowOpen -= value; }
	}

	public event WindowHandler OnAfterWindowClose {
		add {
			onAfterWindowClose -= value;
			onAfterWindowClose += value;
		}
		remove { onAfterWindowClose -= value; }
	}

	public void ShowWindow(WindowType type, object data = null, Action onBeforeShow = null, Action onAfterHide = null) {
		if (windows.ContainsKey(type)) {
			onBeforeWindowOpen?.Invoke(windows[type]);
			windows[type].gameObject.SetActive(true);
			// Дубль C#-события в общую шину — на него подписываются условия туториала.
			// HideWindow/HideAllWindows обходят onAfterHide, поэтому OnWindowClosed
			// гарантирован только для штатного закрытия окна.
			InGameEventManager.SendEvent(InGameEvents.OnWindowOpened, type);

			// Учёт закрытия (ref-count HUD хаба, OnWindowClosed) обязан отработать, даже если
			// колбек вызывающего бросил исключение — иначе HUD остаётся выключенным навсегда
			Action callerAfterHide = onAfterHide;
			onAfterHide = () => {
				try {
					callerAfterHide?.Invoke();
				} finally {
					onAfterWindowClose?.Invoke(windows[type]);
					InGameEventManager.SendEvent(InGameEvents.OnWindowClosed, type);
				}
			};

			windows[type].Show(data, onBeforeShow, onAfterHide);
		}
	}

	public bool IsAnyWindowActive() {
		foreach (var window in windows.Values) {
			if (window != null && window.gameObject.activeSelf)
				return true;
		}
		return false;
	}

	public bool IsWindowActive(WindowType type) {
		if (windows.ContainsKey(type)) {
			return windows[type].gameObject.activeSelf;
		}
		return false;
	}

	public void HideWindow(WindowType type) {
		if (windows.ContainsKey(type)) {
			windows[type].gameObject.SetActive(false);
		}
	}

	public void HideAllWindows() {
		foreach (var window in windows.Values) {
			window.gameObject.SetActive(false);
		}
	}

	public BaseWindow GetWindow(WindowType type) {
		return windows.ContainsKey(type) ? windows[type] : null;
	}

#if UNITY_EDITOR
	// Для билдеров UI: регистрация окна в словаре без ручного тыканья в инспектор
	public void EditorRegisterWindow(WindowType type, BaseWindow window) {
		if (windows == null)
			windows = new WindowsDictionary();
		windows[type] = window;
	}
#endif
}