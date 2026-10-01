using Assets.SimpleLocalization;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Окно настроек на вкладках: Экран / Качество / Звук / Игра.
// Экран — отложенное применение: «Применить» включает новый режим и показывает попап с таймером,
// без подтверждения всё возвращается как было. Остальные вкладки применяются и сохраняются сразу.
// Префаб собирает Tools/UI/Build Settings Window, значения на старте игры накатывает GameSettingsApplier.
public class SettingsWindow : AnimationWindow {
	private struct DisplayState : IEquatable<DisplayState> {
		public int display;
		public int width, height;
		public FullScreenMode mode;
		public bool vSync;
		public int fpsLimit;

		public bool Equals(DisplayState other) {
			return display == other.display && width == other.width && height == other.height
				&& mode == other.mode && vSync == other.vSync && fpsLimit == other.fpsLimit;
		}
	}

	// Порядок = порядок в карусели режима окна
	private static readonly FullScreenMode[] windowModes = { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed };
	private static readonly string[] windowModeKeys = { "Settings_WindowMode_Fullscreen", "Settings_WindowMode_Borderless", "Settings_WindowMode_Windowed" };
	private static readonly int[] fpsLimits = { 30, 60, 120, 0 }; // 0 — без лимита
	private static readonly string[] antialiasingNames = { null, "FXAA", "SMAA", "TAA" }; // индекс = AntialiasingMode, null — «Выкл»

	[SerializeField] TabsPanel tabs;
	[SerializeField] ConfirmPopup confirmPopup;
	[SerializeField] TMP_Text versionText;

	[Header("Экран")]
	[SerializeField] GameObject monitorRow; // скрыт, если монитор один
	[SerializeField] CarouselControl monitor;
	[SerializeField] CarouselControl resolution;
	[SerializeField] CarouselControl windowMode;
	[SerializeField] Toggle vSync;
	[SerializeField] CarouselControl fpsLimit;
	[SerializeField] Button applyButton;
	[SerializeField] Button cancelButton;
	[SerializeField] float confirmTimeout = 15f;

	[Header("Качество")]
	[SerializeField] Toggle postProcessing;
	[SerializeField] CarouselControl antialiasing;
	[SerializeField] Toggle ambientOcclusion;

	[Header("Звук")]
	[SerializeField] Slider musicVolume;
	[SerializeField] Slider soundVolume;

	[Header("Игра")]
	[SerializeField] CarouselControl language;
	[SerializeField] Button resetProgressButton;

	private readonly List<DisplayInfo> displays = new List<DisplayInfo>();
	private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
	private DisplayState current; // то, что сейчас на экране
	private DisplayState pending; // то, что накликано в контролах
	private bool awaitingConfirm;
	private bool unsavedSettings;

	private SaveDataSettings Settings => SavesManager.Instance.Settings;

	private void Awake() {
		tabs.OnTabSelected += _ => DiscardPending();

		monitor.OnValueChanged += OnMonitorChanged;
		resolution.OnValueChanged += index => { pending.width = resolutions[index].x; pending.height = resolutions[index].y; RefreshDisplayControls(); };
		windowMode.OnValueChanged += index => { pending.mode = windowModes[index]; RefreshDisplayControls(); };
		vSync.onValueChanged.AddListener(value => { pending.vSync = value; RefreshDisplayControls(); });
		fpsLimit.OnValueChanged += index => { pending.fpsLimit = fpsLimits[index]; RefreshDisplayControls(); };
		applyButton.onClick.AddListener(ApplyPending);
		cancelButton.onClick.AddListener(DiscardPending);

		postProcessing.onValueChanged.AddListener(value => { Settings.postProcessing = value; ApplyQuality(); });
		antialiasing.OnValueChanged += index => { Settings.cameraAntialiasing = index; ApplyQuality(); };
		ambientOcclusion.onValueChanged.AddListener(value => { Settings.ambientOcclusion = value; ApplyQuality(); });

		musicVolume.onValueChanged.AddListener(value => { Settings.musicVolume = value; ApplyAudio(); });
		soundVolume.onValueChanged.AddListener(value => { Settings.soundVolume = value; ApplyAudio(); });

		language.OnValueChanged += OnLanguageChanged;
		resetProgressButton.onClick.AddListener(AskResetProgress);
	}

