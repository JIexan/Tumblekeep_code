using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawningCage : MonoBehaviour
{
	private UnitType unit;
	private Waypoint currentWaypoint;
	private int waveId;
	private float delay;
	private bool waiting = false;
	private bool particleSustemStarted = false;

	public ParticleSystem effect;
	public float spawnDelay;
	public Animator anim;
	public float deathDelay;

	private float hpMultiplier = 1f;
	private float rewardMultiplier = -1f;

	// Пленник наследует множители ХП/наград носильщика
	public void Init(int waveId, float delay, UnitType unit, Waypoint currentWaypoint, float hpMultiplier = 1f, float rewardMultiplier = -1f) {
		this.waveId = waveId;
		this.unit = unit;
		this.hpMultiplier = hpMultiplier;
		this.rewardMultiplier = rewardMultiplier;
		this.currentWaypoint = currentWaypoint;
		this.delay = delay;
		waiting = true;
		effect.gameObject.SetActive(true);
		anim.SetBool("isMoving", false);
	}

	public void SpawnUnit() {
		GameController.Instance.SpawnUnitExactPosition(waveId, transform.position, Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f), unit, -1, currentWaypoint, hpMultiplier, false, rewardMultiplier);
		particleSustemStarted = true;
	}

	// Update is called once per frame
	void Update()
    {
		if (waiting) {
			delay -= Time.deltaTime;
			if (delay <= 0) {
				waiting = false;
				anim.SetTrigger("spawn");
				effect.Play();
				DOVirtual.DelayedCall(spawnDelay, SpawnUnit, false);
				transform.DOMove(transform.position + Vector3.down, 4f).SetEase(Ease.OutCirc).SetDelay(deathDelay).OnComplete(() => { Destroy(gameObject); });
				//LeanTween.move(gameObject, transform.position + Vector3.down, 4f).setEase(LeanTweenType.easeOutCirc).setDelay(deathDelay).setOnComplete(() => { Destroy(gameObject); });
				//GetComponent<MeshRenderer>().enabled = false;
			}
		} 
		/*if(particleSustemStarted) { 
			if(!effect.IsAlive(true)) {
				Destroy(gameObject);
			}
		}*/
    }
}
