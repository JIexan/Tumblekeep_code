using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class AnimationWindow : BaseWindow
{
	public static float CLOSE_DELAY = 0.2f;
	public static float EMPTY_SPACE_CLOSE_DELAY = 0.7f;

	public bool CloseByEmptySpaceTap = false;
	public bool HasClosingAnimation = true;

	[HideInInspector]
	public bool CanBeClosed = false;
	[HideInInspector]
	public bool IsOpening = false;
	[HideInInspector]
	public bool IsClosing = false;

	[SerializeField] Animator animator;

	private CanvasGroup inputGroup;

	// Клип закрытия анимирует только alpha/scale — кнопки закрывающегося окна ещё ~0.3 с кликабельны
	// (двойной клик успевал взять второй апгрейд / купить второй товар). Окна, где это критично,
	// блокируют ввод на время закрытия и возвращают в Show. Именно interactable, а не blocksRaycasts:
	// окно должно и дальше перекрывать мир (InputController смотрит IsPointerOverGameObject), иначе
	// клик по башне переоткрыл бы окно посреди WindowClose, и его тут же выключил бы DeactivateWindow.
	protected void SetInputLocked(bool locked) {
		if (inputGroup == null)
			inputGroup = GetComponent<CanvasGroup>();
		if (inputGroup != null)
			inputGroup.interactable = !locked;
	}

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);
		IsClosing = false;
		CloseByEmptySpaceTap = false;

		if (animator != null && animator.isActiveAndEnabled)
			animator.Update(0f);	
	}

	public override void Hide() {
		if (!CanBeClosed) return;

		IsClosing = true;

		if (animator && HasClosingAnimation)
			animator.Play("WindowClose");
		else
			DeactivateWindow();
		base.Hide();
	}

	public void CloseImmediate() {
		if (!CanBeClosed) return;

		IsClosing = true;

		DeactivateWindow();
	}

	public virtual void emptySpaceTap() {
		if (!CanBeClosed) return;

		if (CloseByEmptySpaceTap)
			Hide();
	}

	public virtual void DeactivateWindow() {
		IsClosing = false;

		gameObject.SetActive(false);
	}

	public virtual void WindowOpened() {
		IsOpening = false;

		setWindowClosingFlag();
		SetEmptySpaceCloseFlag();
	}

	protected virtual async void SetEmptySpaceCloseFlag() {
		await UniTask.Delay(TimeSpan.FromSeconds(EMPTY_SPACE_CLOSE_DELAY), ignoreTimeScale: true);
		CloseByEmptySpaceTap = true;
	}


	private async void setWindowClosingFlag() {
		await UniTask.Delay(TimeSpan.FromSeconds(CLOSE_DELAY), ignoreTimeScale: true);
		OnWindowClosingFlagSet();
		CanBeClosed = true;
	}

	public virtual void OnWindowClosingFlagSet() {

	}
}
