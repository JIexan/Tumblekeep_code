using System;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.RestorationSystem.Actions {
	// Подменяет материал в одном слоте рендерера: «мёртвая» жёлтая земля зоны
	// становится зелёной. Подмена, как и у ToggleObjectAction, прячется за пуфом —
	// он растёт, и на пике (actionDelay) материал меняется.
	[System.Serializable]
	public class SwapMaterialAction : BaseProgressAction {
		[SerializeField] Renderer target;
		[SerializeField] Material material;
		[Tooltip("Индекс слота материала в рендерере.")]
		[SerializeField] int materialIndex;

		[Tooltip("Опционально: пуф, прячущий момент подмены. Пусто — меняем без эффекта.")]
		[SerializeField] PuffEffectConfig puff;
		[SerializeField] float effectScale = 1f;
		[Tooltip("Мировой оффсет точки спавна пуфа от центра bounds рендерера.")]
		[SerializeField] Vector3 effectOffset;
		[Tooltip("Задержка до подмены, сек (= время роста пуфа до пика).")]
		[SerializeField] float actionDelay;

		// Instantly set the final state (used when restoring saved progress).
		public override void Apply() {
			if (target == null || material == null)
				return;

			Material[] materials = target.sharedMaterials;
			if (materialIndex < 0 || materialIndex >= materials.Length)
				return;
			if (materials[materialIndex] == material)
				return;

			// Присваиваем ассет напрямую (не .materials) — инстанс материала не плодится.
			materials[materialIndex] = material;
			target.sharedMaterials = materials;
		}

		public override void Play(Action onComplete) {
			if (puff != null && Application.isPlaying && target != null)
				puff.Spawn(target.bounds.center + effectOffset, effectScale);

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
		// One-shot setup for tooling: assign renderer/material + chain delay.
		public void EditorConfigure(Renderer newTarget, Material newMaterial, int slot, float chainDelay) {
			target = newTarget;
			material = newMaterial;
			materialIndex = slot;
			startDelay = chainDelay;
		}

		// Подгоняет пуф под размеры рендерера. Для целого куска террейна пуф выходит
		// огромным — там его обычно оставляют пустым.
		[Sirenix.OdinInspector.Button("Авто-подбор из bounds")]
		private void EditorAutoFit() {
			if (target == null || puff == null) {
				Debug.LogWarning("SwapMaterialAction: для авто-подбора нужны target и PuffEffectConfig.");
				return;
			}

			Bounds bounds = target.bounds;
			float radius = bounds.extents.magnitude;
			if (puff.PeakRadius > 0f)
				effectScale = radius / puff.PeakRadius;
			effectOffset = -puff.CenterOffset * effectScale;
			actionDelay = puff.GrowTime;
		}
#endif
	}
}
