using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserWeapon : BaseTowerWeapon {
	private enum State {
		Charging,     // Зарядка
		Firing,       // Стрельба
		Ready         // Готов (полностью заряжен)
	}

	[SerializeField] Transform mountsRoot;
	[SerializeField] ParticleSystem[] chargeSystems;
	[SerializeField] Renderer crystallRenderer;

	[SerializeField] Color emptyColor;
	[SerializeField] Color chargedColor;

	public Laser laser;
	public float headShotTime;
	State state;

	Unit target;
	float headShotDamage;
	float currentCharge = 0f; // Текущий заряд в процентах (0..1)
	float headShotTimer = 0f;
	float idleTimer = 0;

	// Параметры для прерывания заряда
	[SerializeField] private float minChargeToFire = 0.1f; // Минимальный заряд для начала стрельбы (10%)

	public Transform crystall;
	public float animationAmplitude;
	public float animationTime;
	Tween crystalAnim = null;

	private MaterialPropertyBlock propertyBlock;
	private readonly GameAudioLoop firingAudio = new GameAudioLoop(fadeOutOnFinish: true);

	protected override void LateUpdate() {
		base.LateUpdate();
		UpdateFiringAudio();
	}

	private void UpdateFiringAudio() {
		bool beamActive = state == State.Firing && laser != null && laser.beam != null && laser.beam.activeSelf;
		firingAudio.Update(GameAudioManager.Instance?.GetTower(AudioTowerType)?.shotId,
			weapon != null ? weapon.position : transform.position, beamActive);
	}

	protected override void OnDisable() {
		base.OnDisable();
		firingAudio.Stop(fadeOut: true);
	}

	public override float DPS => Damage / (FireTime + ReloadTime);

	void Start() {
		laser.Deactivate();

		// Анимация кристалла
		Vector3 pos = crystall.localPosition;
		float startCrystallY = pos.y;
		pos.y += animationAmplitude;
		crystall.localPosition = pos;
		crystalAnim = crystall.DOLocalMoveY(startCrystallY - animationAmplitude, animationTime / 2f)
			.From(startCrystallY + animationAmplitude)
			.SetEase(Ease.InOutQuad);
		crystalAnim.SetLoops(-1, LoopType.Yoyo);
		crystalAnim.Play();

		propertyBlock = new MaterialPropertyBlock();

		// Начальное состояние - зарядка
		state = State.Charging;
		currentCharge = 0f;
		StartChargeVisuals();
	}

	private void OnDestroy() {
		crystalAnim?.Kill(false);
	}

	void Update() {
		// Обновление вращения оружия и проверка targetLocked
		RotateGun();

		switch (state) {
			case State.Charging:
				UpdateChargingState();
				break;

			case State.Ready:
				UpdateReadyState();
				break;

			case State.Firing:
				UpdateFiringState();
				break;
		}
	}

	void FixedUpdate() {
		UpdateTarget();

		// Проверка возможности начать стрельбу
		CheckIfCanStartFiring();
	}

	void UpdateTarget() {
		Unit nextTarget = FindTarget(GameController.Instance.GetAllEnemies());
		if (nextTarget == target)
			return;

		target = nextTarget;
		targetLocked = false;
		// Хедшот относится к старой цели. Перевод луча не начинает новую атаку
		// и не даёт повторно бросать шанс хедшота.
		headShotTimer = 0f;
		if (state == State.Firing) {
			laser.Deactivate();
			UpdateFiringAudio();
		}
	}

	Unit FindTarget(List<Unit> enemies) {
		Vector3 position = transform.position;
		float sqrRadius = Radius * Radius;
		float remainingFireTime = currentCharge * FireTime;
		float damagePerSecond = Damage;
		bool currentTargetValid = IsValidTarget(target, position, sqrRadius);

		// Сначала заканчиваем уже доступное добивание, чтобы не метаться между целями.
		if (currentTargetValid && GetTimeToKill(target, damagePerSecond, headShotTimer) <= remainingFireTime)
			return target;

		Unit fallback = currentTargetValid ? target : null;
		Unit finisherTarget = null;
		float shortestKillTime = float.PositiveInfinity;
		foreach (Unit enemy in enemies) {
			if (!IsValidTarget(enemy, position, sqrRadius))
				continue;

			if (fallback == null)
				fallback = enemy;

			float killTime = GetTimeToKill(enemy, damagePerSecond, enemy == target ? headShotTimer : 0f);
			if (killTime <= remainingFireTime && killTime < shortestKillTime) {
				finisherTarget = enemy;
				shortestKillTime = killTime;
			}
		}
		return finisherTarget != null ? finisherTarget : fallback;
	}

	bool IsValidTarget(Unit enemy, Vector3 position, float sqrRadius) {
		return enemy != null && !enemy.isDead && enemy.hp > 0f && enemy.isActiveAndEnabled &&
			(enemy.targetMask & (int)tower.towerType) != 0 &&
			(enemy.transform.position - position).sqrMagnitude <= sqrRadius;
	}

	float GetTimeToKill(Unit enemy, float damagePerSecond, float pendingHeadshotTime) {
		float health = enemy.hp;
		if (pendingHeadshotTime > 0f) {
			health -= enemy.GetDamageAfterModifiers(headShotDamage, tower.DamageType);
			if (health <= 0f)
				return pendingHeadshotTime;
		}

		float effectiveDamage = enemy.GetDamageAfterModifiers(damagePerSecond, tower.DamageType);
		return effectiveDamage > 0f ? pendingHeadshotTime + health / effectiveDamage : float.PositiveInfinity;
	}

	/// <summary>
	/// Проверка возможности начать стрельбу
	/// </summary>
	void CheckIfCanStartFiring() {
		if (target == null || target.isDead || state == State.Firing)
			return;

		// Проверяем условия для начала стрельбы:
		// 1. Достаточный заряд
		// 2. Цель захвачена (targetLocked = true)
		if (HasEnoughChargeToFire() && targetLocked) {
			StartFiring();
		}
	}

	/// <summary>
	/// Обновление состояния зарядки
	/// </summary>
	void UpdateChargingState() {
		// Увеличиваем заряд (в процентах в секунду)
		float chargePerSecond = 1f / ReloadTime;
		currentCharge += Time.deltaTime * chargePerSecond;
		currentCharge = Mathf.Min(currentCharge, 1f);

		// Визуальная индикация зарядки
		UpdateChargeVisuals();

		// Если полностью зарядились
		if (currentCharge >= 1f) {
			state = State.Ready;
			OnFullyCharged();

			// Если есть цель и она захвачена - начинаем стрельбу
			if (target != null && !target.isDead && targetLocked) {
				StartFiring();
			}
		}
		// Если появилась захваченная цель и есть минимальный заряд для стрельбы
		else if (target != null && !target.isDead && HasEnoughChargeToFire() && targetLocked) {
			StartFiring();
		}
	}

	/// <summary>
	/// Обновление состояния готовности (полностью заряжен)
	/// </summary>
	void UpdateReadyState() {
		// Если есть захваченная цель - начинаем стрельбу
		if (target != null && !target.isDead && targetLocked) {
			StartFiring();
		}

		if (target == null) {
			idleTimer += Time.deltaTime;
			if (idleTimer >= timeUntilRestPosition) {
				RotateGunTo(tower.GetWeaponDirection());
			}
		}

	}

	/// <summary>
	/// Обновление состояния стрельбы
	/// </summary>
	void UpdateFiringState() {
		// После смерти/ухода цели продолжаем той же порцией заряда, даже ниже
		// порога начала новой атаки. Между FixedUpdate цель могла погибнуть.
		if (!IsValidTarget(target, transform.position, Radius * Radius))
			UpdateTarget();
		if (target == null || currentCharge <= 0f) {
			StopFiring();
			return;
		}

		// Во время перевода оружия заряд не тратится и перезарядка не начинается.
		if (!targetLocked) {
			laser.Deactivate();
			return;
		}
		if (!laser.beam.activeSelf)
			laser.Activate(target);

		// Расходуем заряд в процентах
		// За секунду расходуем (1 / FireTime) процентов
		float firingDeltaTime = Mathf.Min(Time.deltaTime, currentCharge * FireTime);
		float chargeUsedPerSecond = 1f / FireTime;
		currentCharge -= firingDeltaTime * chargeUsedPerSecond;
		currentCharge = Mathf.Max(currentCharge, 0f);

		// Визуальная индикация стрельбы
		UpdateChargeVisuals();

		// Нанесение урона
		if (headShotTimer > 0) {
			float headshotDeltaTime = Mathf.Min(firingDeltaTime, headShotTimer);
			headShotTimer -= headshotDeltaTime;
			firingDeltaTime -= headshotDeltaTime;
			if (headShotTimer <= 0) {
				ApplyHeadshotDamage();
			}
		}
		if (firingDeltaTime > 0f && !target.isDead) {
			target.Damage(tower, Damage * firingDeltaTime, tower.DamageType);
			tower.ApplyTraitsOnContinuousDamage(target);
		}

		// Если заряд закончился - прекращаем стрельбу
		if (currentCharge <= 0) {
			StopFiring();
			// Начинаем новую зарядку с 0
			currentCharge = 0f;
			state = State.Charging;
			StartChargeVisuals();
		}
	}

	/// <summary>
	/// Начать стрельбу
	/// </summary>
	void StartFiring() {
		if (target == null || target.isDead || !HasEnoughChargeToFire() || !targetLocked)
			return;

		idleTimer = 0f;
		state = State.Firing;
		laser.Activate(target);
		UpdateFiringAudio();
		RaiseShotFired(weapon != null ? weapon.position : transform.position, playSound: false);   // пугает птиц (BirdsManager)

		// Проверка способности хедшота
		CheckHeadshotAbility();

		// Останавливаем визуальные эффекты зарядки
		StopChargeVisuals();
	}

	/// <summary>
	/// Остановить стрельбу
	/// </summary>
	void StopFiring() {
		if (state != State.Firing) return;

		laser.Deactivate();
		firingAudio.Stop(fadeOut: true);

		// Если остался заряд после остановки стрельбы
		if (currentCharge > 0) {
			// Если заряд меньше минимального для стрельбы - продолжаем заряжаться
			if (!HasEnoughChargeToFire()) {
				state = State.Charging;
				StartChargeVisuals();
			} else {
				// Иначе переходим в состояние готовности
				state = State.Ready;
			}
		} else {
			// Начинаем новую зарядку с 0
			state = State.Charging;
			StartChargeVisuals();
		}

		// Не сбрасываем target, чтобы не потерять цель при временной потере захвата
		headShotTimer = 0f;
	}

	/// <summary>
	/// Проверить, достаточно ли заряда для начала стрельбы
	/// </summary>
	bool HasEnoughChargeToFire() {
		return currentCharge >= minChargeToFire;
	}

	/// <summary>
	/// Проверка способности хедшота
	/// </summary>
	void CheckHeadshotAbility() {
		headShotTimer = 0f;

		foreach (var ability in tower.abilities) {
			if (ability.type == AbilityType.HeadShot) {
				if (Random.value <= ability.value3) // value3 - шанс срабатывания
				{
					headShotTimer = headShotTime;
					headShotDamage = Damage * ability.value1; // value1 - множитель урона
				}
				break;
			}
		}
	}

	/// <summary>
	/// Применение урона хедшота
	/// </summary>
	void ApplyHeadshotDamage() {
		if (target != null && !target.isDead) {
			target.Damage(tower, headShotDamage, tower.DamageType, true); // хедшот показываем как крит
			headShotTimer = 0f;
		}
	}

	/// <summary>
	/// Обновление визуальных эффектов зарядки
	/// </summary>
	void UpdateChargeVisuals() {
		// Обновляем цвет кристалла в зависимости от заряда
		crystallRenderer.GetPropertyBlock(propertyBlock);
		propertyBlock.SetColor("_InstanceColor", Color.Lerp(emptyColor, chargedColor, currentCharge));
		crystallRenderer.SetPropertyBlock(propertyBlock);
	}

	/// <summary>
	/// Запустить визуальные эффекты зарядки
	/// </summary>
	void StartChargeVisuals() {
		foreach (var system in chargeSystems) {
			if (system.gameObject.activeSelf && !system.isPlaying) {
				system.Play();
			}
		}
	}

	/// <summary>
	/// Остановить визуальные эффекты зарядки
	/// </summary>
	void StopChargeVisuals() {
		foreach (var system in chargeSystems) {
			if (system.gameObject.activeSelf && system.isPlaying) {
				system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			}
		}
	}

	/// <summary>
	/// Вызывается при полной зарядке
	/// </summary>
	void OnFullyCharged() {
		StopChargeVisuals();
	}

	void RotateGun() {
		if (target != null) {
			Vector3 direction = target.transform.position - weapon.position;
			targetLocked = RotateGunTo(direction);
		} else {
			targetLocked = false;
		}
	}

	public override void ApplyOffset(Vector3 offset) {
		mountsRoot.transform.position = offset;
	}
}
