using System;

// Settings facade. GameAudioManager connects these volumes and IDs to AudioToolkit.
public static class GameAudio {
	public static float MusicVolume { get; private set; } = 1f;
	public static float SoundVolume { get; private set; } = 1f;

	public static event Action OnVolumeChanged;

	public static void Play(GameSound sound) {
		var manager = GameAudioManager.Instance;
		if (manager != null) manager.Play(manager.GetId(sound));
	}

	public static void Play(GameSound sound, UnityEngine.Vector3 position) {
		var manager = GameAudioManager.Instance;
		if (manager != null) manager.Play(manager.GetId(sound), position);
	}

	public static void SetVolumes(float music, float sound) {
		MusicVolume = UnityEngine.Mathf.Clamp01(music);
		SoundVolume = UnityEngine.Mathf.Clamp01(sound);
		OnVolumeChanged?.Invoke();
	}
}
