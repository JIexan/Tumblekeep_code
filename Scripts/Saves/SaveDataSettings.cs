using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameLanguage {
	English = 0,
	Russian,
	German,
	French,
	Chinese,
	Spanish,
	Japanese
}

[System.Serializable]
public class SaveDataSettings : SaveDataBase {

	public int version = 1;

	public GameLanguage language = GameLanguage.English;

	[Range(0.6f, 1.4f)] public float difficulty = 1.0f;

	[Range(0.0f, 1.0f)] public float mainCameraSensitivity = 0.6f;
	[Range(0.0f, 100.0f)] public float cinematicCameraDuration = 50.0f;
	[Range(0.0f, 1.0f)] public float mapTimeSpeed = 0.5f;

#if UNITY_ANDROID || UNITY_IPHONE
	[Range(0.0f, 1.0f)] public float musicVolume = 1.0f;
	[Range(0.0f, 1.0f)] public float soundVolume = 1.0f;
#else
	[Range(0.0f, 1.0f)] public float musicVolume = 0.5f;
	[Range(0.0f, 1.0f)] public float soundVolume = 0.7f;
#endif

	public bool lockCursorInWindow = false;
	public bool autoshoot = false;
	public bool cameraFollow = true;
	public bool clickToSwim = false;

	public bool invertedX = false;
	public bool invertedY = false;
	public bool invertedZoom = false;

#if (UNITY_ANDROID || UNITY_IPHONE)
	public bool touchControls = true;
#else
	public bool touchControls = false;
#endif

	public bool wheelEnabled = false;
	public bool lightnings = true;
	public bool shadowsOnLand = true;

	//3D stuff
	public bool TV3DRender = false;
	[Range(0.005f, 0.03f)] public float eyeDistance = 0.01f;
	[Range(0.0f, 0.9f)] public float addDepth = 0.35f;

	[Range(0,3)] public int waterQuality = 1;
	[Range(-1300, -150)] public int UISize = -150;

	public int antiAliasing = 0;

	// Окно настроек (SettingsWindow). Разрешение/режим окна/монитор Unity хранит сам (PlayerPrefs),
	// здесь только то, что нужно применять на старте — см. GameSettingsApplier.
	public bool vSync = true;
	public int fpsLimit = 0; // работает только при выключенном vSync; 0 — без лимита
	public bool postProcessing = true;
	public int cameraAntialiasing = 3; // AntialiasingMode URP: 0 None, 1 FXAA, 2 SMAA, 3 TAA
	public bool ambientOcclusion = true;

	//network stuff
	public bool networkAutofind = false;
	public bool networkOpenGame = false;
	public bool networkAllAlly = true;
	public int networkRegion = 0;

	public TimeSpan timeInGame = TimeSpan.Zero;

	public static GameLanguage SelectLanguage() {

		switch(Application.systemLanguage) {
			case SystemLanguage.Japanese:
				return GameLanguage.Japanese;
			case SystemLanguage.Chinese:
			case SystemLanguage.ChineseSimplified:
			case SystemLanguage.ChineseTraditional:
				return GameLanguage.Chinese;
			case SystemLanguage.French:
				return GameLanguage.French;
			case SystemLanguage.Spanish:
				return GameLanguage.Spanish;
			case SystemLanguage.German:
				return GameLanguage.German;
			case SystemLanguage.Ukrainian:
			case SystemLanguage.Russian:
				return GameLanguage.Russian;
			default:
				return GameLanguage.English;
		}
	}
}
