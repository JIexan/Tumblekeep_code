using UnityEngine;
using System.Collections.Generic;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Flags]
public enum TowerType {
	Placeholder = -1,
	Laser		= (1 << 0),
	Arrow		= (1 << 1),
	Mortar		= (1 << 2),
	Electric	= (1 << 3),
	Flamethrower= (1 << 4)
}

public enum TowerTraitType {
	None = -1,
	Heavy,
	Double,
	Fire,
	Poison,
	SuperDamage,
	SuperSlow,
}

public enum Direction {
	Forward,
	Left,
	Right,
	Back,
}

public class BaseTower : MonoBehaviour, IUpgradable
{
	private const int BaseMaxTraits = 3;

	// «Шлем зодчего» может добавить слот трейта поверх базовых трёх
	public int MaxTraits => BaseMaxTraits + Mathf.RoundToInt(Gameplay.Relics.RelicsManager.GetFlat(Gameplay.Relics.RelicValue.TowerTraitSlots));

	//переменные которые берутся из настроек башен
	private string id;
	public Direction towerDirection;
	public Direction weaponDirection;	
	[SerializeField] protected TowerExperince exp;
	[SerializeField] protected CharacteristicsCollection characteristics = new CharacteristicsCollection();
	[SerializeField] protected TowerInfoCanvas infoPanel;
	[SerializeField] protected BaseTowerWeapon weapon;
	[SerializeField] protected List<TowerTraitType> possibleTraits = new List<TowerTraitType>();
	[SerializeField] protected DamageType damageType = DamageType.Physical;

	[Header("Visuals")]
	[SerializeField] private TowerFoundation[] foundations;
	[SerializeField] private BaseTowerWeapon[] weapons;
	
	protected List<AbstractTrait> traits = new List<AbstractTrait>();
	private float sharedXPRemainder; // дробный остаток опыта за чужие киллы (OnSharedKillXP)

	public TowerType towerType;

	public ParticleSystem placeEffect;

	[HideInInspector] public TowerUpgradeSettings.AbilitySettings[] abilities;

	public string Id { get => id; }
	public int Level => exp.CurrentLevel;
	public bool CanUpgrade => exp.CanLevelUp();
	public int TraitsCount => traits.Count;
	public virtual float DPS => weapon.DPS;

	public DamageType DamageType => damageType;

	// У Laser/Mortar weapons[1] ссылается на тот же объект, что и weapons[0] —
	// Double-трейт для них не даёт второго ствола и должен компенсировать иначе.
	public bool HasDistinctSecondWeapon => weapons != null && weapons.Length > 1 && weapons[1] != null && weapons[1] != weapons[0];

	public float Radius => GetCharacteristicValue(CharacteristicType.Radius);

	public CharacteristicsCollection Characteristics => characteristics;
	public IReadOnlyList<TowerTraitType> PossibleTraits => possibleTraits;

	public Action OnUpdateState { get; set; }

	private static readonly ITraitFactory _traitFactory = new TraitsFactory();

	public float GetCharacteristicValue(CharacteristicType type) {
		// Множители реликвий (урон/радиус) поверх кламп-логики Characteristic — осознанно:
		// бонус катки не должен упираться в MaxValue из CharacteristicSettings
		float value = characteristics.GetValue(type) * Gameplay.Relics.RelicsManager.GetTowerCharacteristicFactor(type);
		// SpellSpeedUp: временный +% к Speed всех башен (и построенных во время эффекта тоже)
		if (type == CharacteristicType.Speed)
			value *= SpellsManager.TowerSpeedFactor;
		return value;
	}

	public void AddCharacteristic(CharacteristicType type) {
		characteristics.Add(type, true);
	}

	public bool HasTrait(TowerTraitType type) {
		for (int i = 0; i < traits.Count; i++) {
			if (traits[i].Type == type) 
				return true;
		}
		return false;
	}

	private void Awake() {
		if (exp != null) exp.OnUpgradeReady += PlayUpgradeReady;
		if (weapon != null) {
			weapon.Construct(this);
		}
	}

	private void PlayUpgradeReady() {
		if (isActiveAndEnabled) GameAudio.Play(GameSound.TowerUpgradeReady, transform.position);
	}

	private void OnDestroy() {
		if (exp != null) exp.OnUpgradeReady -= PlayUpgradeReady;
	}

	protected virtual void Start() {
		UpdateState();
		if (placeEffect != null)
			placeEffect.Play();
	}

	public void Upgrade(TowerCharacteristicUpgradeData data) {
		characteristics.Upgrade(data);
		exp.LevelUp();
		UpdateState();
	}

	public void Upgrade(TowerTraitUpgradeData data) {
		var trait = _traitFactory.Create(this, data.trait);
		trait.Install();
		traits.Add(trait);
		exp.LevelUp();
		UpdateState();
	}

