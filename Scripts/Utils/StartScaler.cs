using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartScaler : MonoBehaviour
{
	[SerializeField] Transform[] objects;
	[SerializeField] AnimationCurve scaleCurve;
	[SerializeField] float startScale = 0f;
	[SerializeField] float endScale = 1f;
	[SerializeField] float time = 1f;

	private void OnEnable() {
		DOVirtual.Float(startScale, endScale, time, OnValueChanged).SetEase(scaleCurve);
	}

	private void OnValueChanged(float value) {
		foreach(var o in objects) {
			o.localScale = new Vector3(value, value, value);
		}
	}
}
