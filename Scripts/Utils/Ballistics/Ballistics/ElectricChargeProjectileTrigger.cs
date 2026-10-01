using Gameplay.RoadDamage;
using UnityEngine;

public class ElectricChargeProjectileTrigger : MonoBehaviour {

	// Inspector fields
	[SerializeField] BaseMotion motion;
	[SerializeField] ParticleSystem particleSystem; // Используется для ожидания завершения эффектов
	[SerializeField] GameObject hitEffect;
	[SerializeField] GameObject missEffect;
	[SerializeField] float lifetime = 10f;
	[Tooltip("Шансы повредить дорогу при ударе о землю (RoadTileMap).")]
	[SerializeField] RoadImpactProfile roadImpact = new RoadImpactProfile();

	private bool waitForPartiles = false;

	protected BaseTowerWeapon owner;
	private string impactAudioId;
	private bool impactAudioPlayed;

	public void Init(BaseTowerWeapon owner) {
		this.owner = owner;
		impactAudioId = owner != null ? owner.ImpactAudioId : null;
		impactAudioPlayed = false;
	}

	void Update() {
		lifetime -= Time.deltaTime;
		if (waitForPartiles) {
			if(!particleSystem.IsAlive(true) || lifetime <= 0) {
				Destroy(gameObject);
			}
		} else {
			
			if (transform.position.y <= 0.5 || lifetime <= 0) {
				if (!impactAudioPlayed) {
					impactAudioPlayed = true;
					GameAudioManager.Instance?.Play(impactAudioId, transform.position);
				}
				if (transform.position.y <= 0.5)
					RoadTileMap.ReportImpact(transform.position, roadImpact);
				if (owner != null) {
					owner.DamageUnitsWithProjectileInRadius(transform.position);
				}
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
