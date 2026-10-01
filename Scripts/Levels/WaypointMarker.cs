using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointMarker : MonoBehaviour {

    void OnDrawGizmos()
    {
		Gizmos.DrawSphere(transform.position, 1f);
    }
}
