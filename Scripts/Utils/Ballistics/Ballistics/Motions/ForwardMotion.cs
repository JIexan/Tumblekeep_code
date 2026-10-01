using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForwardMotion : BaseMotion
{
	private float speed;
	private float startDelay;

	public bool Enabled { get => enabled; set => enabled = value; }

	public void Init(float speed, float startDelay) {
		this.speed = speed;
		this.startDelay = startDelay;
	}

	private void Update() {
		if(startDelay >= 0) {
			startDelay -= Time.deltaTime;
			return;
		}
		transform.position += transform.forward * Time.deltaTime * speed;
		//transform.Translate(transform.forward * (speed * Time.deltaTime));
	}
}
