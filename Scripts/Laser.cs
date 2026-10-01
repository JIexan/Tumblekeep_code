using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
	public GameObject beamStart;
	public GameObject beamEnd;
	public GameObject beam;
	public LineRenderer line;

	private Unit target;

	public void Activate(Unit target) {
		beamStart.SetActive(true);
		beamEnd.SetActive(true);
		beam.SetActive(true);
		this.target = target;
		ShootBeamInDir(target.transform.position + target.aimOffset);
	}

	public void Deactivate() {
		beamStart.SetActive(false);
		beamEnd.SetActive(false);
		beam.SetActive(false);
		target = null;
	}

    // Update is called once per frame
    void Update()
    {
		if(target != null)
			ShootBeamInDir(target.transform.position + target.aimOffset);
	}

	void ShootBeamInDir(Vector3 end) {
		line.positionCount = 2;
		line.SetPosition(0, transform.position);
		beamStart.transform.position = transform.position;

		beamEnd.transform.position = end;
		line.SetPosition(1, end);

		beamStart.transform.LookAt(beamEnd.transform.position);
		beamEnd.transform.LookAt(beamStart.transform.position);

		float distance = Vector3.Distance(transform.position, end);
	}
}
