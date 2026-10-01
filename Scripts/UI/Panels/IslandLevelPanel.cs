using Gameplay.IslandExperience;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Уровень острова + прогресс XP до следующего уровня. Живёт в шапке LevelInfoWindow.
/// </summary>
public class IslandLevelPanel : MonoBehaviour
{
	[SerializeField] TMP_Text txtLevel;
	[SerializeField] Slider progressFill;

	private string levelId;

	public void Setup(string levelId) {
		this.levelId = levelId;
		Refresh();
	}

	private void OnEnable() {
		InGameEventManager.Subscribe(InGameEvents.OnIslandXPChanged, OnIslandXPChanged);
		if (!string.IsNullOrEmpty(levelId))
			Refresh();
	}

	private void OnDisable() {
		InGameEventManager.Unsubscribe(InGameEvents.OnIslandXPChanged, OnIslandXPChanged);
	}

	private void OnIslandXPChanged(object payload) {
		if (payload as string == levelId)
			Refresh();
	}

	private void Refresh() {
		if (string.IsNullOrEmpty(levelId))
			return;

		if (txtLevel != null)
			txtLevel.text = IslandExperienceManager.GetLevel(levelId).ToString();
		if (progressFill != null)
			progressFill.value = IslandExperienceManager.IsMaxLevel(levelId)
				? 1f
				: IslandExperienceManager.GetProgressToNextLevel(levelId);
	}
}
