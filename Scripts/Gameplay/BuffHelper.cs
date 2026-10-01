using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BuffType {
	None,
	ElectricTowerSlowDown,
	ElectricTowerDamage,
	Heal,
	Poison
}

public class BuffHelper : MonoBehaviour
{
	public BaseTowerWeapon weapon;
	public BuffType type;
	public bool takeOffOnExit = true;

	public void OnTriggerEnter(Collider other) {
		//Debug.Log("buff start");
		weapon.ApplyBuff(other.GetComponent<Unit>(), type);
	}

	public void OnTriggerExit(Collider other) {
		if(takeOffOnExit)
			weapon.TakeOffBuff(other.GetComponent<Unit>(), type);
	}
}
