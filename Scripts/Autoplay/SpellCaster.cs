#if AUTOPLAY
using System.Collections.Generic;
using Gameplay.Relics;
using UnityEngine;

namespace Autoplay {
	/// <summary>
	/// Применение свитков из профиля с лимитом (интервал + максимум на волну). Приоритет:
	/// лечение замка → защита замка → Freeze/Lightning по юнитам у замка → точечные атаки по группе,
	/// прошедшей башни → баффы (деньги/скорость) на больших волнах. Свиток списывается как в SpellPanel.
	/// </summary>
	public class SpellCaster {
		private static readonly ItemType[] pointSpells = {
			ItemType.SpellExplode, ItemType.SpellFireball, ItemType.SpellPoison, ItemType.SpellPoisonCloud
		};

		private readonly AutoplayBot bot;
		private readonly AutoplayConfig cfg;
		private readonly RunStats stats;
		private readonly List<RouteModel.UnitProgress> leakers = new List<RouteModel.UnitProgress>();

		private float lastCastTime = -999f;
		private float lastHealTime = -999f;
		private int castsThisWave;

		public float LastHealTime => lastHealTime;

		public SpellCaster(AutoplayBot bot, AutoplayConfig cfg, RunStats stats) {
			this.bot = bot;
			this.cfg = cfg;
			this.stats = stats;
		}

		public void OnWaveStarted() {
			castsThisWave = 0;
		}

		public void Tick(float gameTime, List<Unit> enemies) {
			if (cfg.maxSpellsPerWave <= 0 || castsThisWave >= cfg.maxSpellsPerWave)
				return;
			if (gameTime - lastCastTime < cfg.spellMinInterval)
				return;
			var profile = SavesManager.Profile;
			var spells = SpellsManager.Instance;
			var castle = Castle.Instance;
			var gc = bot.Game;
			if (profile == null || spells == null || castle == null || gc == null)
				return;

			bool Has(ItemType type) => profile.HasItem(type, 1);

			// 1. Лечение
			if (Has(ItemType.SpellHeal) && castle.NormalizedHP < cfg.healThreshold) {
				Cast(ItemType.SpellHeal, castle.transform.position, gameTime, $"castle hp {castle.NormalizedHP:P0}");
				lastHealTime = gameTime;
				return;
			}

			// Кто прошёл башни и насколько близко к замку
			bot.Routes.EnsureCoverage(gameTime, gc.GetAllTowers());
			bot.Routes.CollectLeakers(enemies, leakers);
			int approaching = 0;
			float incomingDamage = 0f;
			bool bossApproaching = false;
			int leaderIndex = -1;
			for (int i = 0; i < leakers.Count; i++) {
				var l = leakers[i];
				if (l.remaining < cfg.leakDistance) {
					approaching++;
					incomingDamage += l.unit.damagePower;
					if (l.unit.IsBoss)
						bossApproaching = true;
				}
				if (leaderIndex < 0 || l.s > leakers[leaderIndex].s)
					leaderIndex = i;
			}

			// 2. Защита замка
			if (Has(ItemType.SpellProtect) && SpellsManager.CastleResistanceBonus <= 0f && incomingDamage >= cfg.protectMinIncomingDamage) {
				Cast(ItemType.SpellProtect, castle.transform.position, gameTime, $"incoming damage {incomingDamage:F0}");
				return;
			}

			// 3. Заморозка всех
			if (Has(ItemType.SpellFreeze) && (approaching >= 2 || bossApproaching || (castle.NormalizedHP < 0.3f && approaching >= 1))) {
				Cast(ItemType.SpellFreeze, Vector3.zero, gameTime, $"{approaching} approaching{(bossApproaching ? " incl. boss" : "")}");
				return;
			}

			// 4. Молнии
			if (Has(ItemType.SpellLightning) && approaching >= 1 && spells.lighnningEffect != null && !spells.lighnningEffect.IsPlaying) {
				Cast(ItemType.SpellLightning, Vector3.zero, gameTime, $"{approaching} approaching");
				return;
			}

			// 5. Точечные — по лидеру утёкшей группы
			if (leaderIndex >= 0) {
				var leader = leakers[leaderIndex];
				var itemSettings = GameConfigs.Instance.ItemSettings;
				foreach (var type in pointSpells) {
					if (!Has(type))
						continue;
					var data = itemSettings.GetSpellData(type);
					float radius = data != null ? Mathf.Max(data.value1, 1f) : 3f;
					// Метеорам нужно время долететь — целимся чуть впереди
					float lead = type == ItemType.SpellFireball ? 3f : 0f;
					Vector3 point = bot.Routes.PointAhead(leader.route, leader.s, lead);
					int inRadius = gc.GetAllEnemiesInRadius(point, radius).Length;
					int need = type == ItemType.SpellPoisonCloud ? 2 : 1;
					if (inRadius >= need) {
						Cast(type, point, gameTime, $"{inRadius} leakers in r={radius:F1}");
						return;
					}
				}
			}

			// 6. Баффы на крупных волнах
			int alive = enemies.Count;
			if (Has(ItemType.SpellMoney) && spells.moneySpellEffect != null && !spells.moneySpellEffect.IsPlaying && alive >= 5) {
				Cast(ItemType.SpellMoney, Vector3.zero, gameTime, $"{alive} enemies alive");
				return;
			}
			if (Has(ItemType.SpellSpeedUp) && spells.speedUpSpellEffect != null && !spells.speedUpSpellEffect.IsPlaying && (alive >= 8 || AnyBoss(enemies))) {
				Cast(ItemType.SpellSpeedUp, Vector3.zero, gameTime, $"{alive} enemies alive");
			}
		}

		private static bool AnyBoss(List<Unit> enemies) {
			foreach (var u in enemies) {
				if (u != null && u.IsBoss)
					return true;
			}
			return false;
		}

		private void Cast(ItemType type, Vector3 point, float gameTime, string reason) {
			SpellsManager.Instance.ApplySpellAtPoint(point, type);
			// Как в SpellPanel: реликвия может сохранить свиток
			if (!RelicsManager.RollSpellSave())
				SavesManager.Profile.SpendItem(type, 1);
			lastCastTime = gameTime;
			castsThisWave++;
			stats.spellsCast++;
			RunStats.Inc(stats.spellsCastByType, type.ToString());
			bot.Log($"cast {type}: {reason}");
		}
	}
}
#endif
