using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HealerUnit : Unit
{
	[Header("Healing stuff")]
	public float minHealDelay;
	public float maxHealDelay;
	public float healRadius;

	public int healAmount;
	public float healDuration;

	public ParticleSystem healEffectPrefab;
	public Transform effectStartPoint;

	private float healingTimer;
	private float nextHealDelay;

	private bool healingTimerStarted = false;
	private bool isHealing = false;
	private float healingAnimationDelay;

	public override bool IsMoving => base.IsMoving && !isHealing;

	public override void Start() {
		base.Start();
		nextHealDelay = 0; //Random.Range(minSpawnDelay, maxSpawnDelay);
		healingTimer = 0;
	}


	public override void Update() {
		if(isHealing) {
			healingAnimationDelay -= Time.deltaTime;
			if (healingAnimationDelay <= 0) {
				isHealing = false;
			}
		} else {
			base.Update();
			if(healingTimerStarted)
				healingTimer += Time.deltaTime;
		}
	}

	protected override void OnJumpEnd() {

		if (healingTimerStarted && healingTimer >= nextHealDelay) {
			healingTimerStarted = false;
			nextHealDelay = Random.Range(minHealDelay, maxHealDelay);
			healingTimer = 0;
			if (GameController.Instance.GetDamagedEnemiesInRadiusCount(transform.position, healRadius) > 0) {
				anim.SetTrigger("Heal");
				isHealing = true;
				healingAnimationDelay = 2f;
			}
		}

		if (!healingTimerStarted && GameController.Instance.GetDamagedEnemiesInRadiusCount(transform.position, healRadius) > 0) {
			healingTimerStarted = true;
		}
	}

	public void OnStartHeal() {
		Unit[] units = GameController.Instance.GetDamagedEnemiesInRadius(transform.position, healRadius);
		foreach(var u in units) {
			u.AddPeriodicEffect(null, -(int)(healAmount/ healDuration), 0.1f, UnitEffect.Heal, healDuration, 1);
		}
		ParticleSystem effect = Instantiate(healEffectPrefab, effectStartPoint.position, Quaternion.identity, null);
		effect.Play();
	}

#if UNITY_EDITOR
	public void OnDrawGizmos() {
		Handles.Disc(Quaternion.identity, transform.position, Vector3.up, healRadius, false, 0f);
	}
#endif

}
