using UnityEngine;

public abstract class BaseTowerWeapon : MonoBehaviour, IDamageDealer 
{
	[SerializeField] protected Transform weapon;
	[SerializeField] protected Transform weaponBase;
	// Preview in the Book: the base yaws left/right as if searching for a target
	public Transform WeaponBase => weaponBase;
	public Transform WeaponPivot => weapon;
	[SerializeField] protected float timeUntilRestPosition = 3f;

	// ������� �������� ��� �������
	[SerializeField] protected float baseReloadTime = 1f;
	// Сырое значение с префаба — для расчёта показателей в книге без Construct/реликвий
	public float BaseReloadTime => baseReloadTime;
	// Перезарядка при заданной скорости без реликвий — та же кривая, что в ReloadTime
	public float GetBaseReloadTime(float speed) => baseReloadTime * (RELOAD_SOFTNESS / (speed + RELOAD_SOFTNESS));
	[SerializeField] protected float baseRotationSpeed = 100f;
	
	// ��������� ��� ������ (��� ������, ��� ����� ������)
	private const float RELOAD_SOFTNESS = 100f;
	private const float ROTATION_SOFTNESS = 800f;
	private const float MAX_ROTATION_MULTIPLIER = 16f; // ������������ ��������� ��������

	protected BaseTower tower;
	protected bool targetLocked = false;

	// Глобальное событие «башня выстрелила» (позиция дула) — пугает птиц (BirdsManager).
	// Статик: подписчики обязаны отписываться (BirdsManager.OnDestroy).
	public static event System.Action<Vector3> ShotFired;
	public TowerType AudioTowerType => tower != null ? tower.towerType : TowerType.Placeholder;
	public string ImpactAudioId => GameAudioManager.Instance?.GetTower(AudioTowerType)?.impactId;
	private readonly GameAudioLoop rotationAudio = new GameAudioLoop();
	private int lastRotationFrame = -1;
	private float rotationAudioAngle;
	private float rotationAudioSpeed;
	private bool rotationAudioActive;
	private float rotationAudioLastMovementTime;

	protected void RaiseShotFired(Vector3 position, bool playSound = true) {
		var manager = GameAudioManager.Instance;
		if (playSound && manager != null) manager.Play(manager.GetTower(AudioTowerType)?.shotId, position);
		ShotFired?.Invoke(position);
	}

	protected virtual void LateUpdate() {
		var settings = GameAudioManager.Instance?.GetTower(AudioTowerType);
		if (tower == null || settings == null || string.IsNullOrEmpty(settings.rotationId) ||
			Time.timeScale <= 0f || GameAudio.SoundVolume <= 0f) {
			StopRotationAudio();
			return;
		}

		bool moving = lastRotationFrame == Time.frameCount;
		if (moving) {
			// Thresholds only gate the start. A turn keeps sounding as it approaches its target.
			if (rotationAudioActive || (rotationAudioAngle >= settings.rotationMinAngle &&
				rotationAudioAngle / Mathf.Max(0.01f, rotationAudioSpeed) >= settings.rotationMinDuration)) {
				rotationAudioActive = true;
				rotationAudioLastMovementTime = Time.time;
			}
		} else if (Time.time - rotationAudioLastMovementTime >= settings.rotationStopDelay) {
			rotationAudioActive = false;
		}
		rotationAudio.Update(settings.rotationId, transform.position, rotationAudioActive, allowStart: moving);
	}

	private void StopRotationAudio() {
		rotationAudio.Stop();
		rotationAudioActive = false;
		lastRotationFrame = -1;
	}

	protected virtual void OnDisable() => StopRotationAudio();

	public float Radius => tower.GetCharacteristicValue(CharacteristicType.Radius);
	
	/// <summary>
	/// ����� �����������, ����������� � ������ Speed, �� ������� �� ��������� 0.
	/// �������: BaseReloadTime * (K / (Speed + K))
	/// ��� Speed=1: ~99% �� ��������, ��� Speed=1000: ~9% �� ��������
	/// </summary>
	public float ReloadTime {
		get {
			float speed = tower.GetCharacteristicValue(CharacteristicType.Speed);
			// Фактор реликвий — делителем поверх насыщающейся кривой Speed: «+10% скорости атаки»
			// честно даёт +10% темпа, а не +10% к Speed (что дало бы ~+5% и меньше)
			float relicFactor = Mathf.Max(0.01f, Gameplay.Relics.RelicsManager.GetFactor(Gameplay.Relics.RelicValue.TowerAttackSpeed));
			return baseReloadTime * (RELOAD_SOFTNESS / (speed + RELOAD_SOFTNESS)) / relicFactor;
		}
	}
	
	/// <summary>
	/// �������� ��������, ������������� � ������ Speed � ��������� �������.
	/// �������: BaseRotationSpeed * (1 + (MaxMultiplier - 1) * Speed / (Speed + K))
	/// ��� Speed=1: ~1.37x, ��� Speed=100: ~13.7x, ��� Speed=1000: ~19.1x
	/// </summary>
	public float RotationSpeed {
		get {
			float speed = tower.GetCharacteristicValue(CharacteristicType.Speed);
			float multiplier = 1f + MAX_ROTATION_MULTIPLIER * speed / (speed + ROTATION_SOFTNESS);
			return baseRotationSpeed * multiplier;
		}
	}
	
