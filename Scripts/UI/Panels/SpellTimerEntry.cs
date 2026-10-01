using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Одна строка панели таймеров спеллов: иконка свитка + оставшиеся секунды</summary>
public class SpellTimerEntry : MonoBehaviour
{
	[SerializeField] private Image icon;
	[SerializeField] private TMP_Text timerText;

	public ItemType SpellType { get; private set; }
	public TimedSpellEffect Effect { get; private set; }

	public void Init(ItemType spellType, TimedSpellEffect effect) {
		SpellType = spellType;
		Effect = effect;
		if (icon != null) {
			var sprite = ItemIconsManager.Instance != null ? ItemIconsManager.Instance.GetIcon(spellType) : null;
			icon.sprite = sprite;
			icon.enabled = sprite != null;
		}
		Refresh();
	}

	public void Refresh() {
		if (timerText != null && Effect != null)
			timerText.text = Mathf.CeilToInt(Effect.RemainingTime).ToString();
	}
}
