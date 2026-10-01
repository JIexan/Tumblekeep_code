using Gameplay.Combat;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.TasksSystem.Probes {
	// Общее правило зачёта: рекорды и ваншоты — только прямые попадания башен (owner != null, не DoT);
	// свитки и реликвии (owner == null) идут только в накопительный урон по типу.

	public abstract class DamageProbe : TaskProbe {
		protected override void OnStart() {
			CombatEvents.DamageDealt += OnDamage;
		}

		protected override void OnStop() {
			CombatEvents.DamageDealt -= OnDamage;
		}

		protected abstract void OnDamage(DamageInfo info);

		protected static bool IsTowerHit(in DamageInfo info) {
			return info.owner != null && !info.isDot;
		}
	}

	// Убийства с первого удара: юнит был на полном ХП и умер от одного попадания
	public class OneShotKillsProbe : DamageProbe {
		protected override void OnDamage(DamageInfo info) {
			if (IsTowerHit(info) && info.lethal && info.fromFullHp)
				Add(1f);
		}
	}

	// Самое большое одиночное попадание (цифра урона целиком, оверкилл не режем)
	public class BiggestHitProbe : DamageProbe {
		protected override void OnDamage(DamageInfo info) {
			if (IsTowerHit(info))
				RaiseTo(Mathf.FloorToInt(info.rawAmount));
		}
	}

	// Суммарный урон одного типа (огонь/яд) из любых источников
	public class DamageByTypeProbe : DamageProbe {
		private readonly DamageType damageType;

		public DamageByTypeProbe(DamageType damageType) {
			this.damageType = damageType;
		}

		protected override void OnDamage(DamageInfo info) {
			if (info.type == damageType)
				Add(info.amount);
		}
	}

	// Рекорд числа РАЗНЫХ юнитов, по которым шло событие урона за последние window секунд
	public abstract class RecentUnitsProbe : DamageProbe {
		private readonly float window;
		private readonly Dictionary<Unit, float> lastSeen = new Dictionary<Unit, float>();
		private readonly List<Unit> expired = new List<Unit>();

		protected RecentUnitsProbe(float window) {
			this.window = window;
		}

		protected abstract bool Counts(in DamageInfo info);

		protected override void OnDamage(DamageInfo info) {
			if (Counts(info))
				lastSeen[info.unit] = Time.time;
		}

		protected override void OnTick(float now) {
			expired.Clear();
			int count = 0;
			foreach (var pair in lastSeen) {
				if (pair.Key == null || now - pair.Value > window)
					expired.Add(pair.Key);
				else if (!pair.Key.isDead)
					count++;
			}
			foreach (var unit in expired)
				lastSeen.Remove(unit);
			RaiseTo(count);
		}

		protected override void OnStop() {
			base.OnStop();
			lastSeen.Clear();
		}
	}

	// Задеть X разных врагов за Y секунд (темп: скорость атаки, АОЕ, число башен)
	public class DamagedInWindowProbe : RecentUnitsProbe {
		public DamagedInWindowProbe(float windowSeconds) : base(windowSeconds) { }

		protected override bool Counts(in DamageInfo info) {
			return IsTowerHit(info);
		}
	}

	// X врагов одновременно горят/отравлены: юнит «под эффектом», пока тики DoT этого типа от башен идут не реже интервала
	public class AffectedSimultaneouslyProbe : RecentUnitsProbe {
		private readonly DamageType damageType;

		public AffectedSimultaneouslyProbe(DamageType damageType, float dotTickInterval) : base(dotTickInterval * 1.1f) {
			this.damageType = damageType;
		}

		protected override bool Counts(in DamageInfo info) {
			return info.isDot && info.owner != null && info.type == damageType;
		}
	}

	public abstract class HitProbe : TaskProbe {
		protected override void OnStart() {
			CombatEvents.HitResolved += OnHit;
		}

		protected override void OnStop() {
			CombatEvents.HitResolved -= OnHit;
		}

		protected abstract void OnHit(HitInfo hit);
	}

	public class MultiHitProbe : HitProbe {
		protected override void OnHit(HitInfo hit) {
			RaiseTo(hit.unitsDamaged);
		}
	}

	public class MultiKillProbe : HitProbe {
		protected override void OnHit(HitInfo hit) {
			RaiseTo(hit.unitsKilled);
		}
	}

	public class SlowedSimultaneouslyProbe : TaskProbe {
		protected override void OnStart() { }
		protected override void OnStop() { }

		protected override void OnTick(float now) {
			RaiseTo(CombatEvents.SlowedCount);
		}
	}

	// Сколько секунд подряд один враг пробыл под замедлением электро-башен
	public class SlowedDurationProbe : TaskProbe {
		protected override void OnStart() { }
		protected override void OnStop() { }

		protected override void OnTick(float now) {
			RaiseTo(Mathf.FloorToInt(CombatEvents.LongestCurrentSlow));
		}
	}

	public class CollectGoldProbe : TaskProbe {
		protected override void OnStart() {
			InGameEventManager.Subscribe(InGameEvents.OnCoinsAmountChanged, OnCoinsAmountChanged);
		}

		protected override void OnStop() {
			InGameEventManager.Unsubscribe(InGameEvents.OnCoinsAmountChanged, OnCoinsAmountChanged);
		}

		private void OnCoinsAmountChanged(object obj) {
			if (obj is int amount && amount > 0)
				Add(amount);
		}
	}
}
