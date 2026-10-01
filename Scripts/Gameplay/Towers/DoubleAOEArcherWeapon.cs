using DG.Tweening;
using UnityEngine;

public class DoubleAOEArcherWeapon : BaseTowerWeapon
{
	private enum State {
		Idle,
		Reload,
		Aiming,
		Tension,
	}

	public BallisticMotion arrow;
	public float arkHeight;
	public Animator animator;

	public Transform arrowObject;
	public AnimationCurve arrowReloadCurve;

	public MeshRenderer reloadingArrow1;
	public MeshRenderer reloadingArrow2;
	public int reloadAnimationLength;

	public float targetingRandomness;

	Unit target;

	State state;

	float timer = 0f;

	private Vector3 calculatedVelocity;
	private float gravity;
	private Material reloadingArrowMaterial1;
	private Material reloadingArrowMaterial2;

	public override bool SupportsForcedTarget => true;

	void Start() {
		animator.SetFloat("reloadMultiplier", (reloadAnimationLength / 30f) / ReloadTime);
		reloadingArrowMaterial1 = new Material(reloadingArrow1.material);
		//reloadingArrowMaterial1.DisableKeyword("INVERT_UV");
		reloadingArrow1.material = reloadingArrowMaterial1;

		reloadingArrowMaterial2 = new Material(reloadingArrow2.material);
		//reloadingArrowMaterial2.DisableKeyword("INVERT_UV");
		reloadingArrow2.material = reloadingArrowMaterial2;
	}

	// Update is called once per frame
	void Update() {
		if (!HasTarget(target))
			return;

		switch (state) {
			case State.Reload:
				timer += Time.deltaTime;
				if (timer >= ReloadTime / 2f)
					RotateGun();
				if (timer >= ReloadTime) {
					timer = 0f;
					if (HasTarget(target))
						state = State.Aiming;
					else
						state = State.Idle;
				}
				break;
			case State.Aiming:
				RotateGun();
				if (targetLocked) {
					state = State.Tension;
					animator.SetTrigger("Start");
				}
				break;
			case State.Tension:
				RotateGun();
				break;
		}
	}

	public void OnTentionComplete() {
		state = State.Reload;
		FireProjectile(reloadingArrow1.transform, calculatedVelocity + Random.onUnitSphere * targetingRandomness);
		FireProjectile(reloadingArrow2.transform, calculatedVelocity + Random.onUnitSphere * targetingRandomness);


		reloadingArrow1.material.SetColor("_Color", new Color(1f, 1f, 1f, 0f));
		reloadingArrow1.material.DOColor(new Color(1f, 1f, 1f, 1f), "_Color", ReloadTime).SetEase(arrowReloadCurve);

		reloadingArrow2.material.SetColor("_Color", new Color(1f, 1f, 1f, 0f));
		reloadingArrow2.material.DOColor(new Color(1f, 1f, 1f, 1f), "_Color", ReloadTime).SetEase(arrowReloadCurve);
	}

	private void FireProjectile(Transform arrowOrigin, Vector3 velocity) {
		RaiseShotFired(arrowOrigin.position);   // пугает птиц (BirdsManager)
		var proj = Instantiate<BallisticMotion>(arrow, arrowOrigin.position, arrowOrigin.rotation, transform);
		//proj.transform.forward = arrowObject.up;

		velocity = Quaternion.AngleAxis(Random.Range(-5f, 5f), Vector3.right) * velocity;
		velocity = Quaternion.AngleAxis(Random.Range(-5f, 5f), Vector3.up) * velocity;
		velocity = Quaternion.AngleAxis(Random.Range(-5f, 5f), Vector3.forward) * velocity;

		proj.Initialize(arrowOrigin.position, gravity * Random.Range(0.9f, 1f), velocity);

		BaseProjectileTrigger trigger = proj.GetComponent<BaseProjectileTrigger>();
		trigger.Init(this);
	}

	void RotateGun() {
		if (!TryGetAimPoint(target, out Vector3 position, out Vector3 targetVelocity))
			return;
		if (!ForcedTargetPoint.HasValue)
			position += target.aimOffset;
		if (!fts.solve_ballistic_arc_lateral(arrowObject.position, ArrowSpeed, position, targetVelocity, arkHeight, out calculatedVelocity, out gravity, out var impactPoint)) {
			Debug.LogError("ballistic error");
		}

		targetLocked = RotateGunTo(calculatedVelocity);
	}

	void FixedUpdate() {
		if (ForcedTargetPoint.HasValue) {
			target = null;
			if (state == State.Idle)
				state = State.Aiming;
			return;
		}

		if(target == null) {
			GameObject enemy = GameController.Instance.GetEnemy(transform.position, Radius, tower.towerType);
			if (enemy != null) {
				target = enemy.GetComponent<Unit>();
				if(state == State.Idle)
					state = State.Aiming;
			}
		} else if((target.transform.position - transform.position).sqrMagnitude > Radius * Radius || target.isDead) {
			target = null;
		}
	}
}
