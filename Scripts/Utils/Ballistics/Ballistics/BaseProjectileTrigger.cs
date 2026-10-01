using DG.Tweening;
using Gameplay.RoadDamage;
using UnityEngine;

public class BaseProjectileTrigger : MonoBehaviour {
	public static event System.Action<Vector3, float> GroundImpacted;

	// Inspector fields
	[SerializeField] protected BaseMotion motion;
	[SerializeField] protected ParticleSystem particleSystem; // Используется для ожидания завершения эффектов
	[SerializeField] protected Collider c;
	[SerializeField] protected TrailRenderer trailRenderer;

	[SerializeField] protected GameObject scaleObject;
	[SerializeField] protected AnimationCurve scaleCurve;
	[SerializeField] protected float scaleTime;
	[SerializeField] protected float finalScale;
	[SerializeField] protected float groundLevel = 0.3f;
	[Min(0f), Tooltip("Радиус испуга птиц при ударе о землю, в метрах. 0 — не пугать.")]
	[SerializeField] protected float birdScareRadius = 2f;

	[Tooltip("Шансы повредить дорогу при ударе о землю (RoadTileMap).")]
	[SerializeField] protected RoadImpactProfile roadImpact = new RoadImpactProfile();

	protected IDamageDealer owner;

	protected float deathTimer = 5f;

	// Снаряд, воткнувшийся в юнита, едет с ним под землю при смерти — это не удар о дорогу
	protected bool hitUnit;
	protected bool groundImpactReported;
	private bool impactAudioPlayed;
	private string impactAudioId;

	protected void PlayImpactAudio() {
		if (impactAudioPlayed) return;
		impactAudioPlayed = true;
		GameAudioManager.Instance?.Play(impactAudioId, transform.position);
	}

	/// <summary>
	/// owner хранится по интерфейсу, поэтому перегруженный оператор == Unity не срабатывает
	/// и уничтоженная башня выглядит как живая ссылка. Проверяем явно.
	/// </summary>
	protected bool OwnerAlive {
		get {
			if (owner == null)
				return false;
			if (owner is Object unityOwner)
				return unityOwner != null;
			return true;
		}
	}

	public void Init(IDamageDealer owner) {
		this.owner = owner;
		// Capture the ID at launch: a projectile may outlive its tower.
		impactAudioId = (owner as BaseTowerWeapon)?.ImpactAudioId;
		impactAudioPlayed = false;

		if(scaleObject != null)
			scaleObject.transform.DOScale(new Vector3(finalScale, finalScale, finalScale), scaleTime).SetEase(scaleCurve);
	}

	// Methods
	void OnTriggerEnter(Collider other) {
		PlayImpactAudio();
		Unit u = other.gameObject.GetComponent<Unit>();
		if (u != null)
			hitUnit = true;
		if (u != null && OwnerAlive) {
			owner.DamageUnitWithProjectile(u, transform);
		}
		if (motion != null)
			motion.enabled = false;
		if (u != null)
			transform.SetParent(u.transform);
		if (trailRenderer != null)
			trailRenderer.emitting = false;
		if (c != null)
			c.enabled = false;
		if (scaleObject != null)
			scaleObject.SetActive(false);
	}

	/// <summary>Один раз сообщает об ударе о землю дороге и птицам (если снаряд не сидит в юните).</summary>
	protected void ReportGroundImpact() {
		if (groundImpactReported)
			return;
		groundImpactReported = true;
		if (!hitUnit) {
			PlayImpactAudio();
			RoadTileMap.ReportImpact(transform.position, roadImpact);
			if (birdScareRadius > 0f)
				GroundImpacted?.Invoke(transform.position, birdScareRadius);
		}
	}

	protected virtual void Update() {
		if (transform.position.y <= groundLevel) {
			ReportGroundImpact();
			if (c != null)
				c.enabled = false;
			if (motion != null)
				motion.enabled = false;
		}
		deathTimer -= Time.deltaTime;
		if (deathTimer <= 0) {
			Destroy(gameObject);
		}
	}

	void OnDestroy() {
		if (scaleObject != null)
			DOTween.Kill(scaleObject.transform);
	}

}
