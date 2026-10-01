using System;
using System.Collections.Generic;
using ClockStone;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameSound {
	ButtonClick, CrystalDrop, TowerUpgradeReady, ChestIdle, ShipIdle, BirdTakeoff, Victory, Defeat,
	// Append only: existing values are serialized in prefabs.
	BookTabClick = 8, HubTaskComplete = 9, HubPuff = 10, TowerBuild = 11
}

// ID bindings and AudioToolkit's clip catalogue live together on Resources/GameAudioController.
[DisallowMultipleComponent, RequireComponent(typeof(AudioController))]
public sealed class GameAudioManager : MonoBehaviour {
	[Serializable]
	public class SoundBinding {
		public GameSound sound;
		public string audioId;
	}

	[Serializable]
	public class TowerSounds {
		public TowerType towerType;
		public string shotId;
		public string rotationId;
		public string impactId;
		[Tooltip("Only horizontal base rotation drives the sound; barrel elevation is silent.")]
		public bool rotationHorizontalOnly;
		[Tooltip("Minimum remaining angle in degrees to start a rotation sound. Does not cut off an active sound.")]
		[Min(0f)] public float rotationMinAngle;
		[Tooltip("Minimum estimated turn duration (remaining angle / rotation speed) to start the sound.")]
		[Min(0f)] public float rotationMinDuration;
		[Tooltip("Seconds without rotation before playing the outro. Bridges short aiming interruptions.")]
		[Min(0f)] public float rotationStopDelay;
	}

	[Serializable]
	public class UnitSound {
		public UnitType unitType;
		public string movementId;
	}

	public static GameAudioManager Instance { get; private set; }
	public const string ResourceName = "GameAudioController";

	[Header("Audio IDs — clips and per-ID limits are in AudioController below")]
	public SoundBinding[] sounds = Array.Empty<SoundBinding>();
	public TowerSounds[] towers = Array.Empty<TowerSounds>();
	[Tooltip("One shot per jump. An empty ID silences that unit. Missing entries use the defaults.")]
	public UnitSound[] units = Array.Empty<UnitSound>();
	public string groundMovementId = "Unit.Ground.Move";
	public string flyingMovementId = "Unit.Flying.Move";

	[Header("Shared unit jump budget (across all unit types)")]
	[Min(0)] public int maxUnitMovementVoices = 8;
	[Min(0.05f)] public float loopRetryInterval = 0.4f;

	[Header("Music — playlist names from AudioController")]
	public string calmMusicPlaylist = "Calm";
	public string battleMusicPlaylist = "Battle";
	[Tooltip("Music volume multiplier while timeScale is zero. Does not change the saved volume.")]
	[Range(0f, 1f)] public float pausedMusicVolume = 0.65f;
	[Min(0f)] public float musicPauseFadeTime = 0.35f;

	private const string RuntimeMusicPlaylist = "GameMusic (runtime)";
	private string currentMusicPlaylist;
	private bool musicNeedsStart;
	private float nextMusicAttempt;
	private float musicDuck = 1f;
	private float musicDuckStart = 1f;
	private float musicDuckTarget = 1f;
	private float musicDuckTime;
	private AudioController controller;
	private readonly List<PoolableReference<AudioObject>> movementVoices = new List<PoolableReference<AudioObject>>();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStatics() => Instance = null;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap() {
		if (Instance != null) return;
		var prefab = Resources.Load<GameObject>(ResourceName);
		if (prefab != null) Instantiate(prefab).name = ResourceName;
	}

