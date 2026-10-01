using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyTimer : MonoBehaviour
{
	float time = 0;
	bool inited = false;
	public void Init(float delay) {
		time = delay;
		inited = true;
	}

	private void Update() {
		if(inited) {
			time -= Time.deltaTime;
			if (time <= 0)
				Destroy(gameObject);
		}
	}


}