	public override void Show(object data, Action onBeforeShow = null, Action onAfterHide = null) {
		base.Show(data, onBeforeShow, onAfterHide);

		confirmPopup.gameObject.SetActive(false);
		awaitingConfirm = false;
		current = pending = CaptureDisplayState();
		RefreshAll();
		tabs.Select(0);
	}

	private void OnEnable() {
		LocalizationManager.LocalizationChanged += RefreshAll;
	}

	private void OnDisable() {
		LocalizationManager.LocalizationChanged -= RefreshAll;

		// Окно закрыли, не подтвердив новый режим экрана (HideAllWindows, смена сцены) — откатываем
		if (awaitingConfirm) {
			awaitingConfirm = false;
			ApplyDisplay(current).Forget();
		}
		if (unsavedSettings) {
			unsavedSettings = false;
			SavesManager.Instance.SaveSettings();
		}
	}

	private void RefreshAll() {
		var settings = Settings;

		RefreshDisplayControls();

		postProcessing.SetIsOnWithoutNotify(settings.postProcessing);
		var antialiasingOptions = new List<string>(antialiasingNames);
		antialiasingOptions[0] = LocalizationManager.Localize("Settings_Off");
		antialiasing.SetOptions(antialiasingOptions, settings.cameraAntialiasing);
		ambientOcclusion.SetIsOnWithoutNotify(settings.ambientOcclusion);

		musicVolume.SetValueWithoutNotify(settings.musicVolume);
		soundVolume.SetValueWithoutNotify(settings.soundVolume);

		var languageNames = new List<string>();
		foreach (var item in GameSettingsApplier.Languages)
			languageNames.Add(GameSettingsApplier.LanguageName(item));
		language.SetOptions(languageNames, Mathf.Max(0, Array.IndexOf(GameSettingsApplier.Languages, settings.language)));

		if (versionText != null)
			versionText.text = "v" + Application.version;
	}

	// ---------- Экран ----------

	private DisplayState CaptureDisplayState() {
		Screen.GetDisplayLayout(displays);
		var main = Screen.mainWindowDisplayInfo;
		var mode = Screen.fullScreenMode == FullScreenMode.MaximizedWindow ? FullScreenMode.Windowed : Screen.fullScreenMode;
		return new DisplayState {
			display = Mathf.Max(0, displays.FindIndex(info => SameDisplay(info, main))),
			width = Screen.width,
			height = Screen.height,
			mode = mode,
			vSync = Settings.vSync,
			fpsLimit = Settings.fpsLimit,
		};
	}

	// Имена у одинаковых мониторов совпадают — различаем ещё и по положению на рабочем столе
	private static bool SameDisplay(DisplayInfo a, DisplayInfo b) {
		return a.name == b.name && a.workArea.Equals(b.workArea);
	}

	private void OnMonitorChanged(int index) {
		pending.display = index;
		// Список разрешений чужого монитора до переезда окна не узнать — встаём на его родное
		if (index < displays.Count) {
			pending.width = displays[index].width;
			pending.height = displays[index].height;
		}
		RefreshDisplayControls();
	}

	private void RefreshDisplayControls() {
		monitorRow.SetActive(displays.Count > 1);
		var monitorNames = new List<string>();
		for (int i = 0; i < displays.Count; i++)
			monitorNames.Add((i + 1) + ". " + displays[i].name);
		monitor.SetOptions(monitorNames, pending.display);

		RebuildResolutions();
		var resolutionNames = new List<string>();
		foreach (var item in resolutions)
			resolutionNames.Add(item.x + " x " + item.y);
		resolution.SetOptions(resolutionNames, resolutions.IndexOf(new Vector2Int(pending.width, pending.height)));

		var modeNames = new List<string>();
		foreach (var key in windowModeKeys)
			modeNames.Add(LocalizationManager.Localize(key));
		windowMode.SetOptions(modeNames, Mathf.Max(0, Array.IndexOf(windowModes, pending.mode)));

		vSync.SetIsOnWithoutNotify(pending.vSync);

		var fpsNames = new List<string>();
		foreach (int limit in fpsLimits)
			fpsNames.Add(limit > 0 ? limit.ToString() : LocalizationManager.Localize("Settings_FpsUnlimited"));
		fpsLimit.SetOptions(fpsNames, Mathf.Max(0, Array.IndexOf(fpsLimits, pending.fpsLimit)));
		fpsLimit.Interactable = !pending.vSync; // при vSync движок игнорирует targetFrameRate

		bool changed = !pending.Equals(current);
		SetButtonEnabled(applyButton, changed);
		SetButtonEnabled(cancelButton, changed);
	}