	private void Awake() {
		Instance = this;
		controller = GetComponent<AudioController>();
		DontDestroyOnLoad(gameObject);
		GameAudio.OnVolumeChanged += ApplyVolumes;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void Start() {
		ApplyVolumes();
		if (currentMusicPlaylist == null) SetBattleMusic(false);
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
		// A new level begins in preparation. Keep the current calm track across scenes.
		if (mode == LoadSceneMode.Single) SetBattleMusic(false);
	}

	private void Update() {
		float target = Time.timeScale <= 0f ? pausedMusicVolume : 1f;
		if (target != musicDuckTarget) {
			musicDuckStart = musicDuck;
			musicDuckTarget = target;
			musicDuckTime = 0f;
		}
		musicDuckTime += Time.unscaledDeltaTime;
		float duck = Mathf.Lerp(musicDuckStart, target,
			musicPauseFadeTime > 0f ? musicDuckTime / musicPauseFadeTime : 1f);
		if (duck != musicDuck) {
			musicDuck = duck;
			ApplyVolumes();
		}

		// Empty clips, a muted startup or a temporarily absent listener must not stall music forever.
		if (currentMusicPlaylist != null && Time.unscaledTime >= nextMusicAttempt &&
			(musicNeedsStart || !AudioController.IsPlaylistPlaying())) TryStartMusic();
	}

	public void SetBattleMusic(bool battle) {
		string next = battle ? battleMusicPlaylist : calmMusicPlaylist;
		if (currentMusicPlaylist == next) return;
		currentMusicPlaylist = next;
		musicNeedsStart = true;
		TryStartMusic();
	}

	private void TryStartMusic() {
		nextMusicAttempt = Time.unscaledTime + 0.5f;
		var tracks = new List<string>();
		if (controller.musicPlaylists != null) {
			foreach (var playlist in controller.musicPlaylists) {
				if (playlist == null || playlist.name != currentMusicPlaylist || playlist.playlistItems == null) continue;
				foreach (string id in playlist.playlistItems) {
					if (!HasClip(id) || tracks.Contains(id)) continue;
					var item = AudioController.GetAudioItem(id);
					// Music is always 2D, survives scene loads and advances through the playlist.
					item.DestroyOnLoad = false;
					item.overrideAudioSourceSettings = true;
					item.spatialBlend = 0f;
					item.Loop = AudioItem.LoopMode.DoNotLoop;
					tracks.Add(id);
				}
				break;
			}
		}
		if (tracks.Count == 0 || GameAudio.MusicVolume * musicDuck <= 0f ||
			!controller.musicEnabled || AudioController.GetCurrentAudioListener() == null) {
			if (musicNeedsStart) AudioController.StopMusic(controller.musicCrossFadeTime_Out);
			musicNeedsStart = false;
			return;
		}

		// Filter only a runtime copy: unassigned slots stay editable in the configured lists.
		var runtime = controller.GetPlaylistByName(RuntimeMusicPlaylist);
		if (runtime == null) AudioController.AddPlaylist(RuntimeMusicPlaylist, tracks.ToArray());
		else runtime.playlistItems = tracks.ToArray();
		var audio = AudioController.PlayMusicPlaylist(RuntimeMusicPlaylist);
		if (audio != null) audio.FadeIn(controller.musicCrossFadeTime_In);
		musicNeedsStart = false;
	}

	private void OnDestroy() {
		GameAudio.OnVolumeChanged -= ApplyVolumes;
		SceneManager.sceneLoaded -= OnSceneLoaded;
		if (Instance == this) Instance = null;
	}

	private void ApplyVolumes() {
		// Category.Volume updates sounds already playing; child categories inherit SFX.
		if (controller.AudioCategories == null) return;
		foreach (var category in controller.AudioCategories) {
			if (category.Name == "Music") category.Volume = GameAudio.MusicVolume * musicDuck;
			else if (category.Name == "SFX") category.Volume = GameAudio.SoundVolume;
		}
	}

	public string GetId(GameSound sound) {
		foreach (var binding in sounds)
			if (binding != null && binding.sound == sound) return binding.audioId;
		return null;
	}

	public TowerSounds GetTower(TowerType type) {
		foreach (var binding in towers)
			if (binding != null && binding.towerType == type) return binding;
		return null;
	}

	public string GetUnitMovement(UnitType type, TypeMovement movement) {
		foreach (var binding in units)
			if (binding != null && binding.unitType == type) return binding.movementId;
		return movement == TypeMovement.flyin ? flyingMovementId : groundMovementId;
	}

	// Empty placeholders are intentional: do not ask AudioToolkit to play a missing clip.
	private static bool HasClip(string id, int depth = 0) {
		if (string.IsNullOrEmpty(id) || depth > 8) return false;
		var item = AudioController.GetAudioItem(id);
		if (item == null || item.subItems == null) return false;
		foreach (var sub in item.subItems) {
			if (sub == null) continue;
			if (sub.SubItemType == AudioSubItemType.Clip && sub.Clip != null) return true;
			if (sub.SubItemType == AudioSubItemType.Item && HasClip(sub.ItemModeAudioID, depth + 1)) return true;
		}
		return false;
	}

	public AudioObject Play(string id, Vector3? position = null, bool unitMovement = false) {
		if (GameAudio.SoundVolume <= 0f || !HasClip(id)) return null;
		// Toolkit replaces the oldest voice at its limit (with a fade). For emitters that retry,
		// that would constantly steal looping sounds from each other. Keep the configured cap hard.
		var item = AudioController.GetAudioItem(id);
		if (item.MaxInstanceCount > 0 &&
			AudioController.GetPlayingAudioObjectsCount(id, true) >= item.MaxInstanceCount) return null;
		if (unitMovement) {
			movementVoices.RemoveAll(reference => reference.Get() == null || !reference.Get().IsPlaying());
			if (movementVoices.Count >= maxUnitMovementVoices) return null;
		}
		var audio = position.HasValue ? AudioController.Play(id, position.Value) : AudioController.Play(id);
		if (audio != null && unitMovement) {
			var reference = new PoolableReference<AudioObject>();
			reference.Set(audio, true);
			movementVoices.Add(reference);
		}
		return audio;
	}
}
