using DG.Tweening;
using Gameplay.TasksSystem.Tasks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskPanel : MonoBehaviour
{
	[SerializeField] TMP_Text description;
	[SerializeField] Image icon;
	[SerializeField] Sprite completedIcon;
	[SerializeField] Sprite nonCompletedIcon;

	[Header("Цвета текста")]
	[SerializeField] Color normalColor = Color.white;
	[SerializeField] Color completedColor = Color.green;
	[SerializeField] Color failedColor = Color.red;

	// HUD катки: карточка сама себе прогресс-бар (заливка под текстом), по выполнению перекрашивается.
	// В окне уровня (showProgress = false) поля пустые
	[Header("Прогресс (HUD)")]
	[SerializeField] Image progressFill;
	[SerializeField] Color progressCompletedColor = new Color(0.412f, 0.58f, 0f, 0.45f);
	[SerializeField] Graphic iconBack;
	[SerializeField] Color iconBackCompletedColor = new Color(0.412f, 0.58f, 0f, 1f);
	[SerializeField] Color iconCompletedColor = Color.white;

	private AbstractTask task;
	private bool wasCompleted;
	private Color progressColor;
	private Color iconBackColor;
	private Color iconColor;
	private bool colorsCached;

	public void Init(AbstractTask task, bool showProgress) {
		if (this.task != null) {
			this.task.OnTaskStateChanged -= UpdateState;
		}
		CacheColors();
		// Панель из пула могла остаться от выполненного таска — «пинок» только на переходе в этой катке
		wasCompleted = task.IsCompleted;
		if (showProgress) {
			this.task = task;
			task.OnTaskStateChanged += UpdateState;
			UpdateState(task.CurrentValue, task.TargetValue);
		} else {
			this.task = task;
			UpdateState(-1,task.TargetValue);
		}
	}

	internal void UpdateState(int currentCalue, int targetValue) {
		if (task != null) {
			bool showProgress = currentCalue != -1;
			if (task.IsCompleted) {
				description.color = completedColor;
				description.fontStyle = FontStyles.Normal;
				icon.sprite = completedIcon;
			} else if (task.IsFailed) {
				description.color = failedColor;
				description.fontStyle = FontStyles.Strikethrough;
				icon.sprite = nonCompletedIcon;
			} else {
				description.color = normalColor;
				description.fontStyle = FontStyles.Normal;
				icon.sprite = showProgress ? nonCompletedIcon : completedIcon;
			}

			// Формат строки — дело таска: у доп. тасков цель стоит внутри локализованной фразы
			description.text = task.GetDescription(showProgress);

			UpdateProgressVisual(showProgress, currentCalue, targetValue);
		}
	}

	private void UpdateProgressVisual(bool showProgress, int currentValue, int targetValue) {
		bool completed = showProgress && task.IsCompleted;
		if (progressFill != null) {
			float progress = completed ? 1f : showProgress && targetValue > 0 ? Mathf.Clamp01(currentValue / (float)targetValue) : 0f;
			progressFill.enabled = progress > 0.001f;
			// Ширина якорями, а не Image.Filled: у заливки sliced-спрайт
			progressFill.rectTransform.anchorMax = new Vector2(progress, 1f);
			progressFill.color = completed ? progressCompletedColor : progressColor;
		}
		if (iconBack != null)
			iconBack.color = completed ? iconBackCompletedColor : iconBackColor;
		if (showProgress)
			icon.color = completed ? iconCompletedColor : iconColor;

		if (completed && !wasCompleted) {
			transform.DOKill(true);
			transform.DOPunchScale(Vector3.one * 0.08f, 0.35f, 6).SetUpdate(true);
		}
		wasCompleted = task.IsCompleted;
	}

	// Исходные цвета — из префаба: к ним возвращаемся, когда панель из пула получает невыполненный таск
	private void CacheColors() {
		if (colorsCached)
			return;
		colorsCached = true;
		if (progressFill != null)
			progressColor = progressFill.color;
		if (iconBack != null)
			iconBackColor = iconBack.color;
		iconColor = icon.color;
	}

	private void OnDestroy() {
		transform.DOKill();
		if (task != null) {
			task.OnTaskStateChanged -= UpdateState;
		}
	}
}

