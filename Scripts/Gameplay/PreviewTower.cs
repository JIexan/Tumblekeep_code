using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewTower : MonoBehaviour
{
	public GameObject towerBase;
	public GameObject weapon;

	public void Init(Vector3 position, Vector3 baseDiraction, Vector3 weaponDirection) {
		transform.position = position;
		towerBase.transform.forward = baseDiraction;
		weapon.transform.forward = weaponDirection;
	}
}
