using System.Collections.Generic;
using UnityEngine;

// Следит за тем, как игрок справляется с волнами, и выдаёт адаптивный pressure-множитель
// сложности: быстрые чистые зачистки поднимают его, протечки в замок — опускают.
// Держит игрока «на грани»: базовый рост 1.1^n задаёт тренд, pressure — обратную связь.
public class WavePerformanceTracker {

	private class WaveRecord {
		public float expectedKillTime;
		public float spawnCompletedAt = -1f;
		public bool leaked;
		public bool resolved;
	}

	private readonly WavesGeneratorSettings settings;
	private readonly Dictionary<int, WaveRecord> waves = new Dictionary<int, WaveRecord>();
	private float castleHPAtLastWaveStart = -1f;
	private float pressure = 1f;

	public float Pressure => pressure;

	public WavePerformanceTracker(WavesGeneratorSettings settings) {
		this.settings = settings;
	}

	// Вызывается перед генерацией каждой волны: оцениваем потерю ХП замка за прошлую волну.
	public void OnWaveStarting(float currentCastleHP) {
		if (settings == null)
			return;

		if (castleHPAtLastWaveStart >= 0f) {
			float lost = castleHPAtLastWaveStart - currentCastleHP;
			// Мелкая протечка уже оштрафована в OnWaveLeaked — здесь добираем до big-штрафа.
			if (lost >= settings.bigLeakCastleHP)
				Apply(settings.pressureBigLeak - settings.pressureSmallLeak);
		}
		castleHPAtLastWaveStart = currentCastleHP;
	}

	public void RegisterWave(int waveId, float expectedKillTime) {
		waves[waveId] = new WaveRecord { expectedKillTime = expectedKillTime };
	}

	// Корабль высадил всю волну и уплыл — с этого момента меряем время зачистки.
	public void OnWaveSpawnCompleted(int waveId) {
		if (waves.TryGetValue(waveId, out var record))
			record.spawnCompletedAt = Time.time;
	}

	public void OnWaveCleared(int waveId) {
		if (settings == null || !waves.TryGetValue(waveId, out var record) || record.resolved)
			return;

		record.resolved = true;
		if (record.leaked)
			return;

		float clearTime = record.spawnCompletedAt >= 0f
			? Time.time - record.spawnCompletedAt
			: record.expectedKillTime;
		bool fast = clearTime <= record.expectedKillTime * settings.fastClearRatio;
		Apply(fast ? settings.pressureFastFlawless : settings.pressureFlawless);
	}

	public void OnWaveLeaked(int waveId) {
		if (settings == null)
			return;

		if (!waves.TryGetValue(waveId, out var record)) {
			record = new WaveRecord();
			waves[waveId] = record;
		}

		if (record.leaked)
			return;

		record.leaked = true;
		Apply(settings.pressureSmallLeak);
	}

	public void Reset() {
		waves.Clear();
		castleHPAtLastWaveStart = -1f;
		pressure = 1f;
	}

	private void Apply(float delta) {
		pressure = Mathf.Clamp(pressure + delta, settings.minPressure, settings.maxPressure);
	}
}
