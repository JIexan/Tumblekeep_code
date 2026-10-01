using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Заголовок классической вкладки: выбранная — яркая и выше соседних, остальные притоплены и затемнены.
// Сама ничего не переключает — кликом и выбором управляет владелец панели (см. LevelInfoTabs).
public class TabHeaderButton : MonoBehaviour {
	[SerializeField] Button button;
	[SerializeField] RectTransform body; // прижат к низу вкладки, у выбранной вырастает вверх
	[SerializeField] Image back;
	[SerializeField] Image shade;
	[SerializeField] TMP_Text label;
	[SerializeField] RectTransform badge; // индикатор «!» — на вкладке есть что-то, требующее внимания

	[Header("Выбрана")]
	[SerializeField] float selectedHeight = 84f;
	[SerializeField] Color selectedBack = new Color(0.8745f, 0.7176f, 0.2078f, 1f);
	[SerializeField] Color selectedShade = new Color(0.6f, 0.4941f, 0.1412f, 1f);
	[SerializeField] Color selectedLabel = new Color(0.3412f, 0.2824f, 0.0784f, 1f);

	[Header("Не выбрана")]
	[SerializeField] float deselectedHeight = 66f;
	[SerializeField] Color deselectedBack = new Color(0.26f, 0.29f, 0.35f, 1f);
	[SerializeField] Color deselectedShade = new Color(0.17f, 0.19f, 0.235f, 1f);
	[SerializeField] Color deselectedLabel = new Color(0.6863f, 0.7373f, 0.8f, 1f);

	[Header("Пульс индикатора")]
	[SerializeField] float badgePulseScale = 1.2f;
	[SerializeField] float badgePulseDuration = 0.45f;

	private Tween badgePulse;

	public Button Button => button;

	public void SetBadge(bool visible) {
		if (badge == null)
			return;
		badge.gameObject.SetActive(visible);
		UpdateBadgePulse();
	}

	private void OnEnable() {
		UpdateBadgePulse();
	}

	private void OnDisable() {
		badgePulse?.Kill();
		badgePulse = null;
		if (badge != null)
			badge.localScale = Vector3.one;
	}

	private void UpdateBadgePulse() {
		bool pulse = badge != null && badge.gameObject.activeSelf && isActiveAndEnabled;
		if (pulse == (badgePulse != null))
			return;
		if (pulse) {
			badgePulse = badge.DOScale(badgePulseScale, badgePulseDuration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
		} else {
			badgePulse.Kill();
			badgePulse = null;
			badge.localScale = Vector3.one;
		}
	}

	public void SetSelected(bool value) {
		if (button != null)
			button.interactable = !value;
		if (body != null)
			body.sizeDelta = new Vector2(body.sizeDelta.x, value ? selectedHeight : deselectedHeight);
		if (back != null)
			back.color = value ? selectedBack : deselectedBack;
		if (shade != null)
			shade.color = value ? selectedShade : deselectedShade;
		if (label != null)
			label.color = value ? selectedLabel : deselectedLabel;
	}
}