	protected virtual void UpdateState() {
		//обновить визуал и все остальное у башни. Поидеи у каждой башни индивидуально
		infoPanel.UpdateState(Level, exp.GetProgressToNextLevel());
		// Кламп: с 4-м трейтом («Шлем зодчего») остаёмся на последнем виде фундамента,
		// иначе индекс уходит за массив и гаснут все фундаменты
		int foundationIndex = Mathf.Min(traits.Count, foundations.Length - 1);
		for (int i = 0; i < foundations.Length; i++) {
			if(i == foundationIndex) {
				foundations[i].gameObject.SetActive(true);
				foundations[i].Init(infoPanel, traits);
			} else {
				foundations[i].gameObject.SetActive(false);
			}
		}

		if (HasTrait(TowerTraitType.Double) && weapon != weapons[1]) {
			InstallNewWeapon(weapons[1]);
		} else if(!HasTrait(TowerTraitType.Double) && weapon != weapons[0]) {
			InstallNewWeapon(weapons[0]);
		}

		weapon.UpdateState();
		OnUpdateState?.Invoke();
	}

	// Витринное состояние для превью в книге (инстанс с выключенными MonoBehaviour, Start не идёт):
	// базовый фундамент, базовое оружие на его оффсете, без инфо-панели и эффекта постройки.
	public void SetupPreview() {
		if (infoPanel != null)
			infoPanel.gameObject.SetActive(false);
		if (placeEffect != null)
			placeEffect.gameObject.SetActive(false);

		if (foundations != null) {
			for (int i = 0; i < foundations.Length; i++) {
				if (foundations[i] != null)
					foundations[i].gameObject.SetActive(i == 0);
			}
		}

		if (weapons != null && weapons.Length > 0 && weapons[0] != null) {
			for (int i = 0; i < weapons.Length; i++) {
				if (weapons[i] != null)
					weapons[i].gameObject.SetActive(weapons[i] == weapons[0]);
			}
			weapon = weapons[0];
			if (foundations != null && foundations.Length > 0 && foundations[0] != null)
				weapon.ApplyOffset(foundations[0].WeaponOffset);
		}
	}

	private void InstallNewWeapon(BaseTowerWeapon newWeapon) {
		// Deactivate old weapon if present
		if (weapon != null && weapon.gameObject != null) {
			weapon.gameObject.SetActive(false);
		}

		if (newWeapon == null)
			return;

		// Place new weapon at WeaponOffset of the current active foundation (indexed by traits.Count)
		if (foundations != null && foundations.Length > 0) {
			int foundationIndex = traits.Count;
			foundationIndex = Mathf.Clamp(foundationIndex, 0, foundations.Length - 1);
			var foundation = foundations[foundationIndex];
			if (foundation != null) {
				newWeapon.ApplyOffset(foundation.WeaponOffset);
			}
		}

		// Keep rotation from previous weapon (after positioning)
		newWeapon.UpdateRotationFromOtherWeapon(weapon);

		weapon = newWeapon;
		weapon.gameObject.SetActive(true);
		weapon.Construct(this);
	}

#if UNITY_EDITOR
	public void OnDrawGizmos() {
		Handles.color = Color.yellow;
		Handles.ArrowHandleCap(0, transform.position + Vector3.up * 2, GetRotaion(towerDirection), 3f, EventType.Repaint);

		Handles.color = Color.magenta;
		Handles.ArrowHandleCap(0, transform.position + Vector3.up * 4, GetRotaion(weaponDirection), 3f, EventType.Repaint);
	}

	private Quaternion GetRotaion(Direction dir) {
		Quaternion rotation = Quaternion.identity;
		switch (dir) {
			case Direction.Forward:
				rotation = Quaternion.LookRotation(transform.forward);
				break;
			case Direction.Left:
				rotation = Quaternion.LookRotation(transform.right * -1);
				break;
			case Direction.Right:
				rotation = Quaternion.LookRotation(transform.right);
				break;
			case Direction.Back:
				rotation = Quaternion.LookRotation(transform.forward * -1);
				break;
		}

		return rotation;
	}
#endif

	public void UpdateDirections(Vector3 baseDirection, Vector3 weaponDirection) {
		transform.forward = baseDirection;
		weapon.transform.forward = weaponDirection;
	}

	public Vector3 GetWeaponDirection() {
		return GetDirectionVector(weaponDirection);
	}

	public Vector3 GetDirectionVector(Direction dir) {
		switch (dir) {
			case Direction.Forward:
				return transform.forward;
			case Direction.Left:
				return transform.right * -1f;
			case Direction.Right:
				return transform.right;
			case Direction.Back:
				return transform.forward * -1f;
		}

		return transform.forward;
	}

