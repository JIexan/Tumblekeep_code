using UnityEngine;

namespace Gameplay.RoadDamage {
	/// <summary>
	/// Пресет удара снаряда о дорогу — живёт на префабе снаряда (BaseProjectileTrigger и т.п.).
	/// Глобальные лимиты плотности — в RoadDamageSettings.
	/// </summary>
	[System.Serializable]
	public class RoadImpactProfile {
		[Range(0f, 1f), Tooltip("Шанс повредить ближайшую плитку (потемнение + просадка).")]
		public float damageChance = 0.3f;
		[Range(0f, 1f), Tooltip("Шанс заспавнить камушек рядом с точкой удара.")]
		public float pebbleChance = 0f;
		[Tooltip("В каком радиусе от точки удара искать плитку, м. Нет плитки — удар мимо дороги.")]
		public float tileSearchRadius = 2f;

		public bool CanAffectRoad => damageChance > 0f || pebbleChance > 0f;
	}
}
