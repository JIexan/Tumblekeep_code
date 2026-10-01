using DG.Tweening;
using UnityEngine;

public class ArcherWeapon : BaseTowerWeapon
{
	protected enum State {
		Idle,
		Reload,
		Aiming,
		Tension,
	}

	public TowerArrow arrow;
	public float arkHeight;
	public Animator animator;

	public Transform arrowObject;
	public AnimationCurve arrowReloadCurve;

	[SerializeField] protected TowerArrow reloadingArrow;
	[SerializeField] protected MeshRenderer[] reloadingArrowParts;
	[SerializeField] protected float reloadAnimationLength;

	Unit target;

	protected State state;

	float timer = 0f;

	private Vector3 calculatedVelocity;
	private float gravity;

	public override float DPS => ProjectileDamage / (ReloadTime + (Radius / ArrowSpeed));

	public override bool SupportsForcedTarget => true;

	public override void UpdateState() {
		base.UpdateState();

		animator.SetFloat("reloadMultiplier", reloadAnimationLength / ReloadTime);
		reloadingArrow.Init(tower);
	}

	// Update is called once per frame
	protected virtual void Update()
    {
		switch (state) {
			case State.Reload:
				timer += Time.deltaTime;
				if(timer >= ReloadTime / 2f)
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
					timer = 0;
					animator.SetTrigger("Start");
				}
				break;
			case State.Tension:
				RotateGun();
				break;
			case State.Idle:
				timer += Time.deltaTime;
				if(timer >= timeUntilRestPosition) {
					RotateGunTo(tower.GetWeaponDirection());
				}
				break;
		}
    }

	public void OnTentionComplete() {
		state = State.Reload;
		ShootProjectile(reloadingArrow.transform);
		AnimateArrow(reloadingArrowParts);
	}

	protected void ShootProjectile(Transform arrow) {
		if (!TryCalculateShot(arrow.position, out var shotVelocity, out var shotGravity))
			return;

		RaiseShotFired(arrow.position);   // пугает птиц (BirdsManager)
		var proj = Instantiate(this.arrow, arrow.position, arrow.rotation, transform);

		proj.Motion.Initialize(arrow.position, shotGravity, shotVelocity);
		proj.Trigger.Init(this);
		proj.Init(tower);
	}

	protected void AnimateArrow(MeshRenderer[] arrowParts) {
		for (int i = 0; i < arrowParts.Length; i++) {
			arrowParts[i].material.SetColor("_Color", new Color(1f, 1f, 1f, 0f));
			arrowParts[i].material.DOColor(new Color(1f, 1f, 1f, 1f), "_Color", ReloadTime).SetEase(arrowReloadCurve);
		}
	}

	void RotateGun() {
		if (!HasTarget(target))
			return;

		if (!TryCalculateShot(reloadingArrow.transform.position, out calculatedVelocity, out gravity)) {
			targetLocked = false;
			return;
		}

		targetLocked = RotateGunTo(calculatedVelocity);
	}

	private bool TryCalculateShot(Vector3 origin, out Vector3 velocity, out float shotGravity) {
		velocity = Vector3.zero;
		shotGravity = 0f;

		if (!TryGetAimPoint(target, out Vector3 position, out Vector3 targetVelocity))
			return false;
		if (!ForcedTargetPoint.HasValue)
			position += target.aimOffset;
		if(!fts.solve_ballistic_arc_lateral(origin, ArrowSpeed, position, targetVelocity, arkHeight, out velocity, out shotGravity, out var impactPoint)) {
			Debug.LogError("ballistic error");
			return false;
		}

		return true;
	}

	void FixedUpdate() {
		if (state == State.Tension)
			return;

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
