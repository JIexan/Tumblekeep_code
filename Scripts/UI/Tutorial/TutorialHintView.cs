using System;
using Assets.SimpleLocalization;
using Gameplay.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Tutorial {
	// Визуал туториальной реплики (портрет с эмоцией + текст) — общий для обоих
	// врапперов: мирового (TutorialHintPanel) и UI-шного (UiHintFollower).
	// Опционально: приписка курсивом под репликой и кнопка «Ок» — единственный
	// кликабельный элемент пузыря (всё остальное прозрачно для кликов).
	// Высота пузыря растёт по контенту (ContentSizeFitter в префабе).
	public class TutorialHintView : MonoBehaviour {
		[SerializeField] Image portrait;
		[SerializeField] TMP_Text text;
		[SerializeField] TMP_Text noteText;
		[SerializeField] Button okButton;

		public void Setup(TutorialStep step, Action onOk) {
			var line = step.Line;
			var settings = GameConfigs.Instance.DialogueCharactersSettings;
			var sprite = settings != null ? settings.GetPortrait(line.Character, line.Emotion) : null;
			portrait.sprite = sprite;
			portrait.gameObject.SetActive(sprite != null);
			text.text = LocalizationManager.Localize(line.LocalizationKey(step.Id));

			if (noteText != null) {
				noteText.gameObject.SetActive(step.HasNote);
				if (step.HasNote)
					noteText.text = LocalizationManager.Localize(step.NoteLocalizationKey);
			}

			if (okButton != null) {
				bool showOk = step.OkButton && onOk != null;
				okButton.gameObject.SetActive(showOk);
				okButton.onClick.RemoveAllListeners();
				if (showOk)
					okButton.onClick.AddListener(() => onOk());
			}

			// Врапперы сразу после Setup читают размер пузыря (кламп в экран) —
			// не ждём канвасного ребилда следующего кадра
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
		}
	}
}
