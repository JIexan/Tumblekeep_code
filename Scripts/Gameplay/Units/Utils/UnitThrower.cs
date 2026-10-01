using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitThrower : MonoBehaviour
{

	private Unit u;
	private Vector3 velocity;
	private Vector3 gravity;

	public void Init(Unit u, Vector3 velocity) {
		this.u = u;
		u.AIEnabled = false;
		this.velocity = velocity;
		this.gravity = Physics.gravity;
	}

	void Update() {
		transform.position += velocity * Time.deltaTime;
		velocity += gravity * Time.deltaTime;

		if(transform.position.y <= 0.1f) {
			u.AIEnabled = true;
			Destroy(this);
		}
	}
}
