using UnityEngine;

public class SplashDamageProjectileTrigger : BaseProjectileTrigger {
	[SerializeField] GameObject hitEffect;
	private bool waitForPartiles = false;

	// Update базового класса тут не вызывается, поэтому нужен свой предохранитель,
	// чтобы снаряд не жил вечно, если он так и не опустился до groundLevel
	private float lifeTimer = 15f;

	protected override void Update() {
		if (waitForPartiles) {
			if(!particleSystem.IsAlive(true)) {
				Destroy(gameObject);
			}
		} else {
			lifeTimer -= Time.deltaTime;
			if (transform.position.y <= groundLevel || lifeTimer <= 0f) {
				if (transform.position.y <= groundLevel)
					ReportGroundImpact();
				PlayImpactAudio();
				if(OwnerAlive) {
					owner.DamageUnitsWithProjectileInRadius(transform.position);
				}
				if(motion != null)
					motion.enabled = false;
				if (particleSystem != null) {
					particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
					waitForPartiles = true;
				} else {
					Destroy(gameObject);
				}
				Instantiate(hitEffect, transform.position, Quaternion.identity);
			}
		}
	}

}
