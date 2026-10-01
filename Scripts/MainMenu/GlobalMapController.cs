using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Gameplay.Unlocks;

public class GlobalMapController : Singleton<GlobalMapController> {
	public Transform LvlContent; // Куда инстансить уровни
	public LevelSettings SettingsLevel; // Настройки уровней

	private List<GameObject> ListItems = new List<GameObject>();
	private List<LevelSelector> levelSelectors = new List<LevelSelector>();
	private Dictionary<LevelSelector, LockedIslandLabel> lockedLabels = new Dictionary<LevelSelector, LockedIslandLabel>();

    public void Start()
    {
        InputController.Instance.OnClick += OnClick;
        InGameEventManager.Subscribe(InGameEvents.OnContentUnlocked, OnContentUnlocked);
        InstantiateLvlObjects();
        gameObject.SetActive(false);
	}

	// Карта большую часть времени выключена: к её открытию профиль точно загружен и шаги хаба применены
	private void OnEnable() {
		RefreshLocks();
	}

	private void OnDestroy() {
		InGameEventManager.Unsubscribe(InGameEvents.OnContentUnlocked, OnContentUnlocked);
	}

    public void OnClick()
    {
        Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

		if (Physics.Raycast(r, out hit, float.MaxValue, LayerMask.GetMask("Clickable")))
        {
            var lvl = hit.collider.gameObject.GetComponent<LevelSelector>();
            if (lvl != null && lvl.Status < LevelStatus.close)
                ShowWindowLvlInfo(lvl.lvlId);
        }
    }

    public void CenterCamera() {
        CameraController.Instance.MoveToPositionOnLand(SettingsLevel.startPosition);
    }

    // Instance lvl
    private void InstantiateLvlObjects()
    {
        for (int i = 0; i < SettingsLevel.Levels.Count; i++)
        {
			AddLevel(SettingsLevel.Levels[i]);
		}
		RefreshLocks();
	}

	private void AddLevel(Level settings) {
		var newLevelObject = Instantiate(settings.lvlPrefab, LvlContent);
		newLevelObject.transform.position = SettingsLevel.startPosition + settings.Position;
		newLevelObject.lvlId = settings.Id;

        ListItems.Add(newLevelObject.gameObject);
        levelSelectors.Add(newLevelObject);
	}

	// Остров доступен, если открыт метой: со старта (ContentUnlocksSettings) или шагом хаба (UnlockLevelAction).
	// Закрытый остров виден на карте, но серый (LevelSelector подменяет материалы), не кликается (Status = close)
	// и подписан «пока недоступно»; об открытии сообщает попап (ContentUnlockWindow)
	private void RefreshLocks() {
		foreach (var selector in levelSelectors) {
			if (selector == null)
				continue;
			bool unlocked = ContentUnlocksManager.IsLevelUnlocked(selector.lvlId);
			selector.SetLevelLocked(!unlocked);
			selector.Status = unlocked ? LevelStatus.open : LevelStatus.close;
			SetLockedLabel(selector, !unlocked);
		}
	}

	private void SetLockedLabel(LevelSelector selector, bool show) {
		lockedLabels.TryGetValue(selector, out var label);
		if (show && label == null && SettingsLevel.lockedLabelPrefab != null) {
			label = Instantiate(SettingsLevel.lockedLabelPrefab, LvlContent);
			label.transform.position = selector.GetTopPoint() + Vector3.up * SettingsLevel.lockedLabelOffset;
			lockedLabels[selector] = label;
		}
		if (label != null)
			label.gameObject.SetActive(show);
	}

	private void OnContentUnlocked(object data) {
		if (data is ContentUnlock unlock && unlock.Kind == UnlockKind.Level)
			RefreshLocks();
	}

    // Окно информации уровня
    public void ShowWindowLvlInfo(string idLvl)
    {
        if(idLvl == "Home") {
            return;
        }
        var lvlSettings = SettingsLevel.Levels.FirstOrDefault(x => x.Id.StartsWith(idLvl));
        PlayerLocationUIController.Instance.ShowWindow(WindowType.LevelInfoWindow, lvlSettings);
	}
}
