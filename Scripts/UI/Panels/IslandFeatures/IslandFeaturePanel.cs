using Assets.SimpleLocalization;
using Gameplay.IslandFeatures;
using TMPro;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;

// Лента-флаг вкладки «Остров» в LevelInfoWindow: особенность острова вместо описания уровня.
// Строка 1 — «Особенность острова: {название}» золотом, строка 2 — короткая механика (Desc);
// развёрнутая подсказка (Hint) — по наведению на ленту. У уровня без особенности — прежнее описание.
public class IslandFeaturePanel : MonoBehaviour {
	private const string LabelKey = "IslandFeature_Label";
	private const string TitleColor = "#DFB735"; // золото шапки окна

	[SerializeField] Image icon;
	[SerializeField] TMP_Text text;
	[SerializeField] TooltipTrigger tooltip;

	private Level level;

	public void Setup(Level level) {
		this.level = level;
		Refresh();
	}

	private void OnEnable() {
		LocalizationManager.LocalizationChanged += Refresh;
		Refresh();
	}

	private void OnDisable() {
		LocalizationManager.LocalizationChanged -= Refresh;
	}

	private void Refresh() {
		if (level == null)
			return;

		var feature = IslandFeaturesManager.GetFeature(level.Id);
		if (icon != null) {
			icon.gameObject.SetActive(feature != null && feature.Icon != null);
			if (feature != null)
				icon.sprite = feature.Icon;
		}

		if (feature == null) {
			// LevelDesc_{Id} из таблицы локализации; нет ключа — сырое поле из LevelSettings
			if (text != null)
				text.text = BookLoc.TryLocalize("LevelDesc_" + level.Id, out var levelDesc) ? levelDesc : level.infoLvl;
			if (tooltip != null)
				tooltip.ClearContent();
			return;
		}

		if (text != null) {
			text.text = $"<color={TitleColor}>{LocalizationManager.Localize(LabelKey)}: {LocalizationManager.Localize(feature.NameKey)}</color>\n" +
				$"<size=80%>{LocalizationManager.Localize(feature.DescKey)}</size>";
		}
		if (tooltip != null) {
			tooltip.SetTitled(
				() => LocalizationManager.Localize(feature.NameKey),
				() => LocalizationManager.Localize(feature.HintKey));
		}
	}
}
