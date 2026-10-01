using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawningUnit : Unit
{
	[Header("Spawning stuff")]
	public UnitType unitToSpawn;

	public int minSpawnCount;
	public int maxSpawnCount;

	public float minSpawnDelay;
	public float maxSpawnDelay;

	public SpawnUnitWithEffect spawnEffectPrefab;

	public float appearDelay;

	public Transform spawnPoint;
	public ParticleSystem spawnShootEffect;

	private float spawningTimer;
	private float spawnDelay;
	private float nextSpawnDelay;

	private bool isSpawning = false;
	private int spawnCounter;

	public override bool IsMoving => base.IsMoving && !isSpawning;

	public override void Start() {
		base.Start();
		spawnDelay = 0; //Random.Range(minSpawnDelay, maxSpawnDelay);
		spawningTimer = 0;
	}


	public override void Update() {
		if(isSpawning) {
			nextSpawnDelay -= Time.deltaTime;
			if(nextSpawnDelay <= 0) {
				Vector3 spawnCenter;
				if (currentWaypoint.previous != null) {
					Vector3 normal = currentWaypoint.position - currentWaypoint.previous.position;
					Vector3 heading = transform.position - currentWaypoint.previous.position;
					spawnCenter = currentWaypoint.previous.position + Vector3.Project(heading, normal);
				} else
					spawnCenter = transform.position;

				Vector3 randomRotation = Quaternion.Euler(Random.Range(-90f,90f), Random.Range(0f, 180f), 0f) * Vector3.up;

				spawnShootEffect.gameObject.transform.forward = randomRotation;
				spawnShootEffect.Play();

				SpawnUnitWithEffect spawnEffect = Instantiate(spawnEffectPrefab, spawnPoint.position, Quaternion.LookRotation(randomRotation));

				spawnEffect.Init(waveId, unitToSpawn, currentWaypoint, spawnCenter, spawnCounter, appearDelay, ChildHpMultiplier, RewardMultiplier);

				spawnCounter--;
				isSpawning = false;
				return;
			}
		} else {
			base.Update();
			spawningTimer += Time.deltaTime;
		}
	}

	protected override void OnJumpEnd() {
		if(spawningTimer >= spawnDelay) {
			spawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
			spawningTimer = 0;
			isSpawning = true;
			nextSpawnDelay = appearDelay;
			spawnCounter = Random.Range(minSpawnCount, maxSpawnCount);
		}
	}
}
