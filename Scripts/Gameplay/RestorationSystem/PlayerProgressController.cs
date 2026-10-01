using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Gameplay.RestorationSystem;
using UnityEngine;

public class PlayerProgressController : Singleton<PlayerProgressController>
{
	[SerializeField] List<ProgressRegion> regions;
	[SerializeField] TrackingPanelsController trackingPanelsController;
	[SerializeField] RegionUnlockSettings unlockSettings;

	public List<ProgressRegion> Regions => regions;

	// Правило открытия зоны (условия по предшественникам) — для подписи «когда откроется» в окне заданий;
	// null — зона открывается только через startUnlocked/UnlockRegionAction
	public RegionUnlockSettings.Rule GetUnlockRule(RegionId regionId) {
		if (unlockSettings == null)
			return null;
		foreach (var rule in unlockSettings.Rules) {
			if (rule != null && rule.Region == regionId)
				return rule;
		}
		return null;
	}

	private bool initialized;

	// Зоны догружены (Addressables) и фронтир известен — до этого GetFrontierSteps пуст
	public bool IsInitialized => initialized;

	// Самый дешёвый доступный шаг зоны (null — зона не в работе или ещё не загружена).
	// На него туториал вешает реплику «восстановим это» и по нему считает подарок кристаллов.
	public ProgressStep GetCheapestFrontierStep(RegionId regionId) {
		ProgressStep best = null;
		foreach (var region in regions) {
			if (region.Id != regionId)
				continue;
			foreach (var step in region.GetFrontierSteps()) {
				if (step != null && (best == null || step.Cost < best.Cost))
					best = step;
			}
		}
		return best;
	}

	void Start() {
		InGameEventManager.Subscribe(InGameEvents.OnQuestCompleted, OnQuestCompleted);
		InitAsync().Forget();
	}

	private async UniTaskVoid InitAsync() {
		var loc = SavesManager.Profile.playerLocation;
		foreach (var region in regions) {
			if (region.StartUnlocked)
				loc.SetUnlocked(region.Id);
		}
		ApplyUnlockRules();
		SavesManager.Instance.SaveProfile();

		var tasks = new List<UniTask>(regions.Count);
		foreach (var region in regions)
			tasks.Add(region.LoadForCurrentState());
		await UniTask.WhenAll(tasks);

		// Apply() завершённых шагов (InitSteps) идёт после SaveProfile выше: если Unlock*Action
		// «доподнял» анлок (экшен добавлен в уже пройденный шаг), досохраняем — иначе он пропадёт
		// при загрузке уровня (SavesManager per-scene перечитывает профиль с диска)
		if (Gameplay.Unlocks.ContentUnlocksManager.ConsumeDirty())
			SavesManager.Instance.SaveProfile();

		initialized = true;
		UpdateState();
		InGameEventManager.SendEvent(InGameEvents.OnHubProgressReady);
	}

	private void OnDestroy() {
		InGameEventManager.Unsubscribe(InGameEvents.OnQuestCompleted, OnQuestCompleted);
	}

	private void OnQuestCompleted(object obj) {
		if (obj is ProgressStep completedStep) {
			InGameEventManager.SendEvent(InGameEvents.OnUntrackingRequested, new UntrackingRequest(completedStep));
			completedStep.Region?.OnStepCompleted();
		}
		// Шаг сохранился до события — анлок новой зоны нужно досохранить отдельно.
		if (ApplyUnlockRules())
			SavesManager.Instance.SaveProfile();
		RefreshRegionsAsync().Forget();
	}

	// Процентный гейт очерёдности зон (RegionUnlockSettings): открывает все зоны,
	// у которых выполнены условия по предшественникам. Анлок монотонный, каскада нет
	// (открытие зоны не меняет проценты других) — одного прохода достаточно.
	private bool ApplyUnlockRules() {
		if (unlockSettings == null)
			return false;

		bool changed = false;
		var loc = SavesManager.Profile.playerLocation;
		foreach (var rule in unlockSettings.Rules) {
			if (rule == null || loc.IsUnlocked(rule.Region))
				continue;
			if (rule.AllConditionsMet()) {
				loc.SetUnlocked(rule.Region);
				AnnounceRegionUnlock(rule.Region);
				changed = true;
			}
		}
		return changed;
	}