	public List<TowerTraitType> GetPossibleTraits() {
		List<TowerTraitType> res = new List<TowerTraitType>(this.possibleTraits);

		// Апгрейды уровня расширяют пул (ExtraTraitsEffect); единая точка входа —
		// TowerUpgradeGenerator и остальные потребители получают расширение автоматически.
		foreach (var extra in Gameplay.LevelUpgrades.LevelUpgradesManager.GetExtraTraits(towerType)) {
			if (!res.Contains(extra))
				res.Add(extra);
		}

		for (int i = 0; i < traits.Count; i++) {
			res.Remove(traits[i].Type);
		}
		return res;
	}

	public Characteristic GetRandomNotMaxedCharacteristic(List<CharacteristicType> exceptions) {
		return characteristics.GetRandomNotMaxed(exceptions);
	}

	public void OnUnitKill(Unit u, bool assist) {
		int xpAmount = Mathf.Max(Mathf.RoundToInt(GetKillXP(u, assist ? 0.5f : 1f)), 1);
		exp.AddXP(xpAmount);
		infoPanel.UpdateState(Level, exp.GetProgressToNextLevel());
		FloatingTextManager.Instance?.ShowXP(xpAmount, transform.position + new Vector3(0f, 5f, 0f));
	}

#if CHEATS
	public void CheatAddXP(int amount) {
		if (exp == null || amount <= 0)
			return;
		// Repeated clicks must not wrap either XP counter around int.MaxValue.
		amount = Mathf.Min(amount, int.MaxValue - Mathf.Max(exp.CurrentXP, exp.TotalXP));
		exp.AddXP(amount);
		if (infoPanel != null)
			infoPanel.UpdateState(Level, exp.GetProgressToNextLevel());
		OnUpdateState?.Invoke();
	}
#endif

	// Доля опыта за чужой килл (RelicValue.TowerSharedXP). Копится дробно: округление вверх до 1, как у своих киллов,
	// превратило бы 25% в 100% на дешёвых юнитах. Без всплывающего текста — иначе «+XP» над каждой башней на каждый килл
	public void OnSharedKillXP(Unit u, float share) {
		sharedXPRemainder += GetKillXP(u, share);
		int xpAmount = Mathf.FloorToInt(sharedXPRemainder);
		if (xpAmount <= 0)
			return;
		sharedXPRemainder -= xpAmount;
		exp.AddXP(xpAmount);
		infoPanel.UpdateState(Level, exp.GetProgressToNextLevel());
	}

	// Опыт башне растёт с множителем юнита мягче монет (корень), чтобы не разгонять уровни на поздних волнах
	private float GetKillXP(Unit u, float factor) {
		return u.exp * Mathf.Sqrt(Mathf.Max(u.RewardMultiplier, 0f)) * (1f + Castle.Instance.GetCharacteristicValue(CharacteristicType.ExpGain)) * factor
			* Gameplay.Relics.RelicsManager.GetFactor(Gameplay.Relics.RelicValue.TowerXPGain);
	}

	public void ApplyTraitsOnProjectileDamage(Unit u) {
		for (int i = 0; i < traits.Count; i++) {
			traits[i].DamageUnitWithProjectile(u);
		}
	}

	// Сплеш (мортира, электро-разряд по площади)
	public void ApplyTraitsOnAreaDamage(Unit u) {
		for (int i = 0; i < traits.Count; i++) {
			traits[i].DamageUnitInArea(u);
		}
	}

	// Лучевые башни (лазер, электро) бьют каждый кадр — трейты вешаем на цель
	// не чаще StatusEffectsSettings.continuousTraitInterval
	private readonly Dictionary<Unit, float> lastContinuousTraitTime = new Dictionary<Unit, float>();

	public void ApplyTraitsOnContinuousDamage(Unit u) {
		if (u == null || traits.Count == 0)
			return;
		var settings = GameConfigs.Instance.StatusEffectsSettings;
		float interval = settings != null ? settings.continuousTraitInterval : 1f;
		if (lastContinuousTraitTime.TryGetValue(u, out float last) && Time.time - last < interval)
			return;
		lastContinuousTraitTime[u] = Time.time;
		if (lastContinuousTraitTime.Count > 64)
			PruneContinuousTraitTargets();
		ApplyTraitsOnProjectileDamage(u);
	}

	private void PruneContinuousTraitTargets() {
		var stale = new List<Unit>();
		foreach (var kv in lastContinuousTraitTime) {
			if (kv.Key == null || kv.Key.isDead)
				stale.Add(kv.Key);
		}
		foreach (var u in stale)
			lastContinuousTraitTime.Remove(u);
	}

	public List<UpgradeData> GetUpgradeOptions() {
		return GameController.Instance.GenerateUpgradeOptions(this);
	}
}
