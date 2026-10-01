using Assets.SimpleLocalization;
using DG.Tweening;
using Gameplay.RestorationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Панелька таска в хабе (FollowerPanel): кристалл + цена шага. Клик не открывает окно тасков, а показывает
/// над панелькой попап с названием шага и кнопкой «Выполнить». По кнопке попап прячется, кристалл летит из
/// HUD-счётчика в кристалл панельки, по прилёту — обычный ProgressStep.CompleteWithEffects().
/// Выполнение из окна тасков (QuestPanel) не затронуто.
/// Попап и подписи собирает Tools/UI/Build Quest Tracking Panel — ручные правки вёрстки вносить в билдер.
/// </summary>
public class QuestTrackingPanel : WorldTrackingPanel {
	[Header("Панелька")]
	[SerializeField] private RectTransform clickArea;
	[SerializeField] private Image crystalIcon;
	[SerializeField] private TMP_Text priceText;
	[SerializeField] private Color affordableColor = Color.white;
	[SerializeField] private Color notAffordableColor = new Color(1f, 0.42f, 0.36f, 1f);

	[Header("Попап")]
	[SerializeField] private RectTransform popup;
	[SerializeField] private CanvasGroup popupGroup;
	[SerializeField] private TMP_Text popupTitle;
	[SerializeField] private Button completeButton;
	[SerializeField] private float notAffordableButtonAlpha = 0.5f;
	[SerializeField] private float popupShowDuration = 0.2f;
	[SerializeField] private float popupHideDuration = 0.12f;
	[Tooltip("Отступ попапа от краёв экрана (в единицах канваса)")]
	[SerializeField] private float screenMargin = 12f;

	[Header("Полёт кристалла из счётчика")]
	[SerializeField] private CrystalFlight.Settings flight = new CrystalFlight.Settings();
	[SerializeField] private float panelHideDuration = 0.2f;

	// Одновременно открыт один попап
	private static QuestTrackingPanel openPanel;
	// Кристаллы «в полёте»: тратятся только по прилёту (в CompleteWithEffects), до этого держим резерв,
	// чтобы второй таск нельзя было запустить на те же кристаллы
	private static int reservedCrystals;

	private ProgressStep step;
	private Vector2 popupDefaultPosition;
	private bool popupOpen;
	private bool completing;
	private int reserved;
	private Tween popupTween;
	private CanvasGroup buttonGroup;

	public ProgressStep Step => step;

	protected override void Awake() {
		base.Awake();
		if (popup != null) {
			popupDefaultPosition = popup.anchoredPosition;
			popup.gameObject.SetActive(false);
		}
		if (completeButton != null) {
			completeButton.onClick.AddListener(OnCompleteClicked);
			// У Button.prefab переход — аниматор без состояния Disabled, нехватку кристаллов показываем прозрачностью
			buttonGroup = completeButton.GetComponent<CanvasGroup>();
			if (buttonGroup == null)
				buttonGroup = completeButton.gameObject.AddComponent<CanvasGroup>();
		}
	}

	private void OnEnable() {
		InGameEventManager.Subscribe(InGameEvents.OnItemCountChanged, OnItemCountChanged);
		RefreshAffordable();
	}

