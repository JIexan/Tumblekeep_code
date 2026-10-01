using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MortarWeapon : BaseTowerWeapon
{
	private enum State {
		Idle,
		Fire,
		Reload,
		Aiming
	}

	public BallisticMotion arrow;

	public GameObject shotEffect;
	public Animator anim;

	public GameObject gear;
	public float gearRatio;

	private Unit target;
	private State state;
	private float timer = 0f;

	private Vector3 calculatedVelocity;
	private float gravity;

	public float arkHeight;

	// Среднее число целей под сплешом; радиус AOE — метры, множителем DPS быть не может
	[SerializeField] private float aoeEfficiency = 2f;
	public float AoeEfficiency => aoeEfficiency;

	public override float DPS => ProjectileDamage / (ReloadTime + ((Radius * 2) / ArrowSpeed)) * aoeEfficiency;

	public override bool SupportsForcedTarget => true;

	// Update is called once per frame
	void Update()
    {
		switch (state) {
			case State.Fire:
				anim.SetTrigger("Shot");

				//Выпустить стрелу в цель и уйти на перезарядку
				//Без родителя: transform здесь — это weaponBase, который крутится и анимируется,
				//и снаряд-потомок таскался бы вслед за башней
				RaiseShotFired(weapon.position);   // пугает птиц (BirdsManager)
				var proj = Instantiate<BallisticMotion>(arrow, weapon.position, weapon.rotation);
				proj.Initialize(weapon.position, gravity, calculatedVelocity);
				//У MortarShot нет stopAction=Destroy, поэтому эффекты выстрела копились
				//в детях башни до конца уровня — чистим руками
				var effect = Instantiate(shotEffect, weapon.position + weapon.forward * 0.8f, weapon.rotation, transform);
				Destroy(effect, 5f);

				SplashDamageProjectileTrigger trigger = proj.GetComponent<SplashDamageProjectileTrigger>();
				trigger.Init(this);

				state = State.Reload;
				timer = 0;

				break;
			case State.Reload:
				anim.ResetTrigger("Shot");
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
					state = State.Fire;
					timer = 0;
				}
				break;
			case State.Idle:
				timer += Time.deltaTime;
				if (timer >= timeUntilRestPosition) {
					RotateGunTo(tower.GetWeaponDirection());
				}
				break;
		}
    }

	public override bool RotateGunTo(Vector3 direction) {
		float rotationAngle = weaponBase.transform.eulerAngles.y;

		targetLocked = base.RotateGunTo(direction);

		rotationAngle -= weaponBase.transform.eulerAngles.y;
		RotateGear(rotationAngle);
		return targetLocked;
	}

	void RotateGun() {
		if (!TryGetAimPoint(target, out Vector3 position, out Vector3 targetVelocity))
			return;

		if (!fts.solve_ballistic_arc_lateral(weapon.position, ArrowSpeed, position, targetVelocity / 2f, arkHeight, out calculatedVelocity, out gravity, out var impactPoint)) {
			Debug.LogError("ballistic error");
		}

		targetLocked = RotateGunTo(calculatedVelocity);
	}

	public override void UpdateRotationFromOtherWeapon(BaseTowerWeapon other) {
		float rotationAngle = weaponBase.transform.eulerAngles.y;

		base.UpdateRotationFromOtherWeapon(other);

		rotationAngle -= weaponBase.transform.eulerAngles.y;
		RotateGear(rotationAngle);
	}

	private void RotateGear(float rotationAngle) {
		rotationAngle *= -gearRatio;
		gear.transform.Rotate(new Vector3(0f, rotationAngle, 0f));
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
