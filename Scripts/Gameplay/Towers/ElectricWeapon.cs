using DigitalRuby.ThunderAndLightning;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricWeapon : BaseTowerWeapon
{
	private enum State {
		Idle,
		Fire,
		Reload,
		Aiming
	}

	public SphereCollider buffCollider;
	public float speedModifier;

	public SphereCollider damageCollider;

	public List<LightningBoltPrefabScript> lightnings;

	public Animator anim;

	State state;
	float timer = 0f;

	private Unit target;
	private Vector3 damagePoint;
	private List<Unit> damageUnits = new List<Unit>();

	public override float DPS => ProjectileDamage / (ReloadTime + (Radius / ArrowSpeed));

	void Start() {
		buffCollider.radius = Radius;
		state = State.Idle;
		damageCollider.radius = DamageRadius;
		damageCollider.gameObject.SetActive(false);

		foreach (var l in lightnings) {
			l.gameObject.SetActive(false);
		}
	}

	public override void ApplyBuff(Unit u, BuffType type) {
		if (u == null)
			return;

		switch(type) {
			case BuffType.ElectricTowerSlowDown:
				if ((u.targetMask & (int)tower.towerType) == 0)
					return;
				u.speed.AddPercent(speedModifier,BuffType.ElectricTowerSlowDown,1);
				Gameplay.Combat.CombatEvents.NotifyElectricSlow(u, true);
				break;
			case BuffType.ElectricTowerDamage:
				damageUnits.Add(u);
				break;
		}

	}

	public override void TakeOffBuff(Unit u, BuffType type) {
		if (u == null)
			return;

		switch (type) {
			case BuffType.ElectricTowerSlowDown:
				if ((u.targetMask & (int)tower.towerType) == 0)
					return;
				u.speed.SubPercent(speedModifier,BuffType.ElectricTowerSlowDown);
				Gameplay.Combat.CombatEvents.NotifyElectricSlow(u, false);
				break;
			case BuffType.ElectricTowerDamage:
				damageUnits.Remove(u);
				break;
		}
	}

	// Update is called once per frame
	void Update()
    {
		switch (state) {
			case State.Fire:
				foreach (var e in damageUnits) {
					if (e != null && !e.isDead) {
						e.Damage(tower, Damage * Time.deltaTime, tower.DamageType);
						tower.ApplyTraitsOnContinuousDamage(e);
					}
				}
				timer += Time.deltaTime;
				if (timer >= FireTime) {
					timer = 0f;
					state = State.Reload;
					damageCollider.gameObject.SetActive(false);
					damageUnits.Clear();
					foreach (var l in lightnings) {
						l.gameObject.SetActive(false);
					}
					anim.SetBool("Atack", false);
				}
				break;
			case State.Reload:
				timer += Time.deltaTime;
				if (timer >= ReloadTime) {
					timer = 0f;
					state = State.Idle;
				}
				break;
			case State.Aiming:
				RotateGun();
				if (targetLocked) {
					state = State.Fire;
					RaiseShotFired(transform.position);   // пугает птиц (BirdsManager)
					damageCollider.gameObject.SetActive(true);
					damageCollider.transform.position = target.transform.position;
					foreach(var l in lightnings) {
						l.gameObject.SetActive(true);
						l.Destination.transform.position = damageCollider.transform.position + target.aimOffset;
					}
					anim.SetBool("Atack",true);
					target = null;
					timer = 0f;
				}
				break;
		}

    }

	private void RotateGun() {
		if(target != null)
			targetLocked = RotateGunTo((target.transform.position + target.aimOffset) - weapon.position);
	}

	void FixedUpdate() {
		if (state == State.Idle && (target == null || target.isDead)) {
			GameObject enemy = GameController.Instance.GetEnemy(transform.position, Radius, tower.towerType);
			if (enemy != null) {
				target = enemy.GetComponent<Unit>();
				state = State.Aiming;
			}
		}
	}
}
