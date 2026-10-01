using System;

public class GlobalMapWindow : BaseWindow
{
	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);
		gameObject.SetActive(true);
	}

	public override void Hide() {
		gameObject.SetActive(false);
		base.Hide();
	}

	public void OnCloseClick() {
		MainMenu.Instance.SwitchToLocation(Hide);
	}

}
