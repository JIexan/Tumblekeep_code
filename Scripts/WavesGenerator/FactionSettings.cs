using System.Collections.Generic;
using UnityEngine;

// Ярус юнита: открывается по номеру волны (пороги в WavesGeneratorSettings, сдвиг — на WavesGenerator уровня),
// чтобы в первой катке большие страшные юниты не шли с 5-й волны
public enum UnitTier {
	T1 = 0, // масса
	T2 = 1, // мидл
	T3 = 2, // элита
	T4 = 3, // мини-босс
}

// Стабильный идентификатор фракции для меты (анлоки через хаб, ContentUnlocksManager) и сейва —
// в профиль пишется ИМЯ значения, поэтому переименовывать нельзя; новые значения только дописывать в конец.
// Docs/factions_config.py эмитит factionId в ассеты фракций по этой же нумерации.
public enum FactionId {
	Empire = 0,
	Pirates = 1,
	Vikings = 2,
	Forest = 3,
	Undead = 4,
	Fire = 5,
}

// Фракция для генератора волн: состав, окна появления юнитов в ДОЛЯХ длительности фракции, боссы.
// Ассет общий для всех уровней; уровень (WavesGenerator в сцене) задаёт только список фракций
// и длительность каждой в волнах, порядок выбирается случайно.
[CreateAssetMenu(menuName = "Settings/FactionSettings")]
public class FactionSettings : ScriptableObject {

	[System.Serializable]
	public class UnitConfig {
		public UnitType unitType;
		public UnitTier tier = UnitTier.T1;

		[Header("Окно внутри фракции (доли её длительности)")]
		[Range(0f, 1f)] public float phaseStart = 0f;
		[Range(0f, 1f)] public float phaseEnd = 1f;

		[Header("Вес")]
		public float baseWeight = 1f;
		[Tooltip("Множитель веса по нормализованной позиции внутри окна юнита (0 — появился, 1 — окно кончается).")]
		public AnimationCurve weightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
		[Tooltip("Башня-контра: чем меньше таких башен у игрока, тем больше вес юнита.")]
		public TowerType towerWeakness = TowerType.Arrow;
		[Tooltip("Максимум штук в волне; -1 — без ограничений.")]
		public int limit = -1;
	}

	[System.Serializable]
	public class BossConfig {
		public UnitType unitType;
		[Tooltip("Босс доступен, когда открыт его ярус. Пока ни один босс не открыт, вместо него идёт «чемпион» из состава.")]
		public UnitTier tier = UnitTier.T4;
	}

	[Tooltip("Для логов и отладки.")]
	public string factionName;
	[Tooltip("Идентификатор для меты: фракция участвует в волнах, только если открыта (ContentUnlocksManager).")]
	public FactionId factionId;
	public List<UnitConfig> units = new List<UnitConfig>();
	[Tooltip("Боссы фракции для босс-волн по ярусам. Пусто или всё закрыто — чемпион: самый высокий открытый ярус состава.")]
	public List<BossConfig> bosses = new List<BossConfig>();

	public string DisplayName => string.IsNullOrEmpty(factionName) ? name : factionName;
}
