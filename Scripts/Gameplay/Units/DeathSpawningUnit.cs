using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathSpawningUnit : Unit {

	public SpawningCage cage;
	public float spawnDelay;
	public UnitType unitToSpawn;

	public override void Start() {
		base.Start();

		cage.anim.SetBool("isMoving", true);
	}

	public override void DeathRoutine() {
		base.DeathRoutine();

		cage.Init(waveId, spawnDelay, unitToSpawn, currentWaypoint, ChildHpMultiplier, RewardMultiplier);
		cage.transform.SetParent(null);
		Vector3 finalPos = cage.transform.position;
		finalPos.y = 0.1f;
		Vector3 targetRotation = new Vector3(0f, cage.gameObject.transform.rotation.eulerAngles.y, 0f);
		cage.transform.DORotate(targetRotation, 0.4f).SetEase(Ease.InBack);
		cage.transform.DOMove(finalPos, 0.4f).SetEase(Ease.InBack).OnComplete(() => {
			cage.anim.SetTrigger("jump");
		});

		/*if(settings != null) {
			foreach(var unit in settings.units) {
				for(int i = 0; i < unit.unitCount; i++) {
					GameController.Instance.SpawnUnit(transform.position, transform.rotation, unit.type, -1, currentWaypoint);
				}
			}
		}*/
	}
}