	public float ProjectileDamage => tower.GetCharacteristicValue(CharacteristicType.ProjectileDamage);
	public float ArrowSpeed => tower.GetCharacteristicValue(CharacteristicType.ProjectileSpeed);
	public float DamageRadius => tower.GetCharacteristicValue(CharacteristicType.ProjectileAOERadius);
	public float FireTime => tower.GetCharacteristicValue(CharacteristicType.FireTime);
	public float Damage => tower.GetCharacteristicValue(CharacteristicType.Damage);

	public virtual float DPS { get; }

	public void Construct(BaseTower tower) {
		this.tower = tower;
	}

	// Демо-режим (RoadDamageDemo): принудительная точка прицеливания вместо поиска врагов.
	// Поддерживают оружия со снарядами; лучи/молнии игнорируют.
	public Vector3? ForcedTargetPoint { get; set; }
	public virtual bool SupportsForcedTarget => false;

	protected bool HasTarget(Unit unit) => ForcedTargetPoint.HasValue || unit != null;

	/// <summary>Точка (y=0) и скорость цели для баллистики: принудительная точка или юнит.</summary>
	protected bool TryGetAimPoint(Unit unit, out Vector3 position, out Vector3 velocity) {
		if (ForcedTargetPoint.HasValue) {
			position = ForcedTargetPoint.Value;
			position.y = 0f;
			velocity = Vector3.zero;
			return true;
		}
		if (unit == null || unit.isDead) {
			position = Vector3.zero;
			velocity = Vector3.zero;
			return false;
		}
		position = unit.transform.position;
		position.y = 0f;
		velocity = unit.Velocity;
		return true;
	}

	public virtual bool RotateGunTo(Vector3 direction) {
		Quaternion targetRotation = Quaternion.LookRotation(direction);
		Quaternion baseRotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
		Quaternion crossBowRotation = Quaternion.Euler(targetRotation.eulerAngles.x, 0f, 0f);

		float dotProductBase = Quaternion.Dot(weaponBase.rotation, baseRotation);
		float dotProductCrossBow = Quaternion.Dot(weapon.localRotation, crossBowRotation);

		Quaternion previousBase = weaponBase.rotation;
		Quaternion previousWeapon = weapon.localRotation;
		float rotationSpeed = RotationSpeed;
		weaponBase.rotation = Quaternion.RotateTowards(weaponBase.rotation, baseRotation, rotationSpeed * Time.deltaTime);
		weapon.localRotation = Quaternion.RotateTowards(weapon.localRotation, crossBowRotation, rotationSpeed * Time.deltaTime);

		var soundSettings = GameAudioManager.Instance?.GetTower(AudioTowerType);
		float movedAngle = Mathf.Abs(Mathf.DeltaAngle(previousBase.eulerAngles.y, weaponBase.eulerAngles.y));
		float remainingAngle = Mathf.Abs(Mathf.DeltaAngle(previousBase.eulerAngles.y, baseRotation.eulerAngles.y));
		if (soundSettings == null || !soundSettings.rotationHorizontalOnly) {
			movedAngle = Mathf.Max(movedAngle,
				Mathf.Abs(Mathf.DeltaAngle(previousWeapon.eulerAngles.x, weapon.localEulerAngles.x)));
			remainingAngle = Mathf.Max(remainingAngle,
				Mathf.Abs(Mathf.DeltaAngle(previousWeapon.eulerAngles.x, crossBowRotation.eulerAngles.x)));
		}
		// Compare the actual driven axes: Quaternion.Angle can report tiny changes at rest.
		if (movedAngle > 0.01f) {
			lastRotationFrame = Time.frameCount;
			rotationAudioAngle = remainingAngle;
			rotationAudioSpeed = rotationSpeed;
		}

		return Mathf.Abs(dotProductCrossBow) > 0.99 && Mathf.Abs(dotProductBase) > 0.99;
	}

	public virtual void DamageUnitWithProjectile(Unit u, Transform projectile) {
		if (u != null) {
			// Скоуп удара: CombatEvents соберёт задетых/убитых этим попаданием (таски уровня)
			using (Gameplay.Combat.CombatEvents.BeginHit(tower)) {
				if (projectile != null) {
					u.DamageWithEffect(tower, ProjectileDamage, projectile.position, projectile.forward * -1, tower.DamageType);
				} else {
					u.Damage(tower, ProjectileDamage, tower.DamageType);
				}

				tower.ApplyTraitsOnProjectileDamage(u);
			}
		}
	}

	public virtual void DamageUnitsWithProjectileInRadius(Vector3 position) {
		Unit[] units = GameController.Instance.GetEnemiesInRadius(position, tower.GetCharacteristicValue(CharacteristicType.ProjectileAOERadius), tower.towerType);
		using (Gameplay.Combat.CombatEvents.BeginHit(tower)) {
			foreach (var u in units) {
				u.Damage(tower, ProjectileDamage, tower.DamageType);
				tower.ApplyTraitsOnAreaDamage(u);
			}
		}
	}

	public virtual void UpdateRotationFromOtherWeapon(BaseTowerWeapon other) {
		if(weapon != null)
			weapon.localRotation = other.weapon.localRotation;
		weaponBase.rotation = other.weaponBase.rotation;
	}

	public virtual void UpdateState() {
	}

	public virtual void ApplyBuff(Unit u, BuffType type) {
	}

	public virtual void TakeOffBuff(Unit u, BuffType type) {
	}

	public virtual void ApplyOffset(Vector3 offset) {
	}
}
