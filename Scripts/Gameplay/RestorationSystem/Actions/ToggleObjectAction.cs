using System;
using DG.Tweening;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Gameplay.RestorationSystem.Actions {
	// Enable/disable a GameObject, masking the change with a pooled "puff" effect.
	// The puff is spawned and given time to grow; the actual SetActive happens at
	// its peak (after actionDelay) so the object pops in/out hidden by the puff.
	[System.Serializable]
	public class ToggleObjectAction : BaseProgressAction {
		[SerializeField] GameObject target;
		[SerializeField] bool enable = true;

		[SerializeField] PuffEffectConfig puff;
		[SerializeField] float effectScale = 1f;
		[Tooltip("Мировой оффсет точки спавна пуфа от target.position.")]
		[SerializeField] Vector3 effectOffset;
		[Tooltip("Задержка до SetActive, сек (= время роста пуфа до пика).")]
		[SerializeField] float actionDelay;

		// Instantly set the final state (used when restoring saved progress).
		public override void Apply() {
			if (target != null)
				target.SetActive(enable);
		}

		public override void Play(Action onComplete) {
			if (puff != null && Application.isPlaying && target != null)
				puff.Spawn(target.transform.position + effectOffset, effectScale);

			if (actionDelay > 0f) {
				DOVirtual.DelayedCall(actionDelay, () => {
					Apply();
					onComplete?.Invoke();
				}, false);
			} else {
				Apply();
				onComplete?.Invoke();
			}
		}

#if UNITY_EDITOR
		// One-shot setup used by the Progress Step Builder tool: assign target/state/
		// puff + chain delay, then reuse the bounds auto-fit for scale/offset/actionDelay.
		public void EditorConfigure(GameObject newTarget, bool newEnable, PuffEffectConfig newPuff, float chainDelay) {
			target = newTarget;
			enable = newEnable;
			puff = newPuff;
			startDelay = chainDelay;
			EditorAutoFit();
		}

		// Rough estimate of scale/offset/delay from the target's world bounds.
		// Values are meant to be hand-tweaked afterwards.
		[Sirenix.OdinInspector.Button("Авто-подбор из bounds")]
		private void EditorAutoFit() {
			if (target == null) {
				Debug.LogWarning("ToggleObjectAction: не задан target для авто-подбора.");
				return;
			}
			if (puff == null) {
				Debug.LogWarning("ToggleObjectAction: не задан PuffEffectConfig для авто-подбора.");
				return;
			}

			if (!TryGetWorldBounds(target, out Bounds bounds)) {
				Debug.LogWarning("ToggleObjectAction: у target нет Renderer/Collider — bounds не вычислить.");
				return;
			}

			float radius = bounds.extents.magnitude;
			if (puff.PeakRadius > 0f)
				effectScale = radius / puff.PeakRadius;
			effectOffset = bounds.center - target.transform.position - puff.CenterOffset * effectScale;
			actionDelay = puff.GrowTime;
		}

		private static bool TryGetWorldBounds(GameObject go, out Bounds bounds) {
			bounds = default;
			bool has = false;

			foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) {
				if (!has) { bounds = r.bounds; has = true; }
				else bounds.Encapsulate(r.bounds);
			}
			if (has)
				return true;

			foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) {
				if (!has) { bounds = c.bounds; has = true; }
				else bounds.Encapsulate(c.bounds);
			}
			return has;
		}

		// Edit-mode demo: spawn the puff at the computed position/scale, then after
		// actionDelay toggle the target, then clean up — restoring everything so the
		// prefab keeps its original state.
		[Sirenix.OdinInspector.Button("Демо")]
		private void EditorDemo() {
			if (Application.isPlaying) {
				Debug.LogWarning("ToggleObjectAction: демо предназначено для edit-mode. В Play Mode используйте обычный прогон шага.");
				return;
			}
			if (target == null || puff == null || puff.Prefab == null) {
				Debug.LogWarning("ToggleObjectAction: для демо нужны target и PuffEffectConfig с prefab.");
				return;
			}

			EditorDemoRunner.Run(target, enable, puff, target.transform.position + effectOffset, effectScale, actionDelay);
		}

		// Drives a single edit-mode demo via EditorApplication.update. Static so the
		// timer survives inspector repaints/reconstruction of the drawer.
		private static class EditorDemoRunner {
			private static GameObject puffInstance;
			private static GameObject demoTarget;
			private static ParticleSystem[] rootSystems;
			private static bool enableValue;
			private static bool originalActive;
			private static bool toggled;
			private static double startTime;
			private static double lastSimTime;
			private static float delay;
			private static float lifetime;

			public static void Run(GameObject target, bool enable, PuffEffectConfig config,
				Vector3 worldPos, float scale, float actionDelay) {
				Cleanup();

				demoTarget = target;
				enableValue = enable;
				originalActive = target.activeSelf;
				toggled = false;
				delay = Mathf.Max(0f, actionDelay);
				lifetime = Mathf.Max(config.Lifetime, delay + 0.1f);
				startTime = EditorApplication.timeSinceStartup;
				lastSimTime = startTime;

				puffInstance = UnityEngine.Object.Instantiate(config.Prefab);
				puffInstance.hideFlags = HideFlags.DontSave;
				// Put it into whatever stage is currently shown (main scene OR an open
				// Prefab Stage); otherwise the instance lands in a non-visible scene.
				StageUtility.PlaceGameObjectInCurrentStage(puffInstance);
				puffInstance.transform.position = worldPos;
				puffInstance.transform.localScale = Vector3.one * scale;
				puffInstance.SetActive(true);
				rootSystems = CollectRootSystems(puffInstance);

				// Particle systems don't tick on their own in edit mode — Simulate() drives them.
				ResetSystems();

				EditorApplication.update += Tick;
			}

			// Only top-level systems (no ParticleSystem ancestor) — Simulate(withChildren:true)
			// handles their sub-emitters, so simulating children too would double-advance them.
			private static ParticleSystem[] CollectRootSystems(GameObject go) {
				var all = go.GetComponentsInChildren<ParticleSystem>(true);
				var roots = new System.Collections.Generic.List<ParticleSystem>(all.Length);
				foreach (ParticleSystem ps in all) {
					Transform parent = ps.transform.parent;
					bool hasParentSystem = parent != null && parent.GetComponentInParent<ParticleSystem>() != null;
					if (!hasParentSystem)
						roots.Add(ps);
				}
				return roots.ToArray();
			}

			// Restart each system at t=0 (paused). Subsequent ticks advance incrementally.
			private static void ResetSystems() {
				if (rootSystems == null)
					return;
				foreach (ParticleSystem ps in rootSystems) {
					if (ps != null)
						ps.Simulate(0f, true, true, false);
				}
			}

			// Advance each system by deltaTime (restart:false) — this matches how Unity
			// plays particles, instead of re-simulating 0..elapsed every frame (which jitters).
			private static void AdvanceSystems(float deltaTime) {
				if (rootSystems == null)
					return;
				foreach (ParticleSystem ps in rootSystems) {
					if (ps != null)
						ps.Simulate(deltaTime, true, false, false);
				}
			}

			private static void Tick() {
				// Bail out if anything we depend on vanished (e.g. domain reload).
				if (puffInstance == null || demoTarget == null) {
					Cleanup();
					return;
				}

				double now = EditorApplication.timeSinceStartup;
				float elapsed = (float)(now - startTime);

				// Clamp the step so an editor stall (lost focus, recompile) doesn't fast-forward.
				float delta = Mathf.Clamp((float)(now - lastSimTime), 0f, 0.1f);
				lastSimTime = now;
				AdvanceSystems(delta);

				if (!toggled && elapsed >= delay) {
					demoTarget.SetActive(enableValue);
					toggled = true;
				}

				SceneView.RepaintAll();

				if (elapsed >= lifetime)
					Finish();
			}

			private static void Finish() {
				if (demoTarget != null && demoTarget.activeSelf != originalActive)
					demoTarget.SetActive(originalActive);
				Cleanup();
			}

			private static void Cleanup() {
				EditorApplication.update -= Tick;
				if (puffInstance != null)
					UnityEngine.Object.DestroyImmediate(puffInstance);
				puffInstance = null;
				demoTarget = null;
				rootSystems = null;
				toggled = false;
			}
		}
#endif
	}
}
