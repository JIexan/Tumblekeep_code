using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeathBar : MonoBehaviour
{
	public RectTransform heath;
	// Во сколько раз бар босса (и чемпиона) больше обычного; остальные правила показа те же
	public float bossScale = 2f;

	public void Init(Unit u) {
		if (!u.IsBoss)
			return;
		Vector3 scale = transform.localScale;
		scale.x *= bossScale;
		scale.y *= bossScale;
		transform.localScale = scale;
	}

	public void UpdateHeath(float hp) {
		Vector2 size = (transform as RectTransform).sizeDelta;
		size.x *= hp;
		heath.sizeDelta = size;
	}
}