	private void OnDisable() {
		InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnItemCountChanged);
		// HUD прячется под окна/катсцены — попап с собой не тащим
		ClosePopup(true);
	}

	private void OnDestroy() {
		if (openPanel == this)
			openPanel = null;
		ReleaseReserve();
		popupTween?.Kill();
	}

	public void SetStep(ProgressStep progressStep) {
		step = progressStep;
		if (priceText != null)
			priceText.text = step.Cost.ToString();
		RefreshAffordable();
	}

	public override void OnPanelClicked() {
		if (step == null || completing || Gameplay.Cutscenes.CutsceneController.IsPlaying)
			return;
		if (popupOpen)
			ClosePopup(false);
		else
			OpenPopup();
	}

	protected override void Update() {
		base.Update();
		if (popupOpen && Input.GetMouseButtonDown(0) && !IsPointerOverPanel())
			ClosePopup(false);
	}

	private void LateUpdate() {
		if (popupOpen)
			ClampPopupToScreen();
	}

	// --- Попап ---

	private void OpenPopup() {
		if (popup == null)
			return;
		if (openPanel != null && openPanel != this)
			openPanel.ClosePopup(false);
		openPanel = this;
		popupOpen = true;

		// Поверх соседних панелек
		transform.SetAsLastSibling();
		if (popupTitle != null)
			popupTitle.text = LocalizationManager.Localize(step.Title);
		RefreshAffordable();

		popupTween?.Kill();
		popup.gameObject.SetActive(true);
		popup.localScale = Vector3.one * 0.6f;
		if (popupGroup != null) {
			popupGroup.alpha = 0f;
			popupGroup.interactable = true;
		}
		ClampPopupToScreen();
		var seq = DOTween.Sequence();
		seq.Append(popup.DOScale(1f, popupShowDuration).SetEase(Ease.OutBack));
		if (popupGroup != null)
			seq.Join(popupGroup.DOFade(1f, popupShowDuration * 0.7f));
		popupTween = seq.SetLink(gameObject);

		InGameEventManager.SendEvent(InGameEvents.OnQuestPopupOpened, step);
	}

	private void ClosePopup(bool instant) {
		if (!popupOpen)
			return;
		popupOpen = false;
		if (openPanel == this)
			openPanel = null;

		popupTween?.Kill();
		popupTween = null;
		if (popup == null)
			return;
		if (popupGroup != null)
			popupGroup.interactable = false;
		if (instant || !gameObject.activeInHierarchy) {
			popup.gameObject.SetActive(false);
			return;
		}
		var seq = DOTween.Sequence();
		seq.Append(popup.DOScale(0.6f, popupHideDuration).SetEase(Ease.InBack));
		if (popupGroup != null)
			seq.Join(popupGroup.DOFade(0f, popupHideDuration));
		seq.OnComplete(() => popup.gameObject.SetActive(false));
		popupTween = seq.SetLink(gameObject);
	}

	private bool IsPointerOverPanel() {
		Vector2 pointer = Input.mousePosition;
		// Overlay-канвас — камера null
		return (popup != null && RectTransformUtility.RectangleContainsScreenPoint(popup, pointer, null))
			|| (clickArea != null && RectTransformUtility.RectangleContainsScreenPoint(clickArea, pointer, null));
	}

	// Панелька прижимается к краям экрана, попап над ней мог бы вылезти за экран — сдвигаем внутрь.
	// Размер считаем без собственного scale попапа (он анимируется при показе).
	private void ClampPopupToScreen() {
		var canvas = popup.GetComponentInParent<Canvas>();
		if (canvas == null)
			return;
		var canvasRect = (RectTransform)canvas.rootCanvas.transform;
		var parent = (RectTransform)popup.parent;

		Vector3 position = parent.TransformPoint(popupDefaultPosition);
		float scale = parent.lossyScale.x;
		Vector2 size = popup.rect.size * scale;
		float margin = screenMargin * scale;
		Vector2 pivot = popup.pivot;

		var corners = new Vector3[4];
		canvasRect.GetWorldCorners(corners);
		float minX = corners[0].x + margin + size.x * pivot.x;
		float maxX = corners[2].x - margin - size.x * (1f - pivot.x);
		float minY = corners[0].y + margin + size.y * pivot.y;
		float maxY = corners[2].y - margin - size.y * (1f - pivot.y);
		position.x = Mathf.Clamp(position.x, minX, Mathf.Max(minX, maxX));
		position.y = Mathf.Clamp(position.y, minY, Mathf.Max(minY, maxY));
		popup.position = position;
	}

	// --- Цена ---

	private void OnItemCountChanged(object obj) {
		if (obj is ItemType type && type == ItemType.Crystal)
			RefreshAffordable();
	}

	private bool CanAfford() {
		return step != null && SavesManager.Profile.HasItem(ItemType.Crystal, step.Cost + reservedCrystals - reserved);
	}

	private void RefreshAffordable() {
		if (step == null)
			return;
		bool affordable = CanAfford();
		if (priceText != null)
			priceText.color = affordable ? affordableColor : notAffordableColor;
		if (completeButton != null)
			completeButton.interactable = affordable && !completing;
		if (buttonGroup != null)
			buttonGroup.alpha = affordable ? 1f : notAffordableButtonAlpha;
	}

	private void ReleaseReserve() {
		reservedCrystals -= reserved;
		reserved = 0;
	}

	// --- Выполнение ---

	private void OnCompleteClicked() {
		if (step == null || completing || !CanAfford())
			return;
		completing = true;
		reserved = step.Cost;
		reservedCrystals += reserved;
		ClosePopup(false);
		FlyCrystal();
	}

	// Общий полёт (CrystalFlight): из иконки HUD-счётчика в кристалл панельки
	private void FlyCrystal() {
		var counter = ItemCounterPanel.GetActive(ItemType.Crystal);
		if (counter == null || crystalIcon == null) {
			OnCrystalArrived();
			return;
		}
		CrystalFlight.Fly(counter.Icon, crystalIcon.rectTransform, crystalIcon.sprite, flight, gameObject, OnCrystalArrived);
	}

	private void OnCrystalArrived() {
		// Дальше — как раньше: списание кристаллов, подлёт камеры и экшены шага
		ReleaseReserve();
		step.CompleteWithEffects();

		// Панельку прячем сразу (Untrack придёт только по OnQuestCompleted, после всех экшенов); прилёт уже «пнул» иконку
		transform.DOScale(0f, panelHideDuration).SetDelay(flight.punchDuration).SetEase(Ease.InBack).SetLink(gameObject);
	}
}
