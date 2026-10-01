using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class AltElectricWeapon : BaseTowerWeapon {
	private enum State {
		Idle,
		Reload,
		Aiming,
		Charging,
	}

	public SeekMotion arrow;
	public float arkHeight;
	public Animator animator;

	public ParticleSystem[] charges;
	public GameObject shotEffect;

	public SphereCollider damageCollider;

	public float slowdownCap = 0.5f;

	[SerializeField] private int maxTargets = 2; // Максимальное количество целей
	public int MaxTargets => maxTargets;

	// Время накопления заряда перед залпом. Раньше здесь ждалась вторая полная
	// перезарядка — реальный каденс был ~2×ReloadTime и DPS башни вдвое ниже заявленного.
	[SerializeField] private float chargeWindup = 0.3f;

	List<Unit> targets = new List<Unit>(); // Теперь список целей вместо одной

	State state;

	float timer = 0f;

	public override float DPS => ProjectileDamage * maxTargets / (ReloadTime + (Radius / ArrowSpeed));

	private float SlowDownFactor {
		get {
			float slowdownValue = tower.GetCharacteristicValue(CharacteristicType.Slowdown) / 100f;
			return 1f - ((slowdownValue * slowdownCap) / (slowdownValue + slowdownCap));
		}
	}


	// Update is called once per frame
	void Update()
    {
		switch (state) {
			case State.Reload:
				timer += Time.deltaTime;
				if(timer >= ReloadTime / 2f)
					RotateGun();
				if (timer >= ReloadTime) {
					timer = 0f;
					if (targets != null && targets.Count > 0)
						state = State.Aiming;
					else
						state = State.Idle;
				}
				break;
			case State.Aiming:
				RotateGun();
				if (targetLocked) {
					timer = 0f;
					state = State.Charging;
					animator.SetBool("Atack", true);
					for (int i = 0; i < charges.Length; i++) {
						charges[i].gameObject.SetActive(true);
						charges[i].Play();
					}
					DOVirtual.DelayedCall(chargeWindup, OnChargeComplete, false);
				}
				break;
			case State.Charging:
				RotateGun();
				break;
			case State.Idle:
				timer += Time.deltaTime;
				if (timer >= timeUntilRestPosition) {
					RotateGunTo(tower.GetWeaponDirection());
				}
				break;
		}
    }

	public override void UpdateState() {
		damageCollider.radius = Radius;
	}

	public void OnChargeComplete() {
		state = State.Reload;
		animator.SetBool("Atack", false);

		if (targets.Count > 0)
			RaiseShotFired(transform.position);   // один испуг птиц на залп (BirdsManager)

		// Запускаем снаряды для всех целей
		for (int i = 0; i < charges.Length; i++) {
			Unit target = null;
			if (i < targets.Count && targets[i] != null) {
				target = targets[i];
			} else if(targets.Count > 0) {
				target = targets[0];
			}

			if (target == null) 
				continue;

			var chargeTransform = charges[i].transform;

			var proj = Instantiate<SeekMotion>(arrow, chargeTransform.position,
				GetRandomRotationInHemisphere(chargeTransform.rotation, 45f), transform);
			proj.Initialize(target, ArrowSpeed, ArrowSpeed * 2f);

			Instantiate(shotEffect, chargeTransform.position, chargeTransform.rotation, transform);

			ElectricChargeProjectileTrigger trigger = proj.GetComponent<ElectricChargeProjectileTrigger>();
			trigger.Init(this);
			charges[i].Stop();
			charges[i].gameObject.SetActive(false);
		}


	}

	void RotateGun() {
		if (targets.Count == 0) {
			targetLocked = false;
			return;
		}

		// Для нескольких целей вычисляем среднюю позицию
		Vector3 averagePosition = Vector3.zero;

		// Убираем null цели
		targets.RemoveAll(t => t == null);

		if (targets.Count == 0) {
			return;
		}

		for (int i = 0; i < Mathf.Min(targets.Count, maxTargets); i++) {
			averagePosition += targets[i].transform.position + targets[i].aimOffset;
		}

		averagePosition /= targets.Count;

		// Вычисляем траекторию для средней точки
		Vector3 velocity = averagePosition - charges[0].transform.position;
		velocity.y = 0f;
		velocity.Normalize();
		/*float gravity;
		if (!fts.solve_ballistic_arc_lateral(charges[0].transform.position, ArrowSpeed,
			averagePosition, Vector3.zero, arkHeight, out velocity, out gravity, out var impactPoint)) {
			Debug.LogError("ballistic error");
			targetLocked = false;
			return;
		}*/

		// Вращаем оружие к средней точке
		targetLocked = RotateGunTo(velocity);
		
	}

	public override void DamageUnitsWithProjectileInRadius(Vector3 position) {
		Unit[] units = GameController.Instance.GetEnemiesInRadius(position, DamageRadius, TowerType.Electric);
		using (var hit = Gameplay.Combat.CombatEvents.BeginHit(tower)) {
			// Урон разряда отложен (тик электро-эффекта через 0.25 с) — задетых отдаём удару списком
			hit.ReportTargets(units.Length);
			foreach (var u in units) {
				u.AddPeriodicEffect(tower, ProjectileDamage, 0.25f, UnitEffect.ElectricDamage, 1, 1);
				tower.ApplyTraitsOnAreaDamage(u);
			}
		}
	}

	public override void ApplyBuff(Unit u, BuffType type) {
		if (u == null)
			return;

		switch (type) {
			case BuffType.ElectricTowerSlowDown:
				if ((u.targetMask & (int)tower.towerType) == 0)
					return;
				u.speed.AddPercent(SlowDownFactor, BuffType.ElectricTowerSlowDown, 1);
				Gameplay.Combat.CombatEvents.NotifyElectricSlow(u, true);
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
				u.speed.SubPercent(SlowDownFactor, BuffType.ElectricTowerSlowDown);
				Gameplay.Combat.CombatEvents.NotifyElectricSlow(u, false);
				break;
		}
	}

	void FixedUpdate() {
		if (state == State.Charging)
			return;

		// Очищаем список целей от null и мертвых
		targets.RemoveAll(t => t == null || t.isDead);

		// Находим новые цели, если нужно
		while (targets.Count < maxTargets) {
			GameObject enemy = GameController.Instance.GetEnemy(transform.position, Radius, tower.towerType, targets);
			if (enemy != null) {
				Unit unit = enemy.GetComponent<Unit>();
				// Проверяем, не является ли юнит уже в списке целей
				if (!targets.Contains(unit)) {
					targets.Add(unit);
					if (state == State.Idle)
						state = State.Aiming;
				}
			} else {
				break;
			}
		}

		// Удаляем цели, которые вышли за радиус или мертвы
		for (int i = targets.Count - 1; i >= 0; i--) {
			if (targets[i] == null || targets[i].isDead ||
				(targets[i].transform.position - transform.position).sqrMagnitude > Radius * Radius) {
				targets.RemoveAt(i);
			}
		}

		// Если целей нет, переходим в состояние покоя
		if (targets.Count == 0 && state != State.Reload && state != State.Idle) {
			state = State.Idle;
			timer = 0f;
		}
	}

	private Quaternion GetRandomRotationInHemisphere(Quaternion baseRotation, float maxAngle = 90f) {
		return baseRotation * Quaternion.Euler(Random.Range(0f, maxAngle), 0f, Random.Range(0f, maxAngle));
	}
}
