public enum InGameEvents
{
    NoEvent = 0,
    OnProfileLoaded,
    OnBeforeProfileSave,
    OnAfterProfileSave,
	OnSettingsLoaded,
	OnSpawnerFinished,
	OnSpawnerTick,
    OnCoinsAmountChanged,
    OnItemAdded,
    OnItemUsed,
    OnItemCountChanged,
    OnUnitDiscovered,   // param: UnitType — тип впервые заспавнен в этом профиле (книга)
    OnSaveTowerBook,    // obsolete: не шлётся, оставлен ради стабильности int-значений enum
    OnWaveStarted,
    OnWaveEnded,
    OnUnitKilled,
	OnTasksStateChanged,
	OnLevelStarted,
	OnChestOpenned,
	OnQuestClicked,
	OnQuestCompleted,
	OnMerchantPurchase,

	// Tracking panel events
	OnTrackingRequested,
	OnTrackingTimerRequested,
	OnUntrackingRequested,

	OnLevelUpgradePurchased,
	OnIslandXPChanged,
	OnRelicAdded,

	// Tutorial hooks
	OnWindowOpened,   // param: WindowType
	OnWindowClosed,   // param: WindowType
	OnTowerBuilt,     // param: BaseTower

	OnBookStateChanged, // без параметра: изменились открытия/просмотры книги (бейджи)

	// Tutorial hooks (продолжение)
	OnBuildSpotClicked, // param: BuildingPlaceholder — клик по свободному месту постройки (открылась панель выбора башни)
	OnChestDropped,     // param: RewardChest — на поле выпал наградной сундук
	OnChestClicked,     // param: RewardChest — клик по сундуку (открылась панель покупки)

	OnContentUnlocked,  // param: Gameplay.Unlocks.ContentUnlock — мета-анлок (реликвия/свиток/фракция/ярус) реально записан в профиль

	// Tutorial hooks (продолжение)
	OnSpellDragStarted, // param: ItemType — игрок потащил свиток из HUD (SpellPanel.OnBeginDrag)
	OnHubProgressReady, // без параметра: хаб догрузил зоны и раздал панельки фронтира (PlayerProgressController.InitAsync)
	OnQuestPopupOpened, // param: ProgressStep — клик по панельке таска в хабе, открылся попап «Выполнить»

	OnRerollsChanged    // без параметра: изменился пул бесплатных рероллов катки или цена платного (RerollsManager)
}