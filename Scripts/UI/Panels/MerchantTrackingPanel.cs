using UnityEngine;

public class MerchantTrackingPanel : WorldTrackingPanel
{
	protected override void OnTimerExpired() {
		base.OnTimerExpired();
		// При истечении таймера панель скрывается автоматически
		Hide();
	}
}