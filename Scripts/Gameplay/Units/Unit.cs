using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitEffect {
	Heal,
	SlowDown,
	ElectricDamage,
	Poison,
	Fire,
	Freeze
}

public enum DamageType {
	Physical,
	Magic,
	Fire,
	Ice,
	Poison,
	Electric,
	Pure // Урон, который игнорирует резисты
}

[System.Serializable]
public struct Resistance {
	public DamageType damageType;
	[Range(-1f, 1f)] // -1 = слабость, 0 = нейтрально, 1 = иммунитет
	public float value;
}

public class Unit : MonoBehaviour
{
	private readonly Dictionary<UnitEffect, DamageType> effectDamageTypeMap = new Dictionary<UnitEffect, DamageType> {
		{ UnitEffect.Heal, DamageType.Pure },
		{ UnitEffect.SlowDown, DamageType.Electric },
		{ UnitEffect.ElectricDamage, DamageType.Electric },
		{ UnitEffect.Poison, DamageType.Poison },
		{ UnitEffect.Fire, DamageType.Fire },
		{ UnitEffect.Freeze, DamageType.Ice }
	};


	[System.Serializable]
	public struct UnitRandomLook {
		
		public GameObject[] objects;
	}

	private Dictionary<UnitEffect, List<PeriodicEffect>> periodicEffects = new Dictionary<UnitEffect, List<PeriodicEffect>>();
	private Dictionary<VisualEffectType, ParticleSystemEffect> visualEffects = new Dictionary<VisualEffectType, ParticleSystemEffect>();

	// Яд — одна запись на юнита: повторные попадания не стакаются параллельно (как огонь),
	// а обновляют длительность и поднимают «концентрацию» (см. StatusEffectsSettings)
	private class PoisonState {
		public BaseTower owner;
		public float damagePerSecond;
		public int stacks;
		public float remaining;
		public float tickTimer;
		public bool slowApplied;
	}
	private PoisonState poison;

	protected UnitType type;
	public Vector3 aimOffset;

	[Header("Description")]
	public string name;
	public string discription;
	public TypeMovement typeUnit;
	public SpeedUnit speedUnit;


	[Header("Heath")]
	public VisualEffectType damageEffect;
    public int damagePower;
	public float totalHP;
	public float hp;
	public Vector3 healthBarOffset;

	[Header("Animation")]
	public AnimationCurve jumpCurve;
	public float jumpInterval = 1f;
	public float jumpHeight = 1f;
	[HideInInspector] public bool isDead = false;
	public float deathTimer;
	public Animator anim;
	public float animationSpeedMultiplier;
	public int walkAnimationFrames = 100;
	public float animationCycles = 2f;
	public bool hasDeathAnimation;

	[Header("Moving")]
	public DynamicFloat speed;
	public float wayPointDistance = 20f;
	public float minRotationSpeed = 1f;
	public float maxRotationSpeed = 1f;

	[Header("Shadow")]
	public float minShadowSize;
	public float maxShadowSize;
	public Transform shadowTrackingObject;

	[Header("Appearance")]
	public GameObject scaleObject;
	public UnitRandomLook[] randomLooks;
	[SerializeField] UnitColorAnimator colorAnimator;

	[Header("Spawning")]
	[Tooltip("Полудлина тела вдоль хода для дистанции спавна (WaveSpawner), в юнитах мира. 0 — авто по коллайдеру; задавать, когда модель заметно больше коллайдера (дирижабль, дракон).")]
	[SerializeField] private float spacingHalfLength = 0f;

	[Header("Effects")]
	[SerializeField] private Vector3 effectsScale = new Vector3(1.25f, 1.25f, 1.25f);

	[Header("Resistances")]
	[SerializeField] private Resistance[] resistances;
	private Dictionary<DamageType, float> resistanceDict;

	// Минимальный множитель урона (например, 10% урона всегда проходит)
	[SerializeField] private float minDamageMultiplier = 0.1f;

	public int exp;

	private List<BaseTower> dealtDamageOwners = new List<BaseTower>();
	private BaseTower lastHitter = null; 

	/// <summary>
	/// Разрешено ли юниту двигаться в этом кадре (фриз/отключенный AI останавливают движение)
	/// </summary>
	protected bool MovementEnabled => aiEnabled && !freeze;

	/// <summary>
	/// Двигается ли юнит на самом деле. Наследники дополняют условием своих стоячих состояний.
	/// Башни используют это через Velocity, чтобы не стрелять на опережение по стоящей цели.
	/// </summary>
	public virtual bool IsMoving => MovementEnabled && !isDead;

	public Vector3 Velocity { get { return IsMoving ? transform.forward * speed : Vector3.zero; } }
	public bool Damaged { get { return hp < totalHP; } }
	public float Heath { get { return hp / totalHP; } }

	public virtual float CombinedHP => totalHP;
	public IReadOnlyList<Resistance> Resistances => resistances;

	// Путь, пройденный с момента спавна. Копится только реальным движением (фриз, стоячие состояния,
	// замедления учтены сами собой) — по нему WaveSpawner держит дистанцию до следующего юнита
	public float DistanceTravelled { get; private set; }

	// Габариты тела относительно пивота, в юнитах мира: вперёд/назад вдоль хода и диапазон по высоте
	public struct SpacingBounds {
		public float front;
		public float back;
		public float bottom;
		public float top;

		// Тела на разной высоте (дирижабль над пехотой) друг в друга не клипаются
		public bool OverlapsVertically(SpacingBounds other) {
			return bottom < other.top && other.bottom < top;
		}
	}

	/// <summary>
	/// Габариты по корневому коллайдеру. Считается аналитически из параметров коллайдера, а не через bounds:
	/// работает и на ассете префаба (спавнеру размер следующего юнита нужен до его появления)
	/// </summary>
	public SpacingBounds GetSpacingBounds() {
		Vector3 center = Vector3.zero;
		Vector3 half = Vector3.one * 0.5f; // коллайдера нет — условное тело метр на метр

		switch (GetComponent<Collider>()) {
			case CapsuleCollider capsule:
				center = capsule.center;
				half = Vector3.one * capsule.radius;
				half[capsule.direction] = Mathf.Max(capsule.height * 0.5f, capsule.radius);
				break;
			case BoxCollider box:
				center = box.center;
				half = box.size * 0.5f;
				break;
			case SphereCollider sphere:
				center = sphere.center;
				half = Vector3.one * sphere.radius;
				break;
		}

		Vector3 scale = transform.lossyScale;
		scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
		center = Vector3.Scale(center, scale);
		half = Vector3.Scale(half, scale);

		var res = new SpacingBounds {
			front = Mathf.Max(half.z + center.z, 0f),
			back = Mathf.Max(half.z - center.z, 0f),
			bottom = center.y - half.y,
			top = center.y + half.y + jumpHeight // прыжок походки поднимает тело
		};
		if (spacingHalfLength > 0f)
			res.front = res.back = spacingHalfLength;
		return res;
	}

	private float timer = 0;
	protected float rotationSpeed;

	protected Waypoint currentWaypoint;
	protected Quaternion targetRotation;
	protected float targetAngle;

	protected int waveId;
	protected bool isBoss = false;

	[HideInInspector]public int targetMask;

	private bool aiEnabled = true;
	public bool AIEnabled {
		get {
			return aiEnabled;
		}
		set {
			aiEnabled = value;
			if(anim != null)
				anim.SetBool("Walk", aiEnabled);
		}
	}

	private bool freeze = false;
	public bool Freeze {
		get {
			return freeze;
		}
		set {
			freeze = value;
			if (anim != null)
				anim.enabled = !freeze;
		}
	}

	// rewardMultiplier < 0 — награды масштабируются как ХП
	public void Init(int waveId, UnitType type, float hpMultiplier, bool isBoss, float rewardMultiplier = -1f) {
		this.type = type;
		totalHP *= hpMultiplier;
		hp *= hpMultiplier;
		this.isBoss = isBoss;
		HpMultiplier = hpMultiplier;
		RewardMultiplier = rewardMultiplier >= 0f ? rewardMultiplier : hpMultiplier;
		// Строго после установки isBoss: «Надкусанная корона» режет ХП только боссам
		float relicHpFactor = Gameplay.Relics.RelicsManager.GetFactor(Gameplay.Relics.RelicValue.EnemyMaxHP, new Gameplay.Relics.RelicContext(this));
		totalHP *= relicHpFactor;
		hp *= relicHpFactor;
		SetWaveId(waveId);
		InitVisuals();
	}

	public void SetWaveId(int waveId) {
		this.waveId = waveId;
	}

	public Waypoint CurrentWaypoint {
		get { return currentWaypoint; }
	}

	public int WaveId => waveId;

	public bool IsBoss => isBoss;

	// Дошёл до замка и снят с поля через Kill(false). OnUnitKilled приходит и по таким — это не килл игрока
	public bool ReachedCastle { get; private set; }

	// Множитель ХП, с которым юнит заспавнен (фракция × кап тел; у босса — масштаб под пул волны)
	public float HpMultiplier { get; private set; } = 1f;
	// Множитель наград за килл: у обычных юнитов равен HpMultiplier, у босса — множитель его фракции
	public float RewardMultiplier { get; private set; } = 1f;
	// Что наследуют дети (экипаж, порталы, клетки, всадник): босс передаёт не свой масштаб, а фракционный
	public float ChildHpMultiplier => isBoss ? RewardMultiplier : HpMultiplier;
	public float MinDamageMultiplier => minDamageMultiplier;

	public UnitType Type => type;

	void Awake()
    {
		hp = totalHP;
		rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);

		resistanceDict = new Dictionary<DamageType, float>();
		foreach (var resistance in resistances) {
			if (!resistanceDict.ContainsKey(resistance.damageType)) {
				resistanceDict.Add(resistance.damageType, resistance.value);
			}
		}
	}

	/// <summary>
	/// Включить один вариант внешности из randomLooks (остальные выключить).
	/// Публично — для превью в книге, где случайность не нужна.
	/// </summary>
	public void ApplyLook(int index) {
		if (randomLooks == null || randomLooks.Length == 0)
			return;
		foreach (var l in randomLooks) {
			foreach (var o in l.objects) {
				o.SetActive(false);
			}
		}
		index = Mathf.Clamp(index, 0, randomLooks.Length - 1);
		foreach (var o in randomLooks[index].objects) {
			o.SetActive(true);
		}
	}

	private void InitVisuals() {
		if (randomLooks != null && randomLooks.Length > 0) {
			ApplyLook(Random.Range(0, randomLooks.Length));

			// Пустой scaleObject на префабе раньше ронял Init: юнит не попадал в список врагов,
			// шёл без маршрута, а спавнер корабля зависал навсегда
			if (scaleObject != null) {
				float scale = Random.Range(0.9f, 1.1f);
				scaleObject.transform.localScale *= scale;
			} else {
				Debug.LogWarning($"{name}: scaleObject не задан, случайный масштаб пропущен", this);
			}
		}

		if (ShadowsManager.Instance != null && shadowTrackingObject != null)
			ShadowsManager.Instance.AddShadow(shadowTrackingObject, minShadowSize, maxShadowSize, shadowTrackingObject.localPosition.y, shadowTrackingObject.localPosition.y + jumpHeight);

	}

	public virtual void Start() {
		if (anim != null) {
			anim.SetBool("Walk", aiEnabled);
			animationSpeedMultiplier *= Random.Range(0.9f, 1.1f);
			anim.SetFloat("SpeedMultiplier", animationSpeedMultiplier);
			jumpInterval = (((float)walkAnimationFrames / 30f) / animationSpeedMultiplier) / animationCycles;
		}
	}

	public void SetRouteIndex(int i) {
		currentWaypoint = LevelDesriptor.Instance.GetRoute(i);
	}

	public void SetCurrentWaypoint(Waypoint w) {
		currentWaypoint = w;
	}

	// ===== Полоса =====
	// Боковой сдвиг от оси маршрута (вправо по ходу), юниты мира. Юнит на полосе идёт по траектории, параллельной
	// оси, до самого замка: рулит не в вейпоинт, а в точку, сдвинутую от него по биссектрисе поворота.
	// Без этого боковой разнос на спавне схлопывался к первому же вейпоинту. 0 — идём по оси, как раньше

	private float laneOffset;
	private Vector3 laneOrigin;
	private Waypoint laneTargetWaypoint;
	private Vector3 laneTarget;

	public float LaneOffset => laneOffset;

	/// <summary>
	/// Поставить юнита на полосу. routeOrigin — точка на оси, откуда юнит вышел на маршрут (точка спавна):
	/// по ней считается направление входа в первый вейпоинт
	/// </summary>
	public void SetLane(float offset, Vector3 routeOrigin) {
		laneOffset = offset;
		laneOrigin = routeOrigin;
		laneTargetWaypoint = null;
	}

	// Точка, в которую юнит рулит к текущему вейпоинту; пересчитывается только при смене вейпоинта
	private Vector3 GetWaypointTarget() {
		if (laneOffset == 0f)
			return currentWaypoint.position;
		if (laneTargetWaypoint == currentWaypoint)
			return laneTarget;

		Waypoint w = currentWaypoint;
		Vector3 dirIn = w.position - (w.previous != null ? w.previous.position : laneOrigin);
		dirIn.y = 0f;
		dirIn.Normalize();
		Vector3 dirOut = dirIn;
		if (w.next != null) {
			dirOut = w.next.position - w.position;
			dirOut.y = 0f;
			dirOut.Normalize();
		}

		// Касательная на повороте — биссектриса входа и выхода; на развороте в 180° её нет, берём вход
		Vector3 tangent = dirIn + dirOut;
		if (tangent.sqrMagnitude < 0.0001f)
			tangent = dirIn;
		tangent.Normalize();

		// Митра: на повороте сдвиг длиннее в 1/cos(пол-угла), тогда полоса держит дистанцию до оси на обоих отрезках.
		// Кламп ×2 — на острых углах внешняя полоса не улетает за дорогу
		float cosHalf = Mathf.Max(Vector3.Dot(tangent, dirIn), 0.5f);
		laneTarget = w.position + Vector3.Cross(Vector3.up, tangent) * (laneOffset / cosHalf);
		laneTargetWaypoint = w;
		return laneTarget;
	}

	// isDot — тик периодического эффекта (огонь/яд/электро), а не прямое попадание: для CombatEvents
	public void Damage(BaseTower owner, float damage, DamageType damageType, bool critical = false, bool isDot = false) {
		if (owner != null && !dealtDamageOwners.Contains(owner)) {
			dealtDamageOwners.Add(owner);
		}
		lastHitter = owner;

		Damage(GetDamageAfterModifiers(damage, damageType), critical, owner, damageType, isDot);
	}

	// Оценка урона без попадания: те же резисты и реликвии, что при реальной атаке.
	public float GetDamageAfterModifiers(float damage, DamageType damageType) {
		float finalDamage = ApplyResistance(damage, damageType);
		// Только для урона (damage > 0): хилы врагов идут этим же путём с отрицательным
		// значением — «Сеть птицелова» не должна усиливать лечение
		if (damage > 0f)
			finalDamage *= Gameplay.Relics.RelicsManager.GetFactor(Gameplay.Relics.RelicValue.EnemyDamageTaken, new Gameplay.Relics.RelicContext(this));
		return finalDamage;
	}

	private float ApplyResistance(float baseDamage, DamageType damageType) {
		// Если резист не задан - возвращаем базовый урон
		if (!resistanceDict.TryGetValue(damageType, out float resistanceValue)) {
			return baseDamage;
		}

		// Ограничиваем значение резиста между -1 и 1
		resistanceValue = Mathf.Clamp(resistanceValue, -1f, 1f);

		// Определяем множитель урона
		float damageMultiplier = 1f - resistanceValue;

		// Применяем минимальный множитель урона
		damageMultiplier = Mathf.Max(damageMultiplier, minDamageMultiplier);

		return baseDamage * damageMultiplier;
	}

	private void Damage(float amount, bool critical, BaseTower owner, DamageType damageType, bool isDot) {
		if (hp <= 0)
			return;

		if (amount > 0 && colorAnimator != null) {
			colorAnimator.PlayDamageAnimation();
		}

		float hpBefore = hp;
		hp -= amount;
		if(hp > totalHP) {
			hp = totalHP;
		}

		if (amount > 0f) {
			RegisterDamageText(amount, critical);
			Gameplay.Combat.CombatEvents.ReportDamage(this, owner, damageType, amount, hpBefore, hp, totalHP, isDot);
		} else if (hp > hpBefore) {
			// Фактический хил после клампа — перехил не показываем
			RegisterHealText(hp - hpBefore);
		}

		if (hp <= 0) {
			Kill();
			return;
		}

	}

	// ===== Всплывающие цифры урона/хила =====
	// Лучевые башни и DoT бьют тиками до 60 раз/с — тики меньше единицы копятся и
	// сбрасываются по таймеру, дискретные удары (>= 1) показываются сразу.
	// Хил всегда копится (тики 10/с у хилера).

	private const float DamageTextFlushInterval = 0.45f;
	private const float HealTextFlushInterval = 0.5f;

	private float pendingDamageText;
	private float pendingHealText;
	private float damageTextTimer;
	private float healTextTimer;

	private Vector3 FloatingTextAnchor => transform.position + healthBarOffset;

	private void RegisterDamageText(float amount, bool critical) {
		if (critical) {
			// Крит показываем сразу отдельным числом, не смешивая с накопителем
			FloatingTextManager.Instance?.ShowDamage(Mathf.Max(1f, Mathf.Round(amount)), FloatingTextAnchor, true);
			return;
		}

		pendingDamageText += amount;
		if (amount >= 1f) {
			FlushDamageText();
		}
	}

	private void FlushDamageText() {
		int shown = Mathf.RoundToInt(pendingDamageText);
		if (shown >= 1) {
			pendingDamageText -= shown; // остаток переносится в следующий показ — суммарно честно
			FloatingTextManager.Instance?.ShowDamage(shown, FloatingTextAnchor);
		}
		damageTextTimer = 0f;
	}

	private void RegisterHealText(float actualHealed) {
		pendingHealText += actualHealed;
	}

	private void FlushHealText() {
		int shown = Mathf.RoundToInt(pendingHealText);
		if (shown >= 1) {
			pendingHealText -= shown;
			FloatingTextManager.Instance?.ShowHeal(shown, FloatingTextAnchor);
		}
		healTextTimer = 0f;
	}

	// Намеренно LateUpdate: Update виртуальный и не всеми наследниками пробрасывается (RamUnit),
	// а к LateUpdate тики урона текущего кадра уже слиты в накопитель
	private void LateUpdate() {
		if (isDead)
			return;

		if (pendingDamageText > 0.001f) {
			damageTextTimer += Time.deltaTime;
			if (damageTextTimer >= DamageTextFlushInterval) {
				FlushDamageText();
			}
		} else {
			damageTextTimer = 0f;
		}

		if (pendingHealText > 0.001f) {
			healTextTimer += Time.deltaTime;
			if (healTextTimer >= HealTextFlushInterval) {
				FlushHealText();
			}
		} else {
			healTextTimer = 0f;
		}
	}

	public void DamageWithEffect(BaseTower owner, float amount, Vector3 position, Vector3 normal, DamageType damageType) {
	
		var effect = EffectsManager.Instance.Spawn(damageEffect, transform, position);
		effect.transform.forward = normal;
		Damage(owner, amount, damageType);
	}

	public void Kill(bool addCoins = true)
    {
		if (!isDead) {
			FlushDamageText(); // добивающий тик тоже виден; хил-накопитель при смерти отбрасываем
			StopPoison();
		}

        if (addCoins) {
            var reward = LevelDesriptor.Instance.GetUnitReward(type);
            // Тел в волне с ростом множителя ХП становится меньше — монеты и XP острова растут вместе с ним
            int baseCoins = reward != null ? Mathf.RoundToInt(reward.price * RewardMultiplier) : 0;
            int coins = Gameplay.Relics.RelicsManager.ModifyKillCoins(baseCoins, this);
            CoinsManager.Instance.Add(coins);

            int droppedCrystals = GameController.Instance.MatchRewards.AccrueForKill(reward, RewardMultiplier);
            // Спрайт кристалла вылетает из юнита и летит в HUD-счётчик; счётчик тикает по прилёту
            if (droppedCrystals > 0) {
                GameAudio.Play(GameSound.CrystalDrop, transform.position);
                CrystalsHudCounter.Instance?.DropFromWorld(transform.position, droppedCrystals);
            } // точка ног: вылет с высоты и приземление считает сам счётчик
        }

		GameController.Instance.RemoveEnemy(this, addCoins);

		// После RemoveEnemy: взрыв «Петарды» не должен зацепить умирающего (он уже вне списка врагов).
		// Только наградные киллы — утёкшие до замка (addCoins=false) триггеры не дёргают и опыт башням не дают.
		if (addCoins) {
			Gameplay.Relics.RelicsManager.NotifyRewardedKill(this);

			for(int i = 0; i < dealtDamageOwners.Count; i++) {
				dealtDamageOwners[i].OnUnitKill(this, dealtDamageOwners[i] != lastHitter);
			}

			// «Общий котёл» (особенность острова): остальные башни получают долю опыта за килл —
			// башни рукава, по которому волна не пошла, не отстают в уровнях
			float sharedXP = Gameplay.Relics.RelicsManager.GetFlat(Gameplay.Relics.RelicValue.TowerSharedXP);
			if (sharedXP > 0f) {
				var towers = GameController.Instance.GetAllTowers();
				for (int i = 0; i < towers.Count; i++) {
					if (!dealtDamageOwners.Contains(towers[i]))
						towers[i].OnSharedKillXP(this, sharedXP);
				}
			}
		}

		InGameEventManager.SendEvent(InGameEvents.OnUnitKilled, this);

		isDead = true;

		DeathRoutine();
	}

	public virtual void DeathRoutine() {
		if (hasDeathAnimation) {
			anim.SetTrigger("Dead");
		} else {
			Vector3 finalRotation = transform.rotation.eulerAngles;
			finalRotation.x = 90f;
			Sequence mySequence = DOTween.Sequence();
			mySequence.Append(transform.DORotate(finalRotation, deathTimer / 3f));
			mySequence.Append(transform.DOMoveY(-1, (deathTimer / 3f) * 2f));
			mySequence.Play();
		}
		//LeanTween.rotateX(gameObject, 90, deathTimer / 3f).setOnComplete(x => { LeanTween.moveY(gameObject, -1, (deathTimer / 3f) * 2f); });
	}

    public virtual void Update()
    {
		if(MovementEnabled)
			MovingUpdate();

		UpdatePeriodicEffects();
		UpdatePoison();
	}

	protected void UpdatePeriodicEffects() {
		foreach (var e in periodicEffects.Values) {
			e.RemoveAll(effect => effect.Update(Time.deltaTime));
		}
	}

	protected void MovingUpdate() {
		if (isDead) {
			deathTimer -= Time.deltaTime;
			if (deathTimer <= 0) {
				Destroy(gameObject);
			}
			return;
		}

		// The movement timer starts at zero and resets after each jump. Do not replay
		// mid-jump after a freeze, or retry a sound skipped by the shared voice limit.
		if (timer == 0f && Time.deltaTime > 0f) {
			var audio = GameAudioManager.Instance;
			if (audio != null)
				audio.Play(audio.GetUnitMovement(type, typeUnit), transform.position, true);
		}

		timer += Time.deltaTime;
		if (timer > jumpInterval) {
			timer = 0f;
			OnJumpEnd();
		}
		float t = timer / jumpInterval;
		Vector3 pos = transform.localPosition;
		pos.y = jumpCurve.Evaluate(t) * jumpHeight;
		transform.localPosition = pos;

		MoveForward(speed * Gameplay.Relics.RelicsManager.GetFactor(Gameplay.Relics.RelicValue.EnemySpeed) * Time.deltaTime);

		if (targetAngle != transform.rotation.eulerAngles.y) {
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * speed.GetPercent() * Time.deltaTime);
		}
	}

	// Единственная точка поступательного движения (и у наследников со своим Move) — здесь копится DistanceTravelled
	protected void MoveForward(float distance) {
		transform.position += transform.forward * distance;
		DistanceTravelled += distance;
	}

	protected virtual void OnJumpEnd() { }

	protected virtual void OnNextWaypoint() { }

	private void FixedUpdate() {
		if (currentWaypoint == null)
			return;

		Vector3 toWaypoint = GetWaypointTarget() - transform.position;
		targetAngle = Quaternion.LookRotation(toWaypoint).eulerAngles.y;
		targetRotation = Quaternion.Euler(0f, targetAngle, 0f);

		if (toWaypoint.magnitude < wayPointDistance) {
			currentWaypoint = currentWaypoint.next;

			if (currentWaypoint == null) {
				ReachedCastle = true;
				Castle.Instance.Damage(damagePower);
				Kill(false);
				return;
			}

			toWaypoint = GetWaypointTarget() - transform.position;
			targetAngle = Quaternion.LookRotation(toWaypoint).eulerAngles.y;
			targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
			OnNextWaypoint();
		}
	}

	public void OnBorn() {
		AIEnabled = true;
	}

	// ===== Яд =====

	/// <summary>
	/// Отравить юнита. Первое применение: замедление + визуал. Повторное — обновляет длительность
	/// (берётся большая) и поднимает концентрацию до poisonMaxStacks; урон тика растёт с концентрацией.
	/// owner — башня для начисления опыта за килл (null для спеллов)
	/// </summary>
	public void ApplyPoison(BaseTower owner, float damagePerSecond, float duration) {
		if (isDead || damagePerSecond <= 0f || duration <= 0f)
			return;

		var settings = GameConfigs.Instance.StatusEffectsSettings;
		if (poison == null) {
			poison = new PoisonState {
				owner = owner,
				damagePerSecond = damagePerSecond,
				stacks = 1,
				remaining = duration,
				tickTimer = settings != null ? settings.poisonTickInterval : 1f
			};
			if (settings != null && settings.poisonSlowPercent > 0) {
				speed.AddPercent(1f - settings.poisonSlowPercent / 100f, BuffType.Poison, 1);
				poison.slowApplied = true;
			}
			OnPeriodicEffectAdded(UnitEffect.Poison, duration);
		} else {
			int maxStacks = settings != null ? settings.poisonMaxStacks : 5;
			poison.stacks = Mathf.Min(poison.stacks + 1, maxStacks);
			poison.remaining = Mathf.Max(poison.remaining, duration);
			poison.damagePerSecond = Mathf.Max(poison.damagePerSecond, damagePerSecond);
			if (owner != null)
				poison.owner = owner;
		}
	}

	public bool IsPoisoned => poison != null;
	public int PoisonStacks => poison != null ? poison.stacks : 0;

	private void UpdatePoison() {
		if (poison == null)
			return;

		var settings = GameConfigs.Instance.StatusEffectsSettings;
		float interval = settings != null ? settings.poisonTickInterval : 1f;

		poison.remaining -= Time.deltaTime;
		poison.tickTimer -= Time.deltaTime;
		if (poison.tickTimer <= 0f) {
			poison.tickTimer += interval;
			float multiplier = settings != null ? settings.GetPoisonStackMultiplier(poison.stacks) : 1f;
			Damage(poison.owner, poison.damagePerSecond * interval * multiplier, DamageType.Poison, isDot: true);
			// Тик мог добить юнита: Kill() уже снял яд
			if (poison == null)
				return;
		}

		if (poison.remaining <= 0f)
			StopPoison();
	}

	private void StopPoison() {
		if (poison == null)
			return;

		if (poison.slowApplied) {
			var settings = GameConfigs.Instance.StatusEffectsSettings;
			int slow = settings != null ? settings.poisonSlowPercent : 20;
			speed.SubPercent(1f - slow / 100f, BuffType.Poison);
		}
		poison = null;
		OnPeriodicEffectRemoved(UnitEffect.Poison);
	}

	public void OnPeriodicEffectAdded(UnitEffect effectType, float duration) {

		switch(effectType) {
			case UnitEffect.Heal:
				if (visualEffects.ContainsKey(VisualEffectType.Heal))
					return;

				var healEffect = EffectsManager.Instance.SpawnLooping(VisualEffectType.Heal, transform, transform.position);
				healEffect.transform.forward = Vector3.up;
				visualEffects.Add(VisualEffectType.Heal, healEffect);
				break;
			case UnitEffect.SlowDown:
				if (visualEffects.ContainsKey(VisualEffectType.Electricity))
					return;

				var lightningEffect = EffectsManager.Instance.SpawnLooping(VisualEffectType.Electricity, transform, transform.position + aimOffset);
				lightningEffect.transform.localScale = Vector3.one * GetComponent<CapsuleCollider>().radius * 3f;
				visualEffects.Add(VisualEffectType.Electricity, lightningEffect);
				break;
			case UnitEffect.Freeze:
				if (visualEffects.ContainsKey(VisualEffectType.Freeze))
					return;

				var freezeEffect = EffectsManager.Instance.Spawn(VisualEffectType.Freeze, duration);
				freezeEffect.transform.SetParent(transform);
				freezeEffect.transform.position = transform.position + aimOffset;
				//freezeEffect
				break;

			case UnitEffect.Fire:
				if (visualEffects.ContainsKey(VisualEffectType.Fire))
					return;

				var fireEffect = EffectsManager.Instance.SpawnLooping(VisualEffectType.Fire, transform, transform.position, Quaternion.identity, effectsScale);
				if (fireEffect != null)
					visualEffects.Add(VisualEffectType.Fire, fireEffect);
				break;

			case UnitEffect.Poison:
				if (visualEffects.ContainsKey(VisualEffectType.Poison))
					return;

				var poisonEffect = EffectsManager.Instance.SpawnLooping(VisualEffectType.Poison, transform, transform.position, Quaternion.identity, effectsScale);
				if (poisonEffect != null)
					visualEffects.Add(VisualEffectType.Poison, poisonEffect);
				break;
		}

	}

	public void OnPeriodicEffectRemoved(UnitEffect effectType) {
		switch (effectType) {
			case UnitEffect.Heal:
				if (!visualEffects.ContainsKey(VisualEffectType.Heal))
					return;

				visualEffects[VisualEffectType.Heal].Stop();
				visualEffects.Remove(VisualEffectType.Heal);
				break;

			case UnitEffect.SlowDown:
				if (!visualEffects.ContainsKey(VisualEffectType.Electricity))
					return;

				visualEffects[VisualEffectType.Electricity].Stop();
				visualEffects.Remove(VisualEffectType.Electricity);
				break;

			case UnitEffect.Fire:
				if (!visualEffects.ContainsKey(VisualEffectType.Fire))
					return;

				visualEffects[VisualEffectType.Fire].Stop();
				visualEffects.Remove(VisualEffectType.Fire);
				break;

			case UnitEffect.Poison:
				if (!visualEffects.ContainsKey(VisualEffectType.Poison))
					return;

				visualEffects[VisualEffectType.Poison].Stop();
				visualEffects.Remove(VisualEffectType.Poison);
				break;


		}
	}

	public void AddPeriodicEffect(BaseTower owner, float damage, float interval, UnitEffect effectType, float duration = float.MaxValue, int limit = int.MaxValue) {

		if (!periodicEffects.ContainsKey(effectType)) {
			periodicEffects.Add(effectType, new List<PeriodicEffect>());
		}

		if (periodicEffects[effectType] == null) {
			periodicEffects[effectType] = new List<PeriodicEffect>();
		}

		if (periodicEffects[effectType].Count >= limit)
			return;

		PeriodicEffect effect = new PeriodicEffect {
			unit = this,
			value1 = damage * interval,
			interval = interval,
			timer = interval,
			duration = duration,
			effectType = effectType,
			// ElectricDamage — отложенный ПРЯМОЙ урон залпа AltElectric (один тик), DoT-ом считаем только огонь и яд
			action = (unit, value1) => unit.Damage(owner, value1, effectDamageTypeMap[effectType],
				isDot: effectType == UnitEffect.Fire || effectType == UnitEffect.Poison)
		};

		periodicEffects[effectType].Add(effect);

		OnPeriodicEffectAdded(effectType, duration);
	}

	public void AddTemporaryEffect(float duration, UnitEffect effectType, System.Action onAddCallback,  System.Action onRemoveCallback, int limit = int.MaxValue) {

		if (!periodicEffects.ContainsKey(effectType)) {
			periodicEffects.Add(effectType, new List<PeriodicEffect>());
		}

		if (periodicEffects[effectType] == null) {
			periodicEffects[effectType] = new List<PeriodicEffect>();
		}

		if (periodicEffects[effectType].Count >= limit)
			return;

		PeriodicEffect effect = new PeriodicEffect {
			unit = this,
			interval = duration,
			timer = duration,
			duration = duration,
			effectType = effectType,
			action = (unit, value1) => onRemoveCallback()
		};

		onAddCallback.Invoke();

		OnPeriodicEffectAdded(effectType, duration);

		periodicEffects[effectType].Add(effect);
	}

	public bool HasEffect(UnitEffect type) {
		if (periodicEffects.ContainsKey(type) && periodicEffects[type] != null && periodicEffects[type].Count > 0)
			return true;

		return false;
	}
}
