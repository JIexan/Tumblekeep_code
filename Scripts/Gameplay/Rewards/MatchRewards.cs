using UnityEngine;

/// <summary>
/// Накопитель наград за катку (island XP + кристаллы).
/// Награды копятся с убийств и выплачиваются в профиль только при победе (банк).
/// Живёт как поле GameController — умирает вместе со сценой.
/// </summary>
public class MatchRewards
{
	public int AccruedXP { get; private set; }
	public int AccruedCrystals { get; private set; }
	public int BankedXP { get; private set; }
	public int BankedCrystals { get; private set; }
	public bool HasBanked => BankedXP > 0 || BankedCrystals > 0;

	/// <summary>
	/// Кристаллы из сундуков этой катки. Они уже лежат в профиле (сундук платит сразу, не через банк)
	/// и здесь учитываются только для HUD-счётчика «заработано за катку».
	/// </summary>
	public int ChestCrystals { get; private set; }

	/// <summary>Заработано кристаллов за катку (убийства + сундуки) — значение HUD-счётчика, без множителя.</summary>
	public int MatchCrystals => AccruedCrystals + ChestCrystals;

	public void AddChestCrystals(int amount) {
		ChestCrystals += Mathf.Max(0, amount);
	}

	/// <summary>
	/// Множитель результата катки по числу выполненных ДОПОЛНИТЕЛЬНЫХ тасков.
	/// TODO: вынести значения в настройки, если понадобится баланс.
	/// </summary>
	public static float GetMultiplier(int completedSecondaryCount) {
		switch (completedSecondaryCount) {
			case 0: return 1f;
			case 1: return 1.5f;
			default: return 2f;
		}
	}

	/// <summary>
	/// Начисляет награды за убийство юнита. Возвращает кол-во выпавших кристаллов (для FX).
	/// </summary>
	public int AccrueForKill(ItemUnitPrice entry, float multiplier = 1f) {
		// Нулевой множитель — юнит без наград вовсе (чит-парад): кристаллы с него тоже не падают
		if (entry == null || multiplier <= 0f)
			return 0;

		// XP острова масштабируется множителем юнита (фракция/кап тел); кристаллы — нет, это мета-валюта
		AccruedXP += Mathf.Max(0, Mathf.RoundToInt(entry.exp * multiplier));

		float crystalChance = entry.crystalChance * Gameplay.Relics.RelicsManager.GetFactor(Gameplay.Relics.RelicValue.CrystalChance);
		if (entry.crystals > 0 && crystalChance > 0f && Random.value < crystalChance) {
			AccruedCrystals += entry.crystals;
			return entry.crystals;
		}

		return 0;
	}

	/// <summary>
	/// Фиксирует накопленное с множителем. Возвращает дельту к выплате
	/// (итог = накоп × множитель минус уже выплаченное; не бывает отрицательной).
	/// </summary>
	public (int xp, int crystals) Bank(float multiplier) {
		int totalXP = Mathf.RoundToInt(AccruedXP * multiplier);
		int totalCrystals = Mathf.RoundToInt(AccruedCrystals * multiplier);
		int deltaXP = Mathf.Max(0, totalXP - BankedXP);
		int deltaCrystals = Mathf.Max(0, totalCrystals - BankedCrystals);
		BankedXP += deltaXP;
		BankedCrystals += deltaCrystals;
		return (deltaXP, deltaCrystals);
	}
}
