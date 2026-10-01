using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Настройки системы птиц-боидов. Ассет лежит в Assets/Settings/BirdsSettings.asset и подключён в GameConfigs.
/// </summary>
[CreateAssetMenu(fileName = "BirdsSettings", menuName = "Configs/BirdsSettings")]
public class BirdsSettings : ScriptableObject
{
	[Header("Префабы")]
	public GameObject sittingPrefab;
	public GameObject flyingPrefab;
	[Tooltip("У летящей модели ориджин в центре, у сидящей — в лапах: вертикальная поправка при свапе")]
	public float flyingPivotYOffset = 0.24f;
	[Tooltip("Диаметр тушки — ограничивает прыжки внутри ячейки слота")]
	public float birdBodyDiameter = 0.3f;

	[Header("Виды птиц")]
	[Tooltip("Виды по умолчанию с весами — для зон, у которых свой список видов пуст. Пусто — материал префаба как есть")]
	public List<BirdSpeciesWeight> species = new List<BirdSpeciesWeight>();

	[Header("Слоты")]
	public float slotSpacing = BirdPerchZone.DefaultSlotSpacing;

	[Header("Прыжки сидящих")]
	public float hopIntervalMin = 3f;
	public float hopIntervalMax = 9f;
	public float hopDuration = 0.25f;
	public float hopHeight = 0.08f;
	[Range(0f, 1f), Tooltip("Доля срабатываний, когда птица только оглядывается (без прыжка)")]
	public float rotateOnlyChance = 0.5f;

	[Header("Свап моделей")]
	public float swapDuration = 0.15f;

	[Header("Полёт: скорости")]
	public float cruiseSpeed = 6f;
	public float minSpeed = 3f;
	public float maxSpeed = 9f;
	[Tooltip("Разрешённый максимум скорости сразу после испуга")]
	public float scareBoostSpeed = 11f;
	public float scareBoostDuration = 1.5f;
	public float maxSteerAccel = 15f;
	public float maxClimbSpeed = 3f;
	public float maxSinkSpeed = 4.5f;

	[Header("Полёт: высоты")]
	public float cruiseAltitudeMin = 6f;
	public float cruiseAltitudeMax = 12f;
	public float minAltitude = 2.5f;
	[Tooltip("Сила выталкивания вверх ниже минимальной высоты")]
	public float groundAvoidWeight = 3f;

	[Header("Боиды: силы")]
	public float separationRadius = 1.2f;
	public float separationWeight = 2f;
	public float alignmentRadius = 4f;
	public float alignmentWeight = 1f;
	public float cohesionRadius = 6f;
	public float cohesionWeight = 0.8f;
	public float seekWeight = 1.5f;
	public float wanderWeight = 0.3f;
	[Tooltip("Скорость аттрактора относительно cruiseSpeed (меньше 1 — птицы завихряются вокруг)")]
	public float attractorSpeedFactor = 0.85f;

	[Header("Крен")]
	public float bankFactor = 0.35f;
	public float maxBankAngle = 45f;
	public float rollSmoothing = 8f;

	[Header("Взлёт")]
	public float takeoffStaggerMax = 0.25f;
	public float takeoffUpSpeed = 2.5f;
	public float takeoffAwaySpeed = 2f;

	[Header("Посадка")]
	[Tooltip("Дистанция центроида стаи до зоны, с которой птиц по одной отпускают на посадку")]
	public float descendReleaseDistance = 7f;
	public float landingStaggerMin = 0.1f;
	public float landingStaggerMax = 0.3f;
	[Tooltip("Точка захода: высота над слотом")]
	public float hoverHeight = 1f;
	public float approachSlowRadius = 8f;
	public float landingMinSpeed = 1.5f;
	[Tooltip("Условия перехода в тачдаун-твин: ближе этой дистанции к точке захода...")]
	public float touchdownDistance = 0.8f;
	[Tooltip("...и медленнее этой скорости")]
	public float touchdownMaxSpeed = 2.5f;
	public float touchdownDuration = 0.35f;

	[Header("Испуг")]
	public float shotScareRadius = 6f;
	public float unitScareDistance = 4f;
	public float unitPollInterval = 0.25f;
	[Tooltip("Зона снова пригодна для посадки спустя столько секунд после испуга")]
	public float calmDelay = 12f;
	[Tooltip("Единица «жара» зоны спадает за столько секунд")]
	public float heatDecayInterval = 30f;
	[Tooltip("При таком жаре зона забрасывается")]
	public int heatAbandonThreshold = 4;
	public float abandonDuration = 120f;
	[Tooltip("Стая игнорирует испуги столько секунд после взлёта")]
	public float flockScareImmunity = 3f;

	[Header("Кружение / улёт / возврат")]
	public float loiterRadius = 12f;
	[Tooltip("Сколько стая кружит без безопасной зоны, прежде чем улететь с карты")]
	public float loiterTimeout = 20f;
	[Tooltip("Дистанция от точки улёта, на которой стая деспавнится")]
	public float leaveKillDistance = 60f;
	public float returnDelayMin = 60f;
	public float returnDelayMax = 120f;
	public int returnFlockSizeMin = 4;
	public int returnFlockSizeMax = 8;
}
