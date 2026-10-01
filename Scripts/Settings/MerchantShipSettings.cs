using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MerchantShipSettings", menuName = "Settings/MerchantShipSettings")]
public class MerchantShipSettings : ScriptableObject
{
	[Header("Spawn Settings")]
	[Range(0f, 1f)]
	[Tooltip("Базовый шанс появления торговца (0-1)")]
	public float spawnChance = 0.3f;

	[Min(1)]
	[Tooltip("Максимальное количество волн без торговца. После этого торговец появляется со 100% вероятностью")]
	public int maxWavesWithoutMerchant = 5;

	[Header("Route Settings")]
	[Tooltip("Индекс точки спавна для торговца")]
	public int spawnPointIndex = 0;

	[Header("Wait Settings")]
	[Tooltip("Время ожидания торговца в секундах. По истечении корабль уплывает")]
	[Min(5f)]
	public float waitDuration = 30f;
}