using Assets.SimpleLocalization;
using Gameplay.RestorationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// Строка задания в окне заданий: название, индикатор «открывает новое» (у шага есть Unlock*-экшен),
// кнопка с ценой. Выполнение идёт через окно (полёт кристалла → ProgressStep.CompleteWithEffects).
public class QuestPanel : MonoBehaviour {
	[SerializeField] TMP_Text title;
	[SerializeField] TMP_Text price;
	[SerializeField, FormerlySerializedAs("starIcon")] Image currencyIcon;
	[SerializeField] Button buyButton;
	[SerializeField] GameObject highlight;
	[SerializeField] GameObject unlockBadge;
	[SerializeField] Color affordableColor = Color.white;
	[SerializeField] Color notAffordableColor = new Color(1f, 0.42f, 0.36f, 1f);
	[SerializeField] float notAffordableButtonAlpha = 0.5f;

	private ProgressStep currentStep;
	private QuestsWindow owner;
	private CanvasGroup buttonGroup;

	public ProgressStep Step => currentStep;
	public Image CurrencyIcon => currencyIcon;

	private void Awake() {
		if (buyButton != null) {
			// У Button.prefab аниматор без состояния Disabled — нехватку кристаллов показываем прозрачностью
			buttonGroup = buyButton.GetComponent<CanvasGroup>();
			if (buttonGroup == null)
				buttonGroup = buyButton.gameObject.AddComponent<CanvasGroup>();
		}
	}

	private void OnEnable() {
		InGameEventManager.Subscribe(InGameEvents.OnItemCountChanged, OnItemCountChanged);
		RefreshAffordable();
	}

	private void OnDisable() {
		InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnItemCountChanged);
	}

	public void Init(QuestsWindow window, ProgressStep step) {
		owner = window;
		currentStep = step;
		title.text = LocalizationManager.Localize(step.Title);
		price.text = step.Cost.ToString();
		if (unlockBadge != null)
			unlockBadge.SetActive(step.HasUnlocks);
		SetHighlighted(false);
		RefreshAffordable();
	}

	public void SetHighlighted(bool value) {
		if (highlight != null)
			highlight.SetActive(value);
	}

	private void OnItemCountChanged(object obj) {
		if (obj is ItemType type && type == ItemType.Crystal)
			RefreshAffordable();
	}

	private void RefreshAffordable() {
		if (currentStep == null)
			return;
		bool affordable = currentStep.CanComplete();
		if (price != null)
			price.color = affordable ? affordableColor : notAffordableColor;
		if (buttonGroup != null)
			buttonGroup.alpha = affordable ? 1f : notAffordableButtonAlpha;
	}

#if UNITY_EDITOR
	public void EditorPreview(string titleText, int cost, bool hasUnlock) {
		title.text = titleText;
		price.text = cost.ToString();
		if (unlockBadge != null)
			unlockBadge.SetActive(hasUnlock);
	}
#endif

	// OnClick кнопки с ценой
	public void OnBuyButtonClick() {
		if (currentStep != null && currentStep.CanComplete() && owner != null)
			owner.RequestComplete(this);
	}
}
