using System;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.RestorationSystem.Actions {
	// Move/rotate/scale a transform from an explicit start state to an explicit end state.
	[System.Serializable]
	public class MoveTransformAction : BaseProgressAction {
		[SerializeField] Transform target;
		[SerializeField] bool useLocal = true;

		[SerializeField] bool animatePosition = true;
		[SerializeField] bool animateRotation = false;
		[SerializeField] bool animateScale = false;

		[SerializeField] Vector3 fromPosition;
		[SerializeField] Vector3 fromEuler;
		[SerializeField] Vector3 fromScale = Vector3.one;

		[SerializeField] Vector3 toPosition;
		[SerializeField] Vector3 toEuler;
		[SerializeField] Vector3 toScale = Vector3.one;

		[SerializeField] float duration = 1f;
		[SerializeField] Ease ease = Ease.OutCubic;

		public override void Apply() {
			SetState(toPosition, toEuler, toScale);
		}

		public override void Play(Action onComplete) {
			if (target == null || duration <= 0f || !(animatePosition || animateRotation || animateScale)) {
				Apply();
				onComplete?.Invoke();
				return;
			}

			SetState(fromPosition, fromEuler, fromScale);

			Sequence seq = DOTween.Sequence();
			if (animatePosition)
				seq.Join((useLocal ? target.DOLocalMove(toPosition, duration) : target.DOMove(toPosition, duration)).SetEase(ease));
			if (animateRotation)
				seq.Join((useLocal ? target.DOLocalRotate(toEuler, duration) : target.DORotate(toEuler, duration)).SetEase(ease));
			if (animateScale)
				seq.Join(target.DOScale(toScale, duration).SetEase(ease));

			seq.OnComplete(() => onComplete?.Invoke());
		}

		private void SetState(Vector3 pos, Vector3 euler, Vector3 scale) {
			if (target == null) return;
			if (animatePosition) {
				if (useLocal) target.localPosition = pos;
				else target.position = pos;
			}
			if (animateRotation) {
				if (useLocal) target.localRotation = Quaternion.Euler(euler);
				else target.rotation = Quaternion.Euler(euler);
			}
			if (animateScale)
				target.localScale = scale;
		}
	}
}