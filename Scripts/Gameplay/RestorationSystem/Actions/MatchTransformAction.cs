using System;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.RestorationSystem.Actions {
	// Move a transform to match the world position/rotation/scale of another transform.
	[System.Serializable]
	public class MatchTransformAction : BaseProgressAction {
		[SerializeField] Transform target;
		[SerializeField] Transform reference;

		[SerializeField] bool matchPosition = true;
		[SerializeField] bool matchRotation = true;
		[SerializeField] bool matchScale = true;

		[SerializeField] float duration = 1f;
		[SerializeField] Ease ease = Ease.OutCubic;

		public override void Apply() {
			if (target == null || reference == null) return;
			if (matchPosition) target.position = reference.position;
			if (matchRotation) target.rotation = reference.rotation;
			if (matchScale) target.localScale = reference.lossyScale;
		}

		public override void Play(Action onComplete) {
			if (target == null || reference == null || duration <= 0f || !(matchPosition || matchRotation || matchScale)) {
				Apply();
				onComplete?.Invoke();
				return;
			}

			Sequence seq = DOTween.Sequence();
			if (matchPosition)
				seq.Join(target.DOMove(reference.position, duration).SetEase(ease));
			if (matchRotation)
				seq.Join(target.DORotate(reference.eulerAngles, duration).SetEase(ease));
			if (matchScale)
				seq.Join(target.DOScale(reference.lossyScale, duration).SetEase(ease));

			seq.OnComplete(() => onComplete?.Invoke());
		}
	}
}