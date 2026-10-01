using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Контроллер управления всеми кораблями на уровне.
/// Отвечает за создание, отслеживание и уничтожение игровых единиц кораблей.
/// </summary>
public class ShipsController : MonoBehaviour
{
	[SerializeField] private SpawningShip spawningShipPrefab;
	[SerializeField] private MerchantShip merchantShipPrefab;
	[SerializeField] private MerchantShipSettings merchantSettings;

	private Dictionary<int, SpawningShip> spawningShips = new Dictionary<int, SpawningShip>();
	private List<MerchantShip> merchantShips = new List<MerchantShip>();

	private Wave currentWave;
	private int wavesWithoutMerchant = 0;
	private int merchantPurchaseCount = 0;

	// События для внешних подписчиков
	public event Action<SpawningShip> OnSpawningShipArrived;
	public event Action<int> OnSpawningShipDeparted;
	public event Action<MerchantShip> OnMerchantShipArrived;
	public event Action<MerchantShip> OnMerchantShipDeparted;
	public event Action<MerchantShip> OnMerchantShipClicked;

	/// <summary>
	/// Количество покупок у торговца (для расчёта цен)
	/// </summary>
	public int MerchantPurchaseCount => merchantPurchaseCount;

	/// <summary>
	/// Настройки торговца (для доступа к waitDuration извне)
	/// </summary>
	public MerchantShipSettings MerchantSettings => merchantSettings;

	/// <summary>
	/// Инкрементить счётчик покупок
	/// </summary>
	public void IncrementPurchaseCount() {
		merchantPurchaseCount++;
	}

	/// <summary>
	/// Устанавливает текущую волну для спавна
	/// </summary>
	public void SetCurrentWave(Wave wave) {
		currentWave = wave;
	}

	/// <summary>
	/// Спавн кораблей волны для текущей волны
	/// </summary>
	public void SpawnShipsForWave(Wave wave) {
		currentWave = wave;

		foreach (var group in wave.groups) {
			if (!spawningShips.ContainsKey(group.spawnPointIdx)) {
				SpawnSpawningShip(group.spawnPointIdx);
			}
		}

		TrySpawnMerchantShip();
	}

	/// <summary>
	/// Попытка появления торговца с учётом шанса и числа волн
	/// </summary>
	private void TrySpawnMerchantShip() {
		if (merchantSettings == null) {
			Debug.LogWarning("MerchantShipSettings not assigned to ShipsController");
			return;
		}

		// Проверяем, нет ли уже активного торговца
		if (merchantShips.Count > 0) {
			return;
		}

		// Рассчитываем шанс спавна
		float spawnChance = CalculateMerchantSpawnChance();

		if (UnityEngine.Random.value <= spawnChance) {
			SpawnMerchantShipFromSettings();
			wavesWithoutMerchant = 0;
		} else {
			wavesWithoutMerchant++;
		}
	}

	/// <summary>
	/// Рассчитывает шанс появления торговца
	/// </summary>
	private float CalculateMerchantSpawnChance() {
		// Если достигнут лимит волн без торговца - 100% шанс
		if (wavesWithoutMerchant >= merchantSettings.maxWavesWithoutMerchant) {
			return 1f;
		}

		return merchantSettings.spawnChance;
	}

	/// <summary>
	/// Создаёт торговца согласно настройкам
	/// </summary>
	private void SpawnMerchantShipFromSettings() {
		int spawnIdx = merchantSettings.spawnPointIndex;
		
		if (spawnIdx < 0 || spawnIdx >= LevelDesriptor.Instance.spawnPoints.Count) {
			Debug.LogWarning($"Invalid merchant spawn point index: {spawnIdx}");
			return;
		}

		var spawnpoint = LevelDesriptor.Instance.spawnPoints[spawnIdx];
		SpawnMerchantShip(
			spawnpoint.shipTargetPoint.transform,
			spawnpoint.arrivingShipRoute,
			spawnpoint.departingShipRoute
		);
	}

	/// <summary>
	/// Спавн грузовых судов для указанного спавна
	/// </summary>
	private void SpawnSpawningShip(int spawnPointIdx) {
		var spawnpoint = LevelDesriptor.Instance.spawnPoints[spawnPointIdx];

		SpawningShip ship = Instantiate(spawningShipPrefab);
		ship.Setup(
			spawnpoint.shipTargetPoint.transform, 
			spawnpoint.arrivingShipRoute, 
			spawnpoint.departingShipRoute, 
			spawnPointIdx
		);

		ship.OnArrived += () => HandleSpawningShipArrived(ship);
		ship.OnDeparted += () => HandleSpawningShipDeparted(ship);

		spawningShips.Add(spawnPointIdx, ship);
		ship.Arrive();
	}

	/// <summary>
	/// Отправляет грузовой корабль обратно
	/// </summary>
	public void DepartSpawningShip(int spawnPointID) {
		if (spawningShips.TryGetValue(spawnPointID, out var ship)) {
			ship.Depart();
			spawningShips.Remove(spawnPointID);
		}

		OnSpawningShipDeparted?.Invoke(spawnPointID);
	}

	/// <summary>
	/// Проверяет, существует ли грузовой корабль для указанного спавна
	/// </summary>
	public bool HasSpawningShip(int spawnPointIdx) {
		return spawningShips.ContainsKey(spawnPointIdx);
	}

	/// <summary>
	/// Спавн и настройка корабля торговца
	/// </summary>
	public MerchantShip SpawnMerchantShip(Transform targetPoint, int enterPath, int exitPath) {
		MerchantShip ship = Instantiate(merchantShipPrefab);
		ship.Setup(targetPoint, enterPath, exitPath);
		ship.SetWaitDuration(merchantSettings.waitDuration);

		ship.OnMerchantArrived += HandleMerchantArrived;
		ship.OnMerchantDeparted += HandleMerchantDeparted;
		ship.OnMerchantClicked += HandleMerchantClicked;

		merchantShips.Add(ship);
		ship.Arrive();

		return ship;
	}

	/// <summary>
	/// Получить текущую волну
	/// </summary>
	public Wave GetCurrentWave() {
		return currentWave;
	}

	/// <summary>
	/// Очищает все данные и корабли (для перезагрузки уровня)
	/// </summary>
	public void Reset() {
		foreach (var ship in spawningShips.Values) {
			if (ship != null) {
				Destroy(ship.gameObject);
			}
		}

		foreach (var ship in merchantShips) {
			if (ship != null) {
				ship.OnMerchantArrived -= HandleMerchantArrived;
				ship.OnMerchantDeparted -= HandleMerchantDeparted;
				ship.OnMerchantClicked -= HandleMerchantClicked;
				Destroy(ship.gameObject);
			}
		}

		spawningShips.Clear();
		merchantShips.Clear();
		currentWave = null;
		wavesWithoutMerchant = 0;
		merchantPurchaseCount = 0;
	}

	private void HandleSpawningShipArrived(SpawningShip ship) {
		OnSpawningShipArrived?.Invoke(ship);
	}

	private void HandleSpawningShipDeparted(SpawningShip ship) {
		// Очистка идёт позже по цепочке в DepartSpawningShip
	}

	private void HandleMerchantArrived(MerchantShip ship) {
		OnMerchantShipArrived?.Invoke(ship);
	}

	private void HandleMerchantDeparted(MerchantShip ship) {
		ship.OnMerchantArrived -= HandleMerchantArrived;
		ship.OnMerchantDeparted -= HandleMerchantDeparted;
		ship.OnMerchantClicked -= HandleMerchantClicked;
		merchantShips.Remove(ship);
		OnMerchantShipDeparted?.Invoke(ship);
	}

	private void HandleMerchantClicked(MerchantShip ship) {
		OnMerchantShipClicked?.Invoke(ship);
	}
}