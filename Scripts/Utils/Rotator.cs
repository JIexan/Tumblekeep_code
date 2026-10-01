using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
	public float rotationSpeed;
	public bool X = false;
	public bool Y = false;
	public bool Z = true;


	void Update()
    {
		if (Z)
			transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

		if (Y)
			transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
	}
}
