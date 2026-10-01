using Assets.SimpleLocalization;
using TMPro;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;

// Слот реликвии на HUD: иконка, рамка цвета рарности, бейдж «×N» при стаках > 1.
// Название и описание — подсказкой по наведению.
public class RelicSlot : MonoBehaviour {
	[SerializeField] Image frame;
	[SerializeField] Image icon;
	[SerializeField] TMP_Text countText;
	[SerializeField] TooltipTrigger tooltip;

	public void Init(Gameplay.Relics.RelicDefinition definition, int stacks, Color frameColor) {
		if (frame != null)
			frame.color = frameColor;
		if (icon != null)
			icon.sprite = definition.Icon;
		if (countText != null) {
			countText.gameObject.SetActive(stacks > 1);
			countText.text = "×" + stacks;
		}
		if (tooltip != null) {
			tooltip.SetTitled(
				() => LocalizationManager.Localize(definition.NameKey),
				() => LocalizationManager.Localize(definition.DescKey));
		}
	}
}
