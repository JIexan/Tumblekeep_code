using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Gameplay.RestorationSystem;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[System.Serializable]
public class ProgressRegion
{
	public enum RegionState { Destroyed, InProgress, Repaired }

	[SerializeField] RegionId id;
	[SerializeField] bool startUnlocked;
	[SerializeField] Transform root;
	[SerializeField] AssetReferenceGameObject destroyedPrefab;
	[SerializeField] AssetReferenceGameObject inProgressPrefab;
	[SerializeField] AssetReferenceGameObject repairedPrefab;

	public RegionId Id => id;
	public string Title => "RegionTitle" + id.ToSaveId();
	public bool StartUnlocked => startUnlocked;
	public RegionState State => currentState;

	// Живой счёт по загруженному контенту (для UI хаба). Для гейтинга вне хаба
	// используется счёт из сейва — см. RegionProgressUpgradeRequirement.
	public int CompletedStepsCount => steps != null ? CountCompletedExisting() : 0;
	public int TotalStepsCount => steps?.Count ?? 0;

	private RegionState currentState = RegionState.Destroyed;
	private bool hasInstance;
	private GameObject instance;
	private List<ProgressStep> steps;

	public RegionState ResolveState() {
		var saveData = SavesManager.Profile.playerLocation;
		if (!saveData.IsUnlocked(id))
			return RegionState.Destroyed;
		if (saveData.IsCompleted(id))
			return RegionState.Repaired;
		return RegionState.InProgress;
	}

	// Loads (or reloads) the prefab matching the region's resolved save state.
	public async UniTask LoadForCurrentState() {
		await LoadState(ResolveState());
	}

	private async UniTask LoadState(RegionState state) {
		Unload();
		currentState = state;

		AssetReferenceGameObject reference = GetReference(state);
		if (reference == null || !reference.RuntimeKeyIsValid())
			return;

		AsyncOperationHandle<GameObject> handle = reference.InstantiateAsync(root);
		instance = await handle;
		hasInstance = true;

		if (state == RegionState.InProgress)
			InitSteps();
	}

	private void InitSteps() {
		var content = instance != null ? instance.GetComponentInChildren<ProgressRegionContent>() : null;
		if (content == null) {
			steps = null;
			return;
		}

		// Own copy scrubbed of broken entries — bad content must never soft-lock the player.
		steps = new List<ProgressStep>();
		foreach (var step in content.Steps) {
			if (step == null) {
				Debug.LogWarning($"[ProgressRegion] '{id}': null-шаг в реестре контента, пропущен.");
				continue;
			}
			step.SetParent(this);
			steps.Add(step);
		}
		if (steps.Count == 0)
			return;

		// Снимок тотала в сейв: процент готовности региона должен считаться из любой сцены.
		SavesManager.Profile.playerLocation.SetTotalSteps(id, steps.Count);

		// Instantly restore already-completed steps; topological order guarantees
		// a step's prerequisites are applied before the step itself.
		foreach (var step in ProgressGraphUtility.TopologicalOrder(steps)) {
			if (IsStepCompleted(step))
				step.Apply();
		}

		// Self-heal: if every step is already done, flag the region repaired for the next load.
		if (CountCompletedExisting() >= steps.Count)
			SavesManager.Profile.playerLocation.SetCompleted(id);
	}

	private void Unload() {
		steps = null;
		if (hasInstance) {
			if (instance != null)
				Addressables.ReleaseInstance(instance);
			instance = null;
			hasInstance = false;
		}
	}

	private AssetReferenceGameObject GetReference(RegionState state) {
		switch (state) {
			case RegionState.Destroyed: return destroyedPrefab;
			case RegionState.InProgress: return inProgressPrefab;
			case RegionState.Repaired: return repairedPrefab;
			default: return null;
		}
	}

	public bool IsStepCompleted(ProgressStep step) {
		return step != null && SavesManager.Profile.playerLocation.IsStepCompleted(id, step.Guid);
	}

	// Completed steps that still exist in the content — stale guids of deleted steps
	// must neither block region completion nor falsely complete it.
	private int CountCompletedExisting() {
		int count = 0;
		foreach (var step in steps) {
			if (IsStepCompleted(step))
				count++;
		}
		return count;
	}

	// All steps currently available to the player: not completed, all prerequisites done.
	public IReadOnlyList<ProgressStep> GetFrontierSteps() {
		if (currentState != RegionState.InProgress || steps == null)
			return System.Array.Empty<ProgressStep>();

		return ProgressGraphUtility.GetFrontier(steps, IsStepCompleted);
	}

	// Marks the region as fully repaired in the save once the last step is done.
	public void OnStepCompleted() {
		if (steps == null)
			return;

		if (CountCompletedExisting() >= steps.Count) {
			SavesManager.Profile.playerLocation.SetCompleted(id);
			SavesManager.Instance.SaveProfile();
		}
	}

#if CHEATS
	// Чит: закрывает в сейве все шаги загруженной InProgress-зоны и помечает её восстановленной.
	// Apply() нужен ради Unlock*-экшенов (анлоки контента/зон пишутся в профиль) — сейв выходит таким же,
	// как при честном прохождении. Панельки шагов снимаются здесь: после перезагрузки зоны ключи-шаги умрут.
	// Сохранение и перезагрузка префаба — на вызывающем (PlayerProgressController.CheatCompleteRegions)
	public void CheatCompleteAllSteps() {
		var loc = SavesManager.Profile.playerLocation;
		if (steps != null) {
			foreach (var step in ProgressGraphUtility.TopologicalOrder(steps)) {
				InGameEventManager.SendEvent(InGameEvents.OnUntrackingRequested, new UntrackingRequest(step));
				if (IsStepCompleted(step))
					continue;
				loc.CompleteStep(id, step.Guid);
				step.Apply();
			}
		}
		loc.SetCompleted(id);
	}
#endif
}