	// У аниматора Button.prefab состояние Disabled пустое — приглушаем сами через CanvasGroup кнопки
	private static void SetButtonEnabled(Button button, bool enabled) {
		button.interactable = enabled;
		var group = button.GetComponent<CanvasGroup>();
		if (group != null)
			group.alpha = enabled ? 1f : 0.4f;
	}

	// Screen.resolutions — режимы монитора, на котором окно сейчас. Для другого монитора берём из них те,
	// что влезают в его родное разрешение. Текущее разрешение (произвольный размер окна) добавляем всегда.
	private void RebuildResolutions() {
		resolutions.Clear();
		var limit = pending.display < displays.Count
			? new Vector2Int(displays[pending.display].width, displays[pending.display].height)
			: new Vector2Int(int.MaxValue, int.MaxValue);
		foreach (var item in Screen.resolutions) {
			var size = new Vector2Int(item.width, item.height);
			if (size.x <= limit.x && size.y <= limit.y && !resolutions.Contains(size))
				resolutions.Add(size);
		}
		if (pending.display < displays.Count && !resolutions.Contains(limit))
			resolutions.Add(limit);
		var selected = new Vector2Int(pending.width, pending.height);
		if (!resolutions.Contains(selected))
			resolutions.Add(selected);
		resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
	}

	private void ApplyPending() {
		if (pending.Equals(current))
			return;
		awaitingConfirm = true;
		ApplyDisplay(pending).Forget();
		confirmPopup.Show("Settings_ConfirmDisplay_Title", "Settings_ConfirmDisplay_Text", "Settings_Save", "Settings_Revert",
			confirmTimeout, KeepPending, RevertPending);
	}

	private void KeepPending() {
		awaitingConfirm = false;
		current = pending;
		Settings.vSync = current.vSync;
		Settings.fpsLimit = current.fpsLimit;
		SavesManager.Instance.SaveSettings();
		RefreshDisplayControls();
	}

	private void RevertPending() {
		awaitingConfirm = false;
		ApplyDisplay(current).Forget();
		DiscardPending();
	}

	private void DiscardPending() {
		pending = current;
		RefreshDisplayControls();
	}

	// UniTask, а не корутина: откат должен доработать, даже если окно уже выключено
	private async UniTaskVoid ApplyDisplay(DisplayState state) {
		GameSettingsApplier.ApplyFrameRate(state.vSync, state.fpsLimit);
#if !UNITY_EDITOR
		var main = Screen.mainWindowDisplayInfo;
		if (state.display < displays.Count && !SameDisplay(displays[state.display], main)) {
			var target = displays[state.display];
			var position = new Vector2Int(Mathf.Max(0, (target.width - state.width) / 2), Mathf.Max(0, (target.height - state.height) / 2));
			await Screen.MoveMainWindowTo(target, position);
		}
#endif
		Screen.SetResolution(state.width, state.height, state.mode);
		await UniTask.CompletedTask;
	}

	// ---------- Качество / Звук ----------

	private void ApplyQuality() {
		GameSettingsApplier.ApplyQuality(Settings);
		SavesManager.Instance.SaveSettings();
	}

	// Слайдер шлёт значение каждый кадр перетаскивания — файл пишем один раз, при закрытии окна
	private void ApplyAudio() {
		GameSettingsApplier.ApplyAudio(Settings);
		unsavedSettings = true;
	}

	// ---------- Игра ----------

	private void OnLanguageChanged(int index) {
		Settings.language = GameSettingsApplier.Languages[index];
		SavesManager.Instance.SaveSettings();
		GameSettingsApplier.ApplyLanguage(Settings); // LocalizationChanged → RefreshAll
	}

	private void AskResetProgress() {
		confirmPopup.Show("Settings_ConfirmReset_Title", "Settings_ConfirmReset_Text", "Settings_Reset", "Settings_Cancel",
			0f, ResetProgress, null);
	}

	// Окно открывается и из паузы в бою — после сброса всегда уходим в главное меню
	private void ResetProgress() {
		PlayerPrefs.DeleteAll();
		SavesManager.Instance.DeleteCurrent();
		Time.timeScale = 1f;
		UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
	}
}
