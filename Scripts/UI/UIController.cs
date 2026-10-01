using DG.Tweening;
using TMPro;
using Gameplay.TasksSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
	[SerializeField] Sprite[] icons;
	[SerializeField] WindowsManager windowsManager;
	public WindowsManager Windows => windowsManager;

	public GameController gameController;

	public GameObject startGameButton;
	public HeathBarController heathBarController;
	public TrackingPanelsController trackingPanelsController;
    public TMP_Text WaveCurrent;
	[SerializeField] TMP_Text towersCount;
	[SerializeField] Color towerLimitColor = new Color(1f, 0.65f, 0.35f);
	private Color towersCountColor;
	public Transform spellDragPanel;
	public SpellTimerPanel spellTimerPanel;

	public RectTransform canvas;
    public Image healthBar;
    public float border = 60f;
    public TMP_Text HPCount;

	public TowerPanel towerPanel;
	public GameObject SpellBuy;

	// Кнопка «Уплыть» под панелью тасков: появляется после основного таска, завершает катку победой
	[SerializeField] RectTransform leaveButton;
	[SerializeField] float leaveButtonAppearDuration = 0.4f;

	bool towerPanelActive = false;
    private Collider selectedObjectCollider = null;

	private bool previewMode = false;

	// Start is called before the first frame update
	void Start()
    {
        towerPanel.gameObject.SetActive(false);
        HPCount.text = LevelDesriptor.Instance.castleHP.ToString();
		if (towersCount != null)
			towersCountColor = towersCount.color;
		InGameEventManager.Subscribe(InGameEvents.OnTowerBuilt, UpdateTowersCount);
		InGameEventManager.Subscribe(InGameEvents.OnLevelUpgradePurchased, UpdateTowersCount);
		UpdateTowersCount(null);

#if CHEATS
		// Создаем читовое окно при старте игры (для тестирования)
		var cheatsManager = gameObject.AddComponent<Cheats.CheatsManager>();
		cheatsManager.GenerateGameCheats();
#endif

	}

	private void OnDestroy() {
		InGameEventManager.Unsubscribe(InGameEvents.OnTowerBuilt, UpdateTowersCount);
		InGameEventManager.Unsubscribe(InGameEvents.OnLevelUpgradePurchased, UpdateTowersCount);
		if (towersCount != null)
			towersCount.rectTransform.DOKill();
	}

	private void UpdateTowersCount(object _) {
		if (towersCount == null || gameController == null)
			return;
		towersCount.SetText("{0}/{1}", gameController.TowersCount, gameController.MaxTowersCount);
		towersCount.color = gameController.HasTowerCapacity ? towersCountColor : towerLimitColor;
	}

	public void ShowTowerLimitReached() {
		UpdateTowersCount(null);
		if (towersCount == null)
			return;
		var rect = towersCount.rectTransform;
		rect.DOKill();
		rect.localScale = Vector3.one;
		rect.DOPunchScale(Vector3.one * 0.15f, 0.35f, 2).SetUpdate(true);
	}

	public void UpdateCastleHealthBar(float currentHP, float normalizedHP) {
		if (currentHP < 0)
			currentHP = 0;
        healthBar.fillAmount = normalizedHP;
        HPCount.text = Mathf.RoundToInt(currentHP).ToString();
    }

	// Обновить номер текущего уровня
    public void UpdateWave(int wave)
    {
        WaveCurrent.text = wave.ToString();
    }

    public void HideTowerPanel() {
		selectedObjectCollider = null;
		towerPanel.Hide();
		towerPanelActive = false;
	}

	public void ShowUpgradeTowerWindow(BaseTower tower, Action onHide) {
		ShowWindow(WindowType.UpgradeWindow, tower, null, onHide);
	}

	[Sirenix.OdinInspector.Button("Test open chest")]
	public void ShowOpenChestWindow() {
		ShowWindow(WindowType.OpenChestWindow, null, null, null);
	}

	public void ShowNewTowerPanel() {
		selectedObjectCollider = null;
		towerPanel.ClearButtons();
		SetupTowersPanel();

		towerPanelActive = true;
		towerPanel.Show();

		if(gameController.selected != null)
			selectedObjectCollider = gameController.selected.GetComponent<Collider>();
	}

	private static readonly TowerType[] buyableTowerTypes = { TowerType.Laser, TowerType.Arrow, TowerType.Mortar, TowerType.Electric };

	private void SetupTowersPanel() {
		foreach (var type in buyableTowerTypes) {
			// Виды башен на уровне открываются апгрейдами (UnlockTowerTypeEffect).
			if (!Gameplay.LevelUpgrades.LevelUpgradesManager.IsTowerTypeAvailable(type))
				continue;

			var towerSettings = GameController.Instance.GetTowerSettings(type, 0);
			// Цена с учётом скидок реликвий — та же, что спишется при постройке
			towerPanel.AddBuyButton((int)type, OnPreviewBuidTower, OnBuildTowerClick, towerSettings.iconName, GameController.Instance.GetTowerPrice(type), TowerButtonType.Buy);
		}
	}

	public void OnPreviewBuidTower(int towerType) {
		if(previewMode) {
			towerPanel.ResetButtonsConfirmMode(towerType);
		} else {
			previewMode = true;
		}

		gameController.ShowPreviewTower(towerType);
	}

    public void OnBuildTowerClick(int towerType) {
        gameController.BuildTowerOnSelectedPlot(towerType);
    }

	public void OnPreviewUpgradeTower(int upgradeId) {
		if (previewMode) {
			towerPanel.ResetButtonsConfirmMode(upgradeId);
		} else {
			previewMode = true;
		}

		if(upgradeId != -1) {
			gameController.ShowUpgradeTowerPreview(upgradeId);
		} else {
			gameController.HideUpgradeTowerPreview();
		}
	}

	public void ShowLoseWindow() {
		ShowWindow(WindowType.DefeatWindow);
	}

	public void ShowWinWindow(VictoryWindowData data) {
		ShowWindow(WindowType.VictoryWindow, data);
	}

	public void ShowLeaveButton() {
		if (leaveButton == null || leaveButton.gameObject.activeSelf)
			return;
		leaveButton.gameObject.SetActive(true);
		leaveButton.DOKill();
		leaveButton.localScale = Vector3.zero;
		leaveButton.DOScale(1f, leaveButtonAppearDuration).SetEase(Ease.OutBack).SetUpdate(true);
	}

	public void HideLeaveButton() {
		if (leaveButton == null)
			return;
		leaveButton.DOKill();
		leaveButton.localScale = Vector3.one;
		leaveButton.gameObject.SetActive(false);
	}

	// OnClick кнопки LeaveButton в UICanvas
	public void OnLeaveClick() {
		gameController.LeaveWithVictory();
	}

	public void ShowPauseMenu() {
		if (windowsManager.IsAnyWindowActive())
			return;
		ShowWindow(WindowType.PauseWindow);
	}

    public void OnStartGame() {
		startGameButton.SetActive(false);
		gameController.StartGame();
        GlobalEventManager.SendEvent(GlobalEvents.StopAnimationExclamationPoint);
    }

	public void AddHealthBar(Unit u) {
		heathBarController.AddHeathBar(u);
	}

	// Update is called once per frame
	void Update()
    {
		// Esc открывает паузу, если нет других окон (закрытие — внутри PauseWindow)
		if (Input.GetKeyDown(KeyCode.Escape))
			ShowPauseMenu();

        if (towerPanelActive) {
			if (selectedObjectCollider == null) {
				HideTowerPanel();
				return;
			}

			Vector3 vieportPosition = Camera.main.WorldToViewportPoint(selectedObjectCollider.transform.position);
			Vector3 canvasPosition = Vector3.Scale(vieportPosition, canvas.sizeDelta);
			(towerPanel.transform as RectTransform).anchoredPosition = canvasPosition;

            if (selectedObjectCollider != null) {
				Bounds b = selectedObjectCollider.bounds;
				vieportPosition = Camera.main.WorldToViewportPoint(selectedObjectCollider.transform.position + new Vector3(b.extents.x, 0f, b.extents.z));
				Vector3 offsetPosition = Vector3.Scale(vieportPosition, canvas.sizeDelta);
				float size = (offsetPosition - canvasPosition).magnitude * 2 + border;
				(towerPanel.transform as RectTransform).sizeDelta = new Vector2(size, size);
            }
		}
	}

	public void ShowWindow(WindowType type, object data = null, Action onBeforeShow = null, Action onAfterHide = null) {
		windowsManager.ShowWindow(type, data, onBeforeShow, onAfterHide);
	}


	public void ShowSpellBuy()
	{
		SpellBuy.SetActive(true);
	}

	public void HideSpellBuy()
	{
		SpellBuy.SetActive(false);
	}

	public Sprite GetIcon(string name) {
		return icons.FirstOrDefault(i => i.name == name);
	}

	public void SetDragPanelActive(bool active) {
		spellDragPanel.gameObject.SetActive(active);
	}

	public void ShowChestPanel(RewardChest chestObject, int price, Action buyCallback) {
		towerPanel.ClearButtons();
		towerPanel.AddBuyButton(-1, null, (id) => {
			buyCallback?.Invoke();
		}, "battlecoins", price, TowerButtonType.Buy);

		towerPanelActive = true;
		towerPanel.Show();

		if (selectedObjectCollider != chestObject.Collider)
			selectedObjectCollider = chestObject.Collider;
	}
}
