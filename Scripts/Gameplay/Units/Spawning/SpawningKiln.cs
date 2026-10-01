using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawningKiln : MonoBehaviour
{
	
	private float delay;
	private bool particleSustemStarted = false;

	public UnitType unit;
	public Unit parentUnit;
	public float spawnDelay;
	public float spawnProbability;
	public float throwPower;

	public void SpawnUnit() {
		Unit u = GameController.Instance.SpawnUnitExactPosition(parentUnit.WaveId, transform.position, Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f), unit, -1, parentUnit.CurrentWaypoint,
			parentUnit.ChildHpMultiplier, false, parentUnit.RewardMultiplier);
		UnitThrower ut = u.gameObject.AddComponent<UnitThrower>();
		ut.Init(u, Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 180f), 0f) * Vector3.up * throwPower);
	}

	// Update is called once per frame
	void Update()
    {
		if(!parentUnit.AIEnabled)
			return;

		delay -= Time.deltaTime;
		if (delay <= 0) {
			if(Random.value <= spawnProbability)
				SpawnUnit();

			delay = spawnDelay;
		}
    }
}
