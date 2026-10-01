using DG.Tweening;
using UnityEngine;

public class SpawningPortal : MonoBehaviour
{
	public ParticleSystem ps;

	public float throwPower = 5;

	private static readonly int bornStateHash = Animator.StringToHash("Born");

	private UnitType unitType;
	private Waypoint nextWaypoint;
	private int waveId;

	private float spawnCounter;
	private float nextSpawnDelay;
	private float appearDelay;

	private bool isSpawning = true;
	private float hpMultiplier = 1f;
	private float rewardMultiplier = -1f;

	Quaternion targetRotation;
	Vector3 offset;

	public void Init(int waveId, UnitType unitType, Waypoint nextWaypoint, int unitsCount, float appearDelay, float hpMultiplier = 1f, float rewardMultiplier = -1f) {
		this.waveId = waveId;
		this.hpMultiplier = hpMultiplier;
		this.rewardMultiplier = rewardMultiplier;
		spawnCounter = unitsCount;
		nextSpawnDelay = 0.6f;


		this.appearDelay = appearDelay;
		this.nextWaypoint = nextWaypoint;
		this.unitType = unitType;

		offset = nextWaypoint.position - transform.position;
		targetRotation = Quaternion.LookRotation(offset);
		offset.Normalize();
		offset *= 0.5f;

		isSpawning = true;
	}

	public void Update() {
		if(!isSpawning) {
			if (ps.particleCount == 0)
				Destroy(gameObject);
			return;
		}

		nextSpawnDelay -= Time.deltaTime;
		if (nextSpawnDelay <= 0) {
			if (spawnCounter >= 0) {
				//spawnUnit
				Unit u = GameController.Instance.SpawnUnitExactPosition(waveId, transform.position + offset, targetRotation, unitType, -1, nextWaypoint, hpMultiplier, false, rewardMultiplier);
				u.AIEnabled = false;
				// ИИ включает ивент OnBorn из клипа рождения. У контроллеров без состояния Born
				// (пчела, драконы) триггер уходит в никуда и юнит замирает — таких выбрасываем
				if(u.anim != null && u.anim.HasState(0, bornStateHash))
					u.anim.SetTrigger("Born");
				else {
					//выбросить юнита
					UnitThrower ut = u.gameObject.AddComponent<UnitThrower>();
					ut.Init(u, Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 180f), 0f) * Vector3.up * throwPower);
				}
				spawnCounter--;
			} else {
				DOVirtual.DelayedCall(1f, () => { ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); }, false);
				isSpawning = false;
				return;
			}
			nextSpawnDelay = appearDelay;
		}
	}
}
