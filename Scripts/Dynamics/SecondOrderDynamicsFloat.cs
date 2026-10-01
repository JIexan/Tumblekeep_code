using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecondOrderDynamicsFloat
{
	private float xp; //previous input
	private float y, yd; //state variables
	private float k1, k2, k3;

	public SecondOrderDynamicsFloat(float f, float z, float r, float x0) {
		k1 = z / (Mathf.PI * f);
		k2 = 1 / ((2 * Mathf.PI * f) * (2 * Mathf.PI * f));
		k3 = r * z / (2 * Mathf.PI * f);
		//initialize variables
		xp = x0;
		y = x0;
		yd = 0;
	}

	public float Update(float T, float x, float? xd = null) {
		if(xd == null) { //estimate velocity
			xd = (x - xp) / T;
			xp = x;
		}
		y = y + T * yd; //itegrate position by velocity
		yd = yd + T * (x + k3 * (float)xd - y - k1 * yd) / k2;
		return y;
	}
}
