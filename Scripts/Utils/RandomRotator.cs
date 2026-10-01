using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomRotator : MonoBehaviour
{
	public float minSpeed;
	public float maxSpeed;

	float speedX, speedY, speedZ;

	private void Start() {
		speedX = Random.Range(minSpeed, maxSpeed);
		speedY = Random.Range(minSpeed, maxSpeed);
		speedZ = Random.Range(minSpeed, maxSpeed);
	}

	void Update()
    {
			transform.Rotate(Vector3.forward, speedX * Time.deltaTime);
			transform.Rotate(Vector3.up, speedY * Time.deltaTime);
			transform.Rotate(Vector3.right, speedZ * Time.deltaTime);
	}
}
