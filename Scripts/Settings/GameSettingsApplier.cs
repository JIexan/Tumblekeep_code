using Assets.SimpleLocalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Применяет SaveDataSettings к игре: язык, vSync/лимит FPS, постпроцессинг и сглаживание камеры, SSAO, громкость.
// SavesManager живёт в каждой сцене и на своём Awake шлёт OnSettingsLoaded — по нему всё и накатывается;
// окно настроек (SettingsWindow) зовёт те же Apply* сразу после изменения значения.
// Разрешение, режим окна и монитор здесь не трогаем — их между запусками хранит сам Unity.
public static class GameSettingsApplier {
	// Языки, для которых есть колонка в localizations.json (порядок = порядок в карусели)
	public static readonly GameLanguage[] Languages = { GameLanguage.English, GameLanguage.Russian, GameLanguage.German };

	// Камеры, у которых постпроцессинг был включён в сцене: после выключения настройки
	// их уже не отличить от тех, кому постпроцессинг не положен.
	private static readonly HashSet<UniversalAdditionalCameraData> postProcessingCameras = new HashSet<UniversalAdditionalCameraData>();

	private static SaveDataSettings Settings => SavesManager.Instance != null ? SavesManager.Instance.Settings : null;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap() {
		InGameEventManager.Unsubscribe(InGameEvents.OnSettingsLoaded, OnSettingsLoaded);
		InGameEventManager.Subscribe(InGameEvents.OnSettingsLoaded, OnSettingsLoaded);
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
#if UNITY_EDITOR
		UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
	}

	private static void OnSettingsLoaded(object _) {
		var settings = Settings;
		if (settings == null)
			return;
		ApplyLanguage(settings);
		ApplyFrameRate(settings);
		ApplyQuality(settings);
		ApplyAudio(settings);
	}

	// Камеры сцены могут появиться позже Awake SavesManager (аддитивные сцены, префабы)
	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
		var settings = Settings;
		if (settings != null)
			ApplyCameras(settings);
	}

	public static string LanguageCode(GameLanguage language) {
		switch (language) {
			case GameLanguage.Russian: return "ru";
			case GameLanguage.German: return "de";
			default: return "en";
		}
	}

	// Название языка на нём самом — не локализуется
	public static string LanguageName(GameLanguage language) {
		switch (language) {
			case GameLanguage.Russian: return "Русский";
			case GameLanguage.German: return "Deutsch";
			default: return "English";
		}
	}

	public static void ApplyLanguage(SaveDataSettings settings) {
		string code = LanguageCode(settings.language);
		if (LocalizationManager.Language != code)
			LocalizationManager.Language = code;
	}

	// targetFrameRate движок игнорирует, пока включён vSync, поэтому лимит имеет смысл только без него
	public static void ApplyFrameRate(SaveDataSettings settings) {
		ApplyFrameRate(settings.vSync, settings.fpsLimit);
	}

	public static void ApplyFrameRate(bool vSync, int fpsLimit) {
		QualitySettings.vSyncCount = vSync ? 1 : 0;
		Application.targetFrameRate = !vSync && fpsLimit > 0 ? fpsLimit : -1;
	}

	public static void ApplyQuality(SaveDataSettings settings) {
		ApplyCameras(settings);

		var occlusion = GameConfigs.Instance != null ? GameConfigs.Instance.AmbientOcclusionFeature : null;
		if (occlusion != null && occlusion.isActive != settings.ambientOcclusion)
			occlusion.SetActive(settings.ambientOcclusion);
	}

	// Постпроцессинг и сглаживание — свойства камеры. Трогаем только базовые камеры, у которых
	// постпроцессинг включён в сцене изначально (UI/оверлейным и превью-камерам он не нужен).
	private static void ApplyCameras(SaveDataSettings settings) {
		var mode = (AntialiasingMode)Mathf.Clamp(settings.cameraAntialiasing, 0, 3);
		postProcessingCameras.RemoveWhere(camera => camera == null);
		foreach (var data in Object.FindObjectsOfType<UniversalAdditionalCameraData>()) {
			if (data.renderType != CameraRenderType.Base)
				continue;
			if (!postProcessingCameras.Contains(data)) {
				if (!data.renderPostProcessing)
					continue;
				postProcessingCameras.Add(data);
			}
			data.renderPostProcessing = settings.postProcessing;
			data.antialiasing = mode;
			if (mode == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
				data.antialiasingQuality = AntialiasingQuality.High;
		}
	}

	public static void ApplyAudio(SaveDataSettings settings) {
		GameAudio.SetVolumes(settings.musicVolume, settings.soundVolume);
	}

#if UNITY_EDITOR
	// SetActive у рендер-фичи в редакторе правит сам ассет — после выхода из play mode возвращаем как было
	private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change) {
		if (change != UnityEditor.PlayModeStateChange.ExitingPlayMode)
			return;
		var occlusion = GameConfigs.Instance != null ? GameConfigs.Instance.AmbientOcclusionFeature : null;
		if (occlusion != null && !occlusion.isActive)
			occlusion.SetActive(true);
	}
#endif
}
