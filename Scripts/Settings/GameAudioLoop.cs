using ClockStone;
using UnityEngine;

// An emitter owns a pool-safe handle; stopping it cannot stop another emitter's reused voice.
public sealed class GameAudioLoop {
	private readonly PoolableReference<AudioObject> voice = new PoolableReference<AudioObject>();
	private string currentId;
	private float nextAttempt;
	private bool finishing;
	private readonly bool fadeOutOnFinish;

	public GameAudioLoop(bool fadeOutOnFinish = false) {
		this.fadeOutOnFinish = fadeOutOnFinish;
	}

	public void Update(string id, Vector3 position, bool active, bool unitMovement = false, bool allowStart = true) {
		var manager = GameAudioManager.Instance;
		if (Time.timeScale <= 0f || manager == null || GameAudio.SoundVolume <= 0f || string.IsNullOrEmpty(id)) {
			Stop();
			return;
		}
		if (currentId != id) Stop();
		if (!active) {
			Finish(position);
			return;
		}
		// A new movement starts with the intro, even if the previous outro is still playing.
		if (finishing) Stop();
		currentId = id;
		var audio = voice.Get();
		if (audio != null && audio.IsPlaying()) {
			audio.transform.position = position;
			return;
		}
		// A release delay can sustain a voice, but must not start a late sound after motion ends.
		if (!allowStart || Time.unscaledTime < nextAttempt) return;
		nextAttempt = Time.unscaledTime + Mathf.Max(0.05f, manager.loopRetryInterval);
		voice.Set(manager.Play(id, position, unitMovement), true);
	}

	private void Finish(Vector3 position) {
		var audio = voice.Get();
		if (audio == null || !audio.IsPlaying()) {
			Stop();
			return;
		}
		audio.transform.position = position;
		if (finishing) return;
		var item = audio.audioItem;
		if (item == null || item.Loop != AudioItem.LoopMode.IntroLoopOutroSequence || item.subItems.Length < 3) {
			Stop(fadeOutOnFinish);
			return;
		}

		int count = item.loopSequenceCount > 0 ? Mathf.Min(item.loopSequenceCount, item.subItems.Length) : item.subItems.Length;
		if (count < 3) { Stop(); return; }
		var outro = item.subItems[count - 1];
		if (outro.SubItemType == AudioSubItemType.Clip) {
			// FinishSequence alone waits for the loop boundary and can omit the outro during
			// a short intro. Play the configured last clip immediately when movement ends.
			// Replacing the current voice keeps the per-ID budget unchanged.
			audio.Stop(0f);
			// PlayAudioItem preserves the event ID and spatial overrides for voice limits.
			var endingItem = new AudioItem(item) { subItems = new[] { outro } };
			var ending = AudioController.Instance.PlayAudioItem(endingItem, 1f, position);
			ending?.FinishSequence(); // This final clip must not start the sequence again.
			voice.Set(ending, true);
		} else {
			// Nested audio items retain the toolkit's native sequence completion behavior.
			audio.FinishSequence();
		}
		finishing = true;
	}

	// Disabling, pausing or muting an emitter must also silence a pending outro.
	public void Stop(bool fadeOut = false) {
		var audio = voice.Get();
		if (fadeOut && audio != null && audio.IsPlaying()) {
			// Stop() uses the current subitem's FadeOut. Keep the handle until the tail ends
			// so a quick restart can replace it instead of accumulating overlapping loops.
			if (!finishing) audio.Stop();
			finishing = true;
			return;
		}
		if (audio != null) audio.Stop(0f);
		voice.Reset();
		currentId = null;
		nextAttempt = 0f;
		finishing = false;
	}
}
