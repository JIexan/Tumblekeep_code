using Assets.SimpleLocalization;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradePanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] Image frame;
	[SerializeField] Image icon;
	[SerializeField] TMP_Text title;
	[SerializeField] TMP_Text description;
	[SerializeField] TMP_Text valueLabel;

	[Header("Hover")]
	[SerializeField, Min(1f)] float hoverScale = 1.035f;
	[SerializeField, Min(0f)] float hoverDuration = 0.16f;
	[SerializeField, Min(0f)] float hoverExitDuration = 0.12f;

	UpgradeWindow window;
	UpgradeData data;
	Transform hoverTarget;
	Vector3 normalScale;
	Tween hoverTween;
	bool hovered;

	private CharacteristicSettings characteristicSettings => GameConfigs.Instance.CharacteristicSettings;
	private RarityConfig rarityConfig => GameConfigs.Instance.RarityConfig;

	private void Awake() {
		CacheHoverTarget();
	}

	private void CacheHoverTarget() {
		if (hoverTarget != null)
			return;
		// The button's visual has a centered pivot; the layout root is anchored to its edge.
		var button = GetComponent<Button>();
		hoverTarget = button != null && button.targetGraphic != null ? button.targetGraphic.transform : transform;
		normalScale = hoverTarget.localScale;
	}

	private void OnDisable() {
		ResetHover();
	}

	public void Init(UpgradeWindow window, UpgradeData item) {
		bool restoreHover = hovered;
		ResetHover();
		this.window = window;
		data = item;

		switch (item) {
			case CharacteristicUpgradeData derived1:
				Init(derived1);
				break;
			case TowerTraitUpgradeData derived2:
				Init(derived2);
				break;
			default:
				throw new ArgumentException($"Unknown type: {item.GetType()}");
		}

		// A reroll can replace this option without moving the pointer.
		if (restoreHover)
			ShowHover();
	}

	public void OnPointerEnter(PointerEventData eventData) {
		ShowHover();
	}

	public void OnPointerExit(PointerEventData eventData) {
		ResetHover(false);
	}

	private void ShowHover() {
		if (hovered || data == null || window == null || window.IsClosing || !isActiveAndEnabled)
			return;

		CacheHoverTarget();
		hovered = true;
		hoverTween?.Kill();
		hoverTween = hoverTarget.DOScale(normalScale * hoverScale, hoverDuration)
			.SetEase(Ease.OutCubic).SetUpdate(true);
		window.ShowUpgradePreview(this, data);
	}

	public void ResetHover(bool immediate = true) {
		bool wasHovered = hovered;
		hovered = false;
		hoverTween?.Kill();
		hoverTween = null;
		if (hoverTarget != null) {
			if (immediate || !isActiveAndEnabled)
				hoverTarget.localScale = normalScale;
			else
				hoverTween = hoverTarget.DOScale(normalScale, hoverExitDuration)
					.SetEase(Ease.OutCubic).SetUpdate(true);
		}
		if (wasHovered && window != null)
			window.ClearUpgradePreview(this);
	}

	private void Init(CharacteristicUpgradeData data) {
		var settings = characteristicSettings.Get(data.characteristicType);
		if (settings == null)
			return;

		var raritySettings = rarityConfig.Get(data.rarity);
		if (raritySettings != null) {
			frame.color = raritySettings.c;
		} else {
			frame.color = Color.white;
		}

		icon.sprite = settings.icon;
		valueLabel.gameObject.SetActive(true);
		description.gameObject.SetActive(false);
		if (data.additionalValue != 0f) {
			valueLabel.text = (settings.positiveIncrease ? "+" : "-") + data.additionalValue.ToString("0.##") + " " + LocalizationManager.Localize(settings.unitsLocale);
		} else {
			valueLabel.text = (settings.positiveIncrease ? "+" : "-") + data.persentValue.ToString() + "%";
		}
		title.text = LocalizationManager.Localize(data.characteristicType.ToString() + "Name");
	}

	private void Init(TowerTraitUpgradeData data) {
		frame.color = Color.white;
		icon.sprite = null;
		valueLabel.gameObject.SetActive(false);
		description.gameObject.SetActive(true);
		// Ключи общие с книгой: TraitName_{Trait} / TraitDesc_{Trait}
		description.text = LocalizationManager.Localize("TraitDesc_" + data.trait);
		title.text = LocalizationManager.Localize("TraitName_" + data.trait);
	}

	public void OnButtonClick() {
		// Пока окно нельзя закрыть (первые мгновения после открытия) или оно уже закрывается, выбор не принимаем:
		// иначе апгрейд применился бы, а Hide() отказал — и можно было бы взять второй
		if(data != null && window.CanBeClosed && !window.IsClosing) {
			data.Upgrade();
			window.Hide();
		}
	}
}
