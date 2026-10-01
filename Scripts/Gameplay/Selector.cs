using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selector : MonoBehaviour
{
	public Transform[] corners;
	public float period;
	public AnimationCurve anim;

	public float minDistance;
	float maxDistance;

	float timer = 0;
    // Start is called before the first frame update
    void Start()
    {
		maxDistance = (transform.position - corners[0].transform.position).magnitude;
    }

    // Update is called once per frame
    void Update()
    {
		timer += Time.deltaTime;

		if (timer >= period) {
			timer = 0;
		}

		float distance = minDistance + (maxDistance - minDistance) * anim.Evaluate(timer / period);
		foreach(var corner in corners) {
			corner.localPosition = corner.localPosition.normalized * distance;
		}

    }
}
