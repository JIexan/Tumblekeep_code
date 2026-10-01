using UnityEngine;
using UnityEngine.UI;

public class NextWaveButton : WorldTrackingPanel
{
	public override void OnPanelClicked() {
		base.OnPanelClicked();

		float remaining = TimeRemaining;
		float total = remaining + TimerProgress;
		CoinsManager.Instance.Add(Mathf.RoundToInt((1f - TimerProgress) * LevelDesriptor.Instance.cointForWaveSpeedUp));

		Hide();
		GameController.Instance.StartNextWave();
	}
}
