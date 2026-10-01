using UnityEngine;

namespace Gameplay.RestorationSystem.Actions {
	// Shared, reusable description of a "puff" particle effect used to mask an
	// object appearing/disappearing. Intrinsic particle params live here (apart
	// from per-action tuning) so the editor can auto-fit scale/offset/delay.
	[CreateAssetMenu(menuName = "Restoration/Puff Effect Config", fileName = "PuffEffectConfig")]
	public class PuffEffectConfig : ScriptableObject {
		[Tooltip("Префаб пуфа (должен содержать ParticleSystemEffect).")]
		[SerializeField] GameObject prefab;

		[Tooltip("Мировой радиус пуфа на пике при localScale = 1 (для авто-подбора скейла).")]
		[SerializeField] float peakRadius = 1f;

		[Tooltip("Время от спавна до пика, сек (дефолт для задержки тоггла).")]
		[SerializeField] float growTime = 0.3f;

		[Tooltip("Полная длительность пуфа, сек (для очистки в демо-режиме).")]
		[SerializeField] float lifetime = 1f;

		[Tooltip("Локальное смещение визуального центра пуфа от точки спавна при scale = 1.")]
		[SerializeField] Vector3 centerOffset;

		[Tooltip("Сколько инстансов заранее налить в пул.")]
		[SerializeField] int preloadCount = 2;

		public GameObject Prefab => prefab;
		public float PeakRadius => peakRadius;
		public float GrowTime => growTime;
		public float Lifetime => lifetime;
		public Vector3 CenterOffset => centerOffset;

		// Pre-fill the shared pool so the first spawn doesn't hitch.
		public void Preload() {
			if (prefab != null && preloadCount > 0)
				SimplePool.Preload(prefab, preloadCount);
		}

		// Runtime spawn through the existing pool. The ParticleSystemEffect on the
		// prefab plays one-shot and returns itself to the pool when it finishes.
		public ParticleSystemEffect Spawn(Vector3 worldPos, float scale) {
			if (prefab == null)
				return null;

			GameObject go = SimplePool.Spawn(prefab, worldPos, Quaternion.identity);
			go.transform.localScale = Vector3.one * scale;

			ParticleSystemEffect fx = go.GetComponent<ParticleSystemEffect>();
			if (fx != null)
				fx.Init(false);

			// Some puff prefabs play particles on enable without ParticleSystemEffect.
			// Every spawn requests its sound; all restoration actions share the ID's voice limit.
			if (Application.isPlaying)
				GameAudio.Play(GameSound.HubPuff, worldPos);

			return fx;
		}
	}
}
