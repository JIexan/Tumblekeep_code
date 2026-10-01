#if AUTOPLAY
using System;
using System.Collections.Generic;

namespace Autoplay {
	/// <summary>Срез состояния матча на старте волны — для медиан «сколько к волне N обычно есть»</summary>
	[Serializable]
	public class WaveSnapshot {
		public int wave;
		public float gameTime;
		public float castleHP;
		public float castleNormalized;
		public int coins;
		public int goldCollected;
		public int goldSpent;
		public int towers;
		public int upgrades;
		public int chests;
		public int kills;
		public int bosses;
		public int merchantPurchases;
		public int spells;
		public float towersDps;
		// Накоп MatchRewards без множителя тасков (сырое значение)
		public int accruedXP;
		public int accruedCrystals;
		// Значения проб всех видов доп. тасков на старте волны (TaskType → значение)
		public Dictionary<string, int> taskMetrics = new Dictionary<string, int>();
	}

	/// <summary>
	/// Статистика одного прогона. Метрики тасков уровня (ReachWave / CollectGold / SpendGold / KillBoss /
	/// OpenChests / MerchantPurchase) считаются из тех же InGameEvents, что и сами таски.
	/// </summary>
	[Serializable]
	public class RunStats {
		public int runIndex;
		public int seed;
		public string levelId;
		public string scene;
		public string startedAt;
		public string endReason = "";

		// ReachWave: номер последней стартовавшей волны (таск засчитывается на OnWaveStarted)
		public int maxWaveReached;
		public int authoredWavesCount;
		public bool reachedInfinite;
		public int wavesStarted;
		public int wavesRushed;
		// волны, из которых хоть один юнит дошёл до замка
		public int wavesLeaked;
		public int firstLeakWave = -1;

		// CollectGold / SpendGold
		public int goldCollected;
		public int goldSpent;
		public int finalCoins;

		// KillBoss
		public int unitsKilled;
		public int bossesKilled;
		public int leaks;
		// сумма damagePower дошедших до замка (до резиста)
		public int castleDamageTaken;

		// OpenChests
		public int chestsOpened;
		public Dictionary<string, int> chestsOpenedByTier = new Dictionary<string, int>();
		// тиков, когда сундук стоял, а денег на него не было
		public int chestsUnaffordable;
		public int castleUpgrades;

		// MerchantPurchase
		public int merchantVisits;
		public int merchantWindowsOpened;
		public int merchantPurchases;

		public int towersBuilt;
		public Dictionary<string, int> towersBuiltByType = new Dictionary<string, int>();
		public int upgradesApplied;
		public int upgradeTraits;

		public int spellsCast;
		public Dictionary<string, int> spellsCastByType = new Dictionary<string, int>();

		public List<string> relics = new List<string>();
		// Накоп MatchRewards за катку БЕЗ множителя тасков (тот же счётчик, что идёт в Bank(mult))
		public int accruedXP;
		public int accruedCrystals;
		// Число убийств, с которых выпал хотя бы один кристалл
		public int crystalDrops;
		public Dictionary<string, int> killsByUnitType = new Dictionary<string, int>();
		public Dictionary<string, int> leaksByUnitType = new Dictionary<string, int>();
		public Dictionary<string, int> crystalsByUnitType = new Dictionary<string, int>();
		public bool secondLifeUsed;

		public float matchDurationGameSec;
		public float matchDurationRealSec;
		public float finalTowersDps;

		// ---- Доп. таски уровня (TaskMetricsRecorder) ----
		// Прокачка острова на сейве прогона: шкала 0–100 из progressWeight купленных апгрейдов уровня
		public float islandProgressPercent;
		public List<string> purchasedUpgrades = new List<string>();
		// Виды тасков, которые генератор мог бы выдать при этих покупках (меряются при этом все виды)
		public List<string> availableTaskTypes = new List<string>();
		// TaskType → значение пробы на конец прогона
		public Dictionary<string, int> taskMetrics = new Dictionary<string, int>();
		// TaskType → цель без разброса, которую кривая даёт на islandProgressPercent
		public Dictionary<string, int> taskTargets = new Dictionary<string, int>();
		// TaskType → волна, на которой значение дошло до цели; -1 — не дошло
		public Dictionary<string, int> taskCompletedAtWave = new Dictionary<string, int>();

		public List<WaveSnapshot> waves = new List<WaveSnapshot>();

		public static void Inc(Dictionary<string, int> dict, string key, int amount = 1) {
			dict.TryGetValue(key, out int current);
			dict[key] = current + amount;
		}
	}
}
#endif
