using System.Collections.Generic;

// Прогресс катсцен и туториала. Хранит guid'ы (скрытая save-идентичность катсцен
// и шагов туториала, как у ProgressStep), а не человекочитаемые id — переименование
// id в контенте не должно сбрасывать игроку прогресс.
[System.Serializable]
public class SaveDataTutorial : SaveDataBase {
	public List<string> watchedCutsceneIds;
	public List<string> completedTutorialStepIds;
	// Игрок хоть раз заходил в бой (ставит GameController.Start) — с этого флага в хабе
	// стартует цепочка «апгрейд острова»: она рассчитана на возвращение из первой катки
	public bool battleVisited;

	public override void CheckValues() {
		base.CheckValues();
		watchedCutsceneIds ??= new();
		completedTutorialStepIds ??= new();
	}

	public bool IsCutsceneWatched(string guid) {
		return !string.IsNullOrEmpty(guid) && watchedCutsceneIds.Contains(guid);
	}

	public void MarkCutsceneWatched(string guid) {
		if (!string.IsNullOrEmpty(guid) && !watchedCutsceneIds.Contains(guid))
			watchedCutsceneIds.Add(guid);
	}

	public bool IsStepCompleted(string guid) {
		return !string.IsNullOrEmpty(guid) && completedTutorialStepIds.Contains(guid);
	}

	public void CompleteStep(string guid) {
		if (!string.IsNullOrEmpty(guid) && !completedTutorialStepIds.Contains(guid))
			completedTutorialStepIds.Add(guid);
	}
}
