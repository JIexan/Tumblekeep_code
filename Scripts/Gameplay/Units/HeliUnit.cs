using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeliUnit : Unit {

	public Joint joint;
	public Rigidbody payload;
	public Vector3 payloadPosition;
	public float forwardAngle = 30f;

	Rigidbody p;

	public override void Start() {
		base.Start();

		/* p = Instantiate(payload,transform.TransformPoint(payloadPosition), Quaternion.identity);
		joint.connectedBody = p;

		LeanTween.rotateLocal(scaleObject, new Vector3(forwardAngle, 0f, 0f), 1f);*/

		//cage.anim.SetBool("isMoving", true);


	}

	public override void Update() {
		base.Update();
		//p.AddForce(-Velocity * 0.25f);
	}
}
