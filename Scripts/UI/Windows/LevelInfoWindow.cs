using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Gameplay.TasksSystem;
using System;
using TMPro;


public class LevelInfoWindow : AnimationWindow
{
    private string nameSceneLoad;
    [SerializeField] TMP_Text txtLvlId;
	[SerializeField] GameObject previewPrefabParent;
	// Лента под вкладками: особенность острова (у уровня без неё — описание LevelDesc_{Id})
	[SerializeField] IslandFeaturePanel featurePanel;
    [SerializeField] TasksPanel tasksPanel;
    [SerializeField] LevelInfoTabs tabs;
    [SerializeField] LevelUpgradesTab upgradesTab;
    [SerializeField] IslandLevelPanel islandLevelPanel;

    private Level level;

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);

        if(data is Level level) {
            Init(level);
        }
	}

    public void Init(Level level) {
        this.level = level;
		nameSceneLoad = level.LoadScene;
		// LevelName_{Id} из таблицы локализации; нет ключа — сырой Id из LevelSettings
		txtLvlId.text = BookLoc.TryLocalize("LevelName_" + level.Id, out var levelName) ? levelName : level.Id;
		if (featurePanel != null)
			featurePanel.Setup(level);
        if (previewPrefabParent.transform.childCount > 0)
        {
            for (int i = previewPrefabParent.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(previewPrefabParent.transform.GetChild(i).gameObject);
            }
        }
        if (level.previewPrefab != null)
        {
            GameObject preview = Instantiate(level.previewPrefab, previewPrefabParent.transform);
            preview.transform.localPosition = Vector3.zero;
            preview.transform.localRotation = Quaternion.Euler(Vector3.zero);
            preview.transform.localScale = Vector3.one;
        }

        tasksPanel.Init(level.GetLevelTasks());

        tabs.SetUpgradesAvailable(Gameplay.LevelUpgrades.LevelUpgradesManager.GetConfig(level.Id) != null);
        tabs.SelectDescription();
        upgradesTab.Setup(level.Id);
        RefreshUpgradesBadge();

        if (islandLevelPanel != null)
            islandLevelPanel.Setup(level.Id);
	}

    private void OnEnable() {
        InGameEventManager.Subscribe(InGameEvents.OnLevelUpgradePurchased, OnUpgradePurchased);
        InGameEventManager.Subscribe(InGameEvents.OnLevelUpgradePurchased, OnUpgradesStateChanged);
        InGameEventManager.Subscribe(InGameEvents.OnItemCountChanged, OnUpgradesStateChanged);
        InGameEventManager.Subscribe(InGameEvents.OnIslandXPChanged, OnUpgradesStateChanged);
    }

    private void OnDisable() {
        InGameEventManager.Unsubscribe(InGameEvents.OnLevelUpgradePurchased, OnUpgradePurchased);
        InGameEventManager.Unsubscribe(InGameEvents.OnLevelUpgradePurchased, OnUpgradesStateChanged);
        InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnUpgradesStateChanged);
        InGameEventManager.Unsubscribe(InGameEvents.OnIslandXPChanged, OnUpgradesStateChanged);
    }

    private void OnUpgradesStateChanged(object _) {
        RefreshUpgradesBadge();
    }

    // Покупка меняет прокачку острова и пул доп. тасков — уже выданные таски устарели
    private void OnUpgradePurchased(object _) {
        if (level != null)
            tasksPanel.Init(level.GetLevelTasks(true));
    }

    // «!» на вкладке апгрейдов, пока есть что купить прямо сейчас
    private void RefreshUpgradesBadge() {
        if (level != null)
            tabs.SetBadge(LevelInfoTab.Upgrades, Gameplay.LevelUpgrades.LevelUpgradesManager.HasAvailableUpgrades(level.Id));
    }

    public void LoadLvl()
    {
        CurrentLevelContext.Level = level;
        LevelTasksManager.Instance.SetLevelTasks(level.GetLevelTasks());

        level.ClearTasks();
		if (!string.IsNullOrEmpty(nameSceneLoad))
            SceneManager.LoadScene(nameSceneLoad);
    }

    public void Close()
    {
        Hide();
    }
}
