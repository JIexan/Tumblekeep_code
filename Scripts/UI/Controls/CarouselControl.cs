using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// «Карусель» — замена выпадающему списку: кнопки по краям, значение посередине,
// листается по кругу. Тексты вариантов задаёт владелец (уже локализованные).
public class CarouselControl : MonoBehaviour {
	[SerializeField] Button prevButton;
	[SerializeField] Button nextButton;
	[SerializeField] TMP_Text label;
	[SerializeField] CanvasGroup group; // приглушает контрол, когда он недоступен

	[Header("Анимация смены значения")]
	[SerializeField] float slideDistance = 60f;
	[SerializeField] float slideDuration = 0.15f;
	[SerializeField] float disabledAlpha = 0.35f;

	private readonly List<string> options = new List<string>();
	private int index;
	private bool interactable = true;
	private Sequence slide;

	public int Index => index;
	public int Count => options.Count;

	// Срабатывает только на действия игрока, не на SetOptions/SetIndex
	public event Action<int> OnValueChanged;

	public bool Interactable {
		get { return interactable; }
		set {
			interactable = value;
			RefreshButtons();
		}
	}

	private void Awake() {
		prevButton.onClick.AddListener(() => Step(-1));
		nextButton.onClick.AddListener(() => Step(1));
	}

	private void OnDisable() {
		KillSlide();
	}

	public void SetOptions(IList<string> texts, int selected) {
		options.Clear();
		options.AddRange(texts);
		SetIndex(selected);
	}

	public void SetIndex(int value) {
		index = options.Count > 0 ? Mathf.Clamp(value, 0, options.Count - 1) : 0;
		KillSlide();
		RefreshLabel();
		RefreshButtons();
	}

	private void Step(int direction) {
		if (!interactable || options.Count < 2)
			return;
		index = (index + direction + options.Count) % options.Count;
		RefreshLabel();
		OnValueChanged?.Invoke(index);
		PlaySlide(direction); // после события: владелец в ответ часто пересобирает варианты (SetOptions гасит анимацию)
	}

	private void RefreshLabel() {
		label.text = options.Count > 0 ? options[index] : string.Empty;
	}

	private void RefreshButtons() {
		bool canStep = interactable && options.Count > 1;
		prevButton.interactable = canStep;
		nextButton.interactable = canStep;
		if (group != null)
			group.alpha = interactable ? 1f : disabledAlpha;
	}

	// Новое значение въезжает с той стороны, куда нажали
	private void PlaySlide(int direction) {
		KillSlide();
		var rect = label.rectTransform;
		rect.anchoredPosition = new Vector2(direction * slideDistance, rect.anchoredPosition.y);
		label.alpha = 0f;
		slide = DOTween.Sequence()
			.Join(rect.DOAnchorPosX(0f, slideDuration).SetEase(Ease.OutCubic))
			.Join(DOTween.To(() => label.alpha, a => label.alpha = a, 1f, slideDuration))
			.SetUpdate(true)
			.OnKill(() => slide = null);
	}

	private void KillSlide() {
		slide?.Kill();
		var rect = label.rectTransform;
		rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
		label.alpha = 1f;
	}
}
