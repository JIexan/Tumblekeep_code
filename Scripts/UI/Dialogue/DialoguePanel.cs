using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using DG.Tweening;
using Gameplay.Cutscenes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialogue {
	// Панель диалога двух персонажей: портрет говорящего слева или справа
	// (сторона — глобальная настройка персонажа), текст по центру с побуквенным
	// появлением. Не окно (AnimationWindow) намеренно: модальная машинерия окон
	// (ref-count HUD, Animator, гварды закрытия) мешает тапу-для-продолжения
	// и конверту катсцены, который сам управляет HUD.
	// Все твины на unscaled time — диалог может идти при timeScale = 0.
	public class DialoguePanel : Singleton<DialoguePanel> {
		[SerializeField] GameObject root;
		[SerializeField] CanvasGroup rootGroup;
		[SerializeField] Image leftPortrait;
		[SerializeField] Image rightPortrait;
		[SerializeField] TMP_Text lineText;
		[SerializeField] TypewriterText typewriter;
		[Tooltip("Затемнение портрета не-говорящего персонажа.")]
		[SerializeField] Color dimColor = new Color(0.45f, 0.45f, 0.45f, 1f);
		[SerializeField] float dimScale = 0.92f;
		[SerializeField] float sideTweenTime = 0.15f;
		[SerializeField] float fadeTime = 0.2f;

		// Катсцена подсвечивает говорящего (talk-анимация на модели).
		public event Action<DialogueLine> OnLineStarted;

		public bool IsPlaying { get; private set; }

		private string conversationId;
		private IReadOnlyList<DialogueLine> lines;
		private int lineIndex;
		private Action onComplete;

		public void Play(string conversationId, IReadOnlyList<DialogueLine> lines, Action onComplete) {
			if (IsPlaying) {
				Debug.LogWarning("[DialoguePanel] Play вызван поверх активного диалога — предыдущий завершается мгновенно.");
				CompleteImmediately();
			}

			if (lines == null || lines.Count == 0) {
				onComplete?.Invoke();
				return;
			}

			this.conversationId = conversationId;
			this.lines = lines;
			this.onComplete = onComplete;
			lineIndex = 0;
			IsPlaying = true;

			leftPortrait.gameObject.SetActive(false);
			rightPortrait.gameObject.SetActive(false);
			root.SetActive(true);
			rootGroup.alpha = 0f;
			rootGroup.DOFade(1f, fadeTime).SetUpdate(true).SetTarget(this);
			ShowCurrentLine();
		}

		// Вызывается тап-кетчером (Button на весь экран).
		public void OnTapAdvance() {
			if (!IsPlaying)
				return;

			if (typewriter.IsRevealing) {
				typewriter.RevealAll();
				return;
			}

			lineIndex++;
			if (lineIndex >= lines.Count)
				FinishDialogue(instant: false);
			else
				ShowCurrentLine();
		}

		// Для Skip катсцены: без анимаций, сразу в конец.
		public void CompleteImmediately() {
			if (!IsPlaying)
				return;
			FinishDialogue(instant: true);
		}

		private void ShowCurrentLine() {
			var line = lines[lineIndex];
			var settings = GameConfigs.Instance.DialogueCharactersSettings;

			var side = settings != null ? settings.GetSide(line.Character) : DialogueCharactersSettings.PanelSide.Left;
			var speaker = side == DialogueCharactersSettings.PanelSide.Left ? leftPortrait : rightPortrait;
			var listener = side == DialogueCharactersSettings.PanelSide.Left ? rightPortrait : leftPortrait;

			var sprite = settings != null ? settings.GetPortrait(line.Character, line.Emotion) : null;
			if (sprite != null) {
				speaker.sprite = sprite;
				speaker.gameObject.SetActive(true);
			}

			SetPortraitState(speaker, active: true);
			if (listener.gameObject.activeSelf)
				SetPortraitState(listener, active: false);

			typewriter.Play(LocalizationManager.Localize(line.LocalizationKey(conversationId)));
			OnLineStarted?.Invoke(line);
		}

		private void SetPortraitState(Image portrait, bool active) {
			portrait.DOKill();
			portrait.DOColor(active ? Color.white : dimColor, sideTweenTime).SetUpdate(true);
			portrait.rectTransform.DOScale(active ? 1f : dimScale, sideTweenTime).SetUpdate(true);
		}

		private void FinishDialogue(bool instant) {
			IsPlaying = false;
			typewriter.Clear();
			DOTween.Kill(this);

			var callback = onComplete;
			onComplete = null;
			lines = null;

			if (instant) {
				root.SetActive(false);
				callback?.Invoke();
			} else {
				rootGroup.DOFade(0f, fadeTime).SetUpdate(true).SetTarget(this).OnComplete(() => {
					root.SetActive(false);
					callback?.Invoke();
				});
			}
		}
	}
}
