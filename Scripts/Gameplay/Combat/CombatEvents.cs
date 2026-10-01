using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Combat {
	// Один фактический урон по юниту (после резистов и реликвий)
	public struct DamageInfo {
		public Unit unit;
		public BaseTower owner;     // null — свитки, реликвии, способности без башни
		public DamageType type;
		public float amount;        // реально снятое ХП (оверкилл отрезан)
		public float rawAmount;     // урон попадания целиком, как во всплывающей цифре
		public float hpBefore;
		public bool lethal;
		public bool fromFullHp;
		public bool isDot;          // тик огня/яда/электро-эффекта, а не прямое попадание
	}

	// Итог одного удара башни: снаряд по цели или взрыв по площади
	public struct HitInfo {
		public BaseTower owner;
		public int unitsDamaged;
		public int unitsKilled;
		public float totalDamage;
	}

	// Боевые события катки для тасков уровня и автоплея. Типизированные C#-события вместо
	// InGameEventManager: лазер и DoT бьют каждый кадр, object-payload давал бы боксинг на каждый тик.
	// Состояние катки (паттерн RelicsManager): ResetRun из GameController.Awake/ResetLevel
	// снимает всех подписчиков — статические события не текут между сценами.
	public static class CombatEvents {
		public static event Action<DamageInfo> DamageDealt;
		public static event Action<HitInfo> HitResolved;

		private struct SlowEntry {
			public int refs;
			public float since;
		}

		private static readonly Dictionary<Unit, SlowEntry> slowedUnits = new Dictionary<Unit, SlowEntry>();
		private static readonly List<Unit> slowedCleanupBuffer = new List<Unit>();

		private static int hitDepth;
		private static HitInfo currentHit;

		public static void ResetRun() {
			DamageDealt = null;
			HitResolved = null;
			slowedUnits.Clear();
			hitDepth = 0;
			currentHit = default;
		}

		// ---- Урон ----

		// Зовёт Unit после списания ХП и до Kill()
		public static void ReportDamage(Unit unit, BaseTower owner, DamageType type, float rawAmount, float hpBefore, float hpAfter, float totalHP, bool isDot) {
			bool lethal = hpAfter <= 0f;
			float amount = hpBefore - Mathf.Max(hpAfter, 0f);

			// В удар идут только прямые попадания его башни: взрыв реликвии по соседям,
			// сработавший внутри скоупа от убийства, к удару не относится
			if (hitDepth > 0 && !isDot && owner != null && owner == currentHit.owner) {
				currentHit.unitsDamaged++;
				currentHit.totalDamage += amount;
				if (lethal)
					currentHit.unitsKilled++;
			}

			DamageDealt?.Invoke(new DamageInfo {
				unit = unit,
				owner = owner,
				type = type,
				amount = amount,
				rawAmount = rawAmount,
				hpBefore = hpBefore,
				lethal = lethal,
				fromFullHp = hpBefore >= totalHP,
				isDot = isDot,
			});
		}

		// ---- Удар башни ----

		public readonly struct HitScope : IDisposable {
			// Урон придёт позже (AltElectric вешает отложенный электро-эффект): задетых считаем по списку целей
			public void ReportTargets(int count) {
				if (hitDepth == 1)
					currentHit.unitsDamaged += count;
			}

			public void Dispose() {
				EndHit();
			}
		}

		// using (CombatEvents.BeginHit(tower)) { ...u.Damage(tower, ...)... }
		public static HitScope BeginHit(BaseTower owner) {
			hitDepth++;
			if (hitDepth == 1)
				currentHit = new HitInfo { owner = owner };
			return default;
		}

		private static void EndHit() {
			if (hitDepth <= 0)
				return;
			hitDepth--;
			if (hitDepth > 0)
				return;

			var hit = currentHit;
			currentHit = default;
			if (hit.unitsDamaged > 0)
				HitResolved?.Invoke(hit);
		}

		// ---- Замедление электро-башен ----

		// Парные вызовы из ApplyBuff/TakeOffBuff электро-башен; счётчик ссылок — ауры башен перекрываются
		public static void NotifyElectricSlow(Unit unit, bool applied) {
			if (unit == null)
				return;

			slowedUnits.TryGetValue(unit, out var entry);
			if (applied) {
				if (entry.refs == 0)
					entry.since = Time.time;
				entry.refs++;
				slowedUnits[unit] = entry;
			} else if (entry.refs > 0) {
				entry.refs--;
				if (entry.refs == 0)
					slowedUnits.Remove(unit);
				else
					slowedUnits[unit] = entry;
			}
		}

		public static int SlowedCount {
			get {
				PurgeDeadSlowed();
				return slowedUnits.Count;
			}
		}

		// Сколько секунд подряд держится под замедлением самый «долгий» из живых юнитов
		public static float LongestCurrentSlow {
			get {
				PurgeDeadSlowed();
				float now = Time.time;
				float longest = 0f;
				foreach (var pair in slowedUnits)
					longest = Mathf.Max(longest, now - pair.Value.since);
				return longest;
			}
		}

		// Юнит, умерший внутри ауры, OnTriggerExit не получает — TakeOffBuff для него не придёт
		private static void PurgeDeadSlowed() {
			slowedCleanupBuffer.Clear();
			foreach (var pair in slowedUnits) {
				if (pair.Key == null || pair.Key.isDead)
					slowedCleanupBuffer.Add(pair.Key);
			}
			foreach (var unit in slowedCleanupBuffer)
				slowedUnits.Remove(unit);
		}
	}
}