	// Called by UnlockRegionAction to activate a region by id.
	public void UnlockRegion(RegionId regionId) {
		var loc = SavesManager.Profile.playerLocation;
		bool wasUnlocked = loc.IsUnlocked(regionId);
		loc.SetUnlocked(regionId);

		// During initial load each region resolves its own state, so skip mid-init reloads.
		if (!initialized)
			return;

		if (!wasUnlocked)
			AnnounceRegionUnlock(regionId);
		SavesManager.Instance.SaveProfile();
		RefreshRegionsAsync().Forget();
	}

	// Попап «новая зона» — той же очередью, что анонсы контента: ProgressStep.CompleteWithEffects зовёт
	// ShowPending() сразу после OnQuestCompleted, так что и правила, и UnlockRegionAction попадают в
	// текущую серию окон (после предметов шага). На старте хаба (initialized == false) не анонсируем.
	private void AnnounceRegionUnlock(RegionId regionId) {
		if (!initialized)
			return;
		Gameplay.Unlocks.ContentUnlockAnnouncer.Enqueue(new Gameplay.Unlocks.ContentUnlock(Gameplay.Unlocks.UnlockKind.Region, regionId.ToSaveId()));
	}

	private async UniTaskVoid RefreshRegionsAsync() {
		foreach (var region in regions) {
			if (region.ResolveState() != region.State)
				await region.LoadForCurrentState();
		}
		UpdateState();
	}

	private void UpdateState() {
		foreach (var region in regions) {
			// Every frontier step gets its own tracking panel (parallel branches).
			foreach (var step in region.GetFrontierSteps()) {
				InGameEventManager.SendEvent(InGameEvents.OnTrackingRequested,
					new TrackingRequest(step, TrackingPanelType.Quest, step.transform, step.OnClick));
			}
		}
	}

#if CHEATS
	// Чит: восстановить зоны без оплаты и анимаций. all == false — только «следующую»: первую зону в работе,
	// а если таких нет — первую закрытую (порядок списка regions). Закрытая зона сначала грузится как
	// InProgress: шаги лежат в её progress-префабе, а их Unlock*-экшены должны попасть в профиль.
	// На время работы initialized снят — как на старте хаба: UnlockRegionAction из Apply() не запускает
	// параллельную перезагрузку зон и не анонсирует, повторное нажатие чита игнорируется.
	// Возвращает число восстановленных зон
	public async UniTask<int> CheatCompleteRegions(bool all) {
		if (!initialized)
			return 0;
		initialized = false;

		int completed = 0;
		try {
			var loc = SavesManager.Profile.playerLocation;
			while (true) {
				var region = CheatFindNextRegion();
				if (region == null)
					break;

				loc.SetUnlocked(region.Id);
				if (region.State != ProgressRegion.RegionState.InProgress)
					await region.LoadForCurrentState();
				region.CheatCompleteAllSteps();
				await region.LoadForCurrentState();
				completed++;

				// Как после честного шага: зоны с выполненным процентным гейтом открываются следом
				ApplyUnlockRules();
				if (!all)
					break;
			}
			await CheatSaveAndReload();
		} finally {
			// Исключение из Apply() кривого контента не должно оставить хаб «вечно грузящимся»
			initialized = true;
		}
		UpdateState();
		return completed;
	}

	// Чит: открыть все закрытые зоны (Destroyed → InProgress), шаги не трогаются.
	// Возвращает число открытых зон
	public async UniTask<int> CheatUnlockAllRegions() {
		if (!initialized)
			return 0;
		initialized = false;

		int unlocked = 0;
		try {
			var loc = SavesManager.Profile.playerLocation;
			foreach (var region in regions) {
				if (loc.IsUnlocked(region.Id))
					continue;
				loc.SetUnlocked(region.Id);
				unlocked++;
			}
			await CheatSaveAndReload();
		} finally {
			initialized = true;
		}
		UpdateState();
		return unlocked;
	}

	private async UniTask CheatSaveAndReload() {
		// Анлоки контента из Apply() встали в очередь анонсов — без очистки всплыли бы на ближайшем честном шаге
		Gameplay.Unlocks.ContentUnlockAnnouncer.Clear();
		SavesManager.Instance.SaveProfile();

		foreach (var region in regions) {
			if (region.ResolveState() != region.State)
				await region.LoadForCurrentState();
		}
	}

	private ProgressRegion CheatFindNextRegion() {
		ProgressRegion firstLocked = null;
		foreach (var region in regions) {
			switch (region.ResolveState()) {
				case ProgressRegion.RegionState.InProgress:
					return region;
				case ProgressRegion.RegionState.Destroyed:
					firstLocked ??= region;
					break;
			}
		}
		return firstLocked;
	}
#endif
}
