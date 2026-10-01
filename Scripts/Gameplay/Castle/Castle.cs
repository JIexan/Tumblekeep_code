using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Castle : Singleton<Castle>, IUpgradable
{
	private float HP;
	private float totalHP;
	[SerializeField] float armorReductionConstant = 50;
	[SerializeField] int characteristicsLimit = 3;
	[SerializeField] private CharacteristicsCollection characteristics;
	[SerializeField] private CastleCrossbow crossbow;
	[SerializeField] bool isMenuCastle = false;
	[SerializeField] List<CharacteristicType> possibleCharacteristics = new List<CharacteristicType>();
	[SerializeField] private Vector3 floatingTextOffset = new Vector3(0f, 5f, 0f); // якорь всплывающего текста урона над замком

	public List<ParticleSystem> damages;

	public float NormalizedHP {
		get { return (float)HP / (float)totalHP; }
	}

	public float CurrentHP => HP;
	public float MaxHP => totalHP;

	public CastleCrossbow Crossbow => crossbow;

	// The built-in weapon stats do not consume the castle's defensive/economy slots.
	public bool CanGetNewCharacteristic => characteristics.Count -
		(crossbow != null && characteristics.Contains(CharacteristicType.Speed) ? 1 : 0) -
		(crossbow != null && characteristics.Contains(CharacteristicType.ProjectileDamage) ? 1 : 0) < characteristicsLimit;

	public List<CharacteristicType> GetAvailableUpgradeTypes() {
		var result = new List<CharacteristicType>();
		foreach (var entry in characteristics) {
			if (entry.Value.IsMaxed) // Existing API: IsMaxed means value is BELOW the maximum.
				result.Add(entry.Key);
		}
		if (CanGetNewCharacteristic) {
			foreach (var type in possibleCharacteristics) {
				if (!characteristics.Contains(type) && !result.Contains(type))
					result.Add(type);
			}
		}
		return result;
	}

	public CharacteristicsCollection Characteristics => characteristics;

	public void Start() {
        if (isMenuCastle) return;
		totalHP = LevelDesriptor.Instance.castleHP;
		HP = totalHP;
	}

#if CHEATS
	// Чит «бессмертный замок»: урон игнорируется целиком (в том числе чит Defeat)
	public bool Invulnerable { get; set; }
#endif

	public void Damage(float amount) {
#if CHEATS
		if (Invulnerable)
			return;
#endif
		// Резист через GetCharacteristicValue — там же плоские бонусы реликвий («Подорожник»)
		float damagePersent = armorReductionConstant / (GetCharacteristicValue(CharacteristicType.Resistance) + armorReductionConstant);
		float dealt = amount * damagePersent;
		HP -= dealt;

		// Показ до ветки проигрыша — смертельный удар тоже виден
		if (!isMenuCastle && dealt > 0.05f) {
			FloatingTextManager.Instance?.ShowCastleDamage(dealt, transform.position + floatingTextOffset);
		}

		GameController.Instance.uiController.UpdateCastleHealthBar(HP, NormalizedHP);
		if (HP <= 0) {
			// «Перо феникса»: раз за катку смертельный удар гасится, замок восстаёт из пепла
			if (Gameplay.Relics.RelicsManager.TryConsumeSecondLife(out float restorePercent)) {
				HP = totalHP * restorePercent;
				GameController.Instance.uiController.UpdateCastleHealthBar(HP, NormalizedHP);
			} else {
				GameController.Instance.LoseGame();
				return;
			}
		}
		UpdateDamages();
	}

	public void Heal(float amount) {
		if (isMenuCastle || totalHP <= 0f || amount <= 0f)
			return;
		HP = Mathf.Min(HP + amount, totalHP);
		GameController.Instance.uiController.UpdateCastleHealthBar(HP, NormalizedHP);
	}

	/// <summary>Лечение на процент от максимального HP (спелл SpellHeal: value1 = %)</summary>
	public void HealPercent(float percent) {
		Heal(totalHP * percent / 100f);
	}

	private void UpdateDamages() {
		float step = 1f / (damages.Count + 1);
		float hpStep = (float)HP / (float)totalHP;
		for (int i = 0; i < damages.Count; i++) {
			if(hpStep < (i + 1) * step && !damages[i].isPlaying) {
				damages[i].Play();
			}
		}
	}

	public void Upgrade(CharacteristicUpgradeData data) {
		if(!characteristics.Contains(data.characteristicType)) {
			characteristics.Add(data.characteristicType, true);
		}
		characteristics.Upgrade(data);
	}

	public float GetCharacteristicValue(CharacteristicType type) {
		float value = characteristics.GetValue(type);
		// Плоские бонусы реликвий (Luck/Resistance/Regeneration); меню-замок статики не читает
		if (!isMenuCastle) {
			value += Gameplay.Relics.RelicsManager.GetCastleFlat(type);
			// Временный резист от спелла «Защита» (SpellProtect)
			if (type == CharacteristicType.Resistance)
				value += SpellsManager.CastleResistanceBonus;
		}
		return value;
	}

	public Characteristic GetRandomNotMaxedCharacteristic(List<CharacteristicType> exceptions) {
		return characteristics.GetRandomNotMaxed(exceptions);
	}

	public CharacteristicType GetRandomNewCharacteristic() {
		var possible = new List<CharacteristicType>(possibleCharacteristics);
		characteristics.FilterTypes(possible);
		return possible[Random.Range(0, possible.Count)];
	}

	private void Update() {
		if (HP < totalHP) {
			// Без гейта hasRegeneration: реген могут давать и реликвии («Гномья замазка»)
			// без прокачанной характеристики. Меню-замок безопасен: HP == totalHP == 0.
			float regeneration = GetCharacteristicValue(CharacteristicType.Regeneration);
			if (regeneration > 0f) {
				HP += (regeneration / 60f) * Time.deltaTime;
				if (HP >= totalHP) {
					HP = totalHP;
				}
				GameController.Instance.uiController.UpdateCastleHealthBar(HP, NormalizedHP);
			}
		}
	}

	public List<UpgradeData> GetUpgradeOptions() {
		return CastleUpgradeGenerator.GenerateUpgradeOptions();
	}
}
