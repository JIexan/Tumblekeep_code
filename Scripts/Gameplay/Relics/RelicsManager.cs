using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Relics {
	// Статический query-сервис + состояние катки (паттерн LevelUpgradesManager + LevelTasksManager).
	// Pull-модель: геймплейный код спрашивает факторы/бонусы в точках вычисления значений
	// (GetFactor/GetFlat/ApplyPrice), триггерные эффекторы исполняются по событиям.
	// Всё состояние живёт одну катку: ResetRun вызывается из GameController.Awake и ResetLevel;
	// «Продолжить» после победы сцену не перезагружает — реликвии переживают.
	// Статический класс намеренно: потребители — неинжектируемый сценовый код (Unit, Castle,
	// CoinsManager), без источников эффекторов все запросы нейтральны и почти бесплатны.
	// Второй источник эффекторов — особенность острова (IslandFeaturesManager): работает как
	// реликвия в один стак, но живёт вне Owned — HUD-ряд, книга, читы и дроп её не видят.
	public static class RelicsManager {
		public class OwnedRelic {
			public RelicDefinition Definition;
			public int Stacks;
		}

		// Глубже этого цепные взрывы «Петарды» не идут (килл от взрыва снова триггерит взрыв).
		private const int MaxExplosionDepth = 3;

		private static RelicsSettings settings;
		private static readonly List<OwnedRelic> owned = new();
		// Эффекторы особенности острова; null — у уровня её нет. ResetRun их не трогает
		// (чит сброса реликвий зовёт его посреди катки) — ставит/снимает GameController.
		private static List<BaseRelicEffector> levelEffectors;
		private static int currentWave;
		private static int killCounter;
		private static bool secondLifeConsumed;
		private static int explosionDepth;
		private static bool subscribed;

		// Кадровый кэш контекстно-независимых агрегатов: статы башен читаются каждый
		// кадр/выстрел, а условия-состояния (ХП замка, монеты, волна) меняются не чаще кадра.
		private static readonly Dictionary<RelicValue, CachedAggregate> cache = new();

		private class CachedAggregate {
			public int frame = -1;
			public float percent;
			public float flat;
		}

		public static IReadOnlyList<OwnedRelic> Owned => owned;
		public static bool HasAny => owned.Count > 0;
		public static int CurrentWave => currentWave;
		// Каталог для читов/дебага; геймплей ходит через query-методы.
		public static RelicsSettings Catalog => settings;

		public static void Init(RelicsSettings relicsSettings) {
			settings = relicsSettings;
			if (!subscribed) {
				subscribed = true;
				InGameEventManager.Subscribe(InGameEvents.OnWaveStarted, OnWaveStarted);
			}
		}

		public static void ResetRun() {
			owned.Clear();
			currentWave = 0;
			killCounter = 0;
			secondLifeConsumed = false;
			explosionDepth = 0;
			cache.Clear();
		}

		public static void SetLevelEffectors(List<BaseRelicEffector> effectors) {
			levelEffectors = effectors != null && effectors.Count > 0 ? effectors : null;
			InvalidateCache();
		}

		public static void Add(RelicDefinition definition) {
			if (definition == null)
				return;
			var existing = FindOwned(definition);
			if (existing != null) {
				// Дубль нестакаемой/упершейся в лимит не должен выпадать (см. CanDrop) — no-op.
				if (!definition.Stackable)
					return;
				if (definition.MaxStacks > 0 && existing.Stacks >= definition.MaxStacks)
					return;
				existing.Stacks++;
			} else {
				owned.Add(new OwnedRelic { Definition = definition, Stacks = 1 });
			}
			InvalidateCache();
			InGameEventManager.SendEvent(InGameEvents.OnRelicAdded, definition);
		}

		public static bool TryAdd(string id) {
			var definition = settings != null ? settings.GetRelic(id) : null;
			if (definition == null || !CanDrop(definition))
				return false;
			Add(definition);
			return true;
		}

		public static int GetOwnedStacks(RelicDefinition definition) {
			var existing = FindOwned(definition);
			return existing != null ? existing.Stacks : 0;
		}

		// ---------- Агрегаты значений (pull) ----------

		public static float GetFactor(RelicValue value) {
			if (!HasSources)
				return 1f;
			var aggregate = GetCachedAggregate(value);
			return Mathf.Max(0f, 1f + aggregate.percent / 100f);
		}

		public static float GetFactor(RelicValue value, in RelicContext ctx) {
			if (!HasSources)
				return 1f;
			if (ctx.Unit == null)
				return GetFactor(value);
			ComputeAggregate(value, ctx, out float percent, out _);
			return Mathf.Max(0f, 1f + percent / 100f);
		}

		public static float GetFlat(RelicValue value) {
			if (!HasSources)
				return 0f;
			return GetCachedAggregate(value).flat;
		}

		public static float GetFlat(RelicValue value, in RelicContext ctx) {
			if (!HasSources)
				return 0f;
			if (ctx.Unit == null)
				return GetFlat(value);
			ComputeAggregate(value, ctx, out _, out float flat);
			return flat;
		}

		// Цены: (base + flat) * factor, не меньше нуля.
		public static int ApplyPrice(RelicValue value, int basePrice) {
			if (!HasSources)
				return basePrice;
			float price = (basePrice + GetFlat(value)) * GetFactor(value);
			return Mathf.Max(0, Mathf.RoundToInt(price));
		}

		// ---------- Киллы ----------

		// Монеты за «наградной» килл: фактор CoinsPerKill (+условия по юниту) и множитель
		// каждого n-го килла («Золотой горшок»). Вызывается ровно один раз на награждённый килл.
		public static int ModifyKillCoins(int coins, Unit unit) {
			if (!HasSources)
				return coins;

			killCounter++;
			var ctx = new RelicContext(unit);
			float result = (coins + GetFlat(RelicValue.CoinsPerKill, ctx)) * GetFactor(RelicValue.CoinsPerKill, ctx);

			for (int i = 0; i < SourceCount; i++) {
				GetSource(i, out var effectors, out _);
				for (int j = 0; j < effectors.Count; j++) {
					if (effectors[j] is not NthKillCoinsRelicEffector nth || nth.N <= 0)
						continue;
					if (killCounter % nth.N != 0 || !nth.ConditionsMet(ctx))
						continue;
					result *= nth.CoinsMultiplier;
				}
			}

			return Mathf.Max(0, Mathf.RoundToInt(result));
		}

		// Триггеры на килл (лечение, взрывы). Вызывать только из ветки addCoins в Unit.Kill —
		// утёкшие до замка юниты наградой не считаются (OnUnitKilled для этого не годится).
		public static void NotifyRewardedKill(Unit unit) {
			if (!HasSources || unit == null)
				return;

			var ctx = new RelicContext(unit);
			bool allowExplosions = explosionDepth < MaxExplosionDepth;
			explosionDepth++;
			try {
				FireTriggers(RelicTrigger.UnitKilled, ctx, allowExplosions);
			} finally {
				explosionDepth--;
			}
		}

		// ---------- Точечные механики ----------

		public static bool RollSpellSave() {
			if (!HasSources)
				return false;
			float chance = GetFlat(RelicValue.SpellSaveChance);
			return chance > 0f && Random.value * 100f < chance;
		}

		// «Перо феникса»: одна вторая жизнь на катку, независимо от числа таких реликвий.
		public static bool TryConsumeSecondLife(out float restorePercent) {
			restorePercent = 0f;
			if (!HasSources || secondLifeConsumed)
				return false;

			var ctx = RelicContext.None;
			for (int i = 0; i < SourceCount; i++) {
				GetSource(i, out var effectors, out _);
				for (int j = 0; j < effectors.Count; j++) {
					if (effectors[j] is not SecondLifeRelicEffector secondLife || !secondLife.ConditionsMet(ctx))
						continue;
					secondLifeConsumed = true;
					restorePercent = secondLife.RestorePercent;
					return true;
				}
			}
			return false;
		}

		// ---------- Маппинги для хуков характеристик ----------

		// Плоские бонусы к характеристикам замка (Castle.GetCharacteristicValue).
		public static float GetCastleFlat(CharacteristicType type) {
			if (!HasSources)
				return 0f;
			switch (type) {
				case CharacteristicType.Luck: return GetFlat(RelicValue.Luck);
				case CharacteristicType.Resistance: return GetFlat(RelicValue.CastleResistance);
				case CharacteristicType.Regeneration: return GetFlat(RelicValue.CastleRegeneration);
				default: return 0f;
			}
		}

		// Множители характеристик башен (BaseTower.GetCharacteristicValue).
		// Скорость атаки сюда не входит — Speed насыщающийся, фактор применяется
		// делителем в BaseTowerWeapon.ReloadTime.
		public static float GetTowerCharacteristicFactor(CharacteristicType type) {
			if (!HasSources)
				return 1f;
			switch (type) {
				case CharacteristicType.Damage:
				case CharacteristicType.ProjectileDamage:
					return GetFactor(RelicValue.TowerDamage);
				case CharacteristicType.Radius:
					return GetFactor(RelicValue.TowerRadius);
				default: return 1f;
			}
		}

		// ---------- Дроп ----------

		// Может ли реликвия ещё выпасть: нестакаемая — только пока не получена,
		// стакаемая — пока не упёрлась в MaxStacks. Сам ролл сундука живёт в
		// ChestTierConfig.RollRelic (таблицы по тирам в ChestsSettings).
		public static bool CanDrop(RelicDefinition definition) {
			if (definition == null)
				return false;
			// Мета-лок: реликвия ещё не открыта восстановлением хаба (ContentUnlocksSettings/профиль)
			if (!Gameplay.Unlocks.ContentUnlocksManager.IsRelicUnlocked(definition.Id))
				return false;
			var existing = FindOwned(definition);
			if (existing == null)
				return true;
			if (!definition.Stackable)
				return false;
			return definition.MaxStacks <= 0 || existing.Stacks < definition.MaxStacks;
		}

		// ---------- Внутреннее ----------

		private static void OnWaveStarted(object param) {
			if (param is int wave)
				currentWave = wave;
			if (!HasSources)
				return;
			FireTriggers(RelicTrigger.WaveStarted, RelicContext.None, allowExplosions: true);
		}

		private static void FireTriggers(RelicTrigger trigger, in RelicContext ctx, bool allowExplosions) {
			for (int i = 0; i < SourceCount; i++) {
				GetSource(i, out var effectors, out int stacks);
				for (int j = 0; j < effectors.Count; j++) {
					if (effectors[j] is not ITriggeredRelicEffector triggered || triggered.Trigger != trigger)
						continue;
					if (!allowExplosions && triggered is ExplodeOnKillRelicEffector)
						continue;
					if (!effectors[j].ConditionsMet(ctx))
						continue;
					triggered.Execute(ctx, stacks);
				}
			}
		}

		// Источники эффекторов: реликвии игрока + (последним индексом) особенность острова в один стак.
		private static bool HasSources => owned.Count > 0 || levelEffectors != null;
		private static int SourceCount => owned.Count + (levelEffectors != null ? 1 : 0);

		private static void GetSource(int index, out List<BaseRelicEffector> effectors, out int stacks) {
			if (index < owned.Count) {
				effectors = owned[index].Definition.Effectors;
				stacks = owned[index].Stacks;
			} else {
				effectors = levelEffectors;
				stacks = 1;
			}
		}

		private static OwnedRelic FindOwned(RelicDefinition definition) {
			for (int i = 0; i < owned.Count; i++) {
				if (owned[i].Definition == definition)
					return owned[i];
			}
			return null;
		}

		private static CachedAggregate GetCachedAggregate(RelicValue value) {
			if (!cache.TryGetValue(value, out var aggregate)) {
				aggregate = new CachedAggregate();
				cache[value] = aggregate;
			}
			if (aggregate.frame != Time.frameCount) {
				ComputeAggregate(value, RelicContext.None, out aggregate.percent, out aggregate.flat);
				aggregate.frame = Time.frameCount;
			}
			return aggregate;
		}

		private static void ComputeAggregate(RelicValue value, in RelicContext ctx, out float percent, out float flat) {
			percent = 0f;
			flat = 0f;
			for (int i = 0; i < SourceCount; i++) {
				GetSource(i, out var effectors, out int stacks);
				for (int j = 0; j < effectors.Count; j++) {
					if (effectors[j] is not IRelicValueContributor contributor || contributor.Target != value)
						continue;
					if (!effectors[j].ConditionsMet(ctx))
						continue;
					percent += contributor.GetPercent(stacks);
					flat += contributor.GetFlat(stacks);
				}
			}
		}

		private static void InvalidateCache() {
			foreach (var aggregate in cache.Values)
				aggregate.frame = -1;
		}
	}
}
