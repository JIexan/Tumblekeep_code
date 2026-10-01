#if AUTOPLAY
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Autoplay {
	/// <summary>
	/// Папка сессии: config.json, runs.jsonl (строка на прогон, пишется сразу — падение редактора
	/// не теряет данные) и summary.json (агрегаты по метрикам тасков + кривая выживания по волнам).
	/// </summary>
	public class AutoplayStatsWriter {
		public string SessionFolder { get; }
		private readonly List<RunStats> runs = new List<RunStats>();

		public AutoplayStatsWriter(string root, AutoplayConfig cfg) {
			string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			SessionFolder = Path.Combine(root, $"{Sanitize(cfg.levelId)}_{stamp}");
			Directory.CreateDirectory(SessionFolder);
			File.WriteAllText(Path.Combine(SessionFolder, "config.json"),
				JsonConvert.SerializeObject(cfg, Formatting.Indented));
		}

		public void AppendRun(RunStats stats) {
			runs.Add(stats);
			if (runs.Count == 1)
				WriteIslandStateToConfig(stats);
			try {
				File.AppendAllText(Path.Combine(SessionFolder, "runs.jsonl"),
					JsonConvert.SerializeObject(stats, Formatting.None) + "\n");
			} catch (Exception e) {
				Debug.LogError("[Autoplay] failed to write run stats: " + e.Message);
			}
		}

		// Серия играет на одном снимке профиля, но в момент создания writer'а он ещё не установлен —
		// состояние прокачки острова дописываем в config.json по первому прогону
		private void WriteIslandStateToConfig(RunStats stats) {
			try {
				string path = Path.Combine(SessionFolder, "config.json");
				var config = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
				config["islandProgressPercent"] = stats.islandProgressPercent;
				config["purchasedUpgrades"] = Newtonsoft.Json.Linq.JArray.FromObject(stats.purchasedUpgrades);
				config["availableTaskTypes"] = Newtonsoft.Json.Linq.JArray.FromObject(stats.availableTaskTypes);
				File.WriteAllText(path, config.ToString(Formatting.Indented));
			} catch (Exception e) {
				Debug.LogError("[Autoplay] failed to write island state to config.json: " + e.Message);
			}
		}

		// Доп. таски уровня: чего бот достигает по каждому виду таска при текущей прокачке острова.
		// Балансировка кривой: на волне основного таска взять perWave p25–median и поставить точку на islandProgressPercent
		private Dictionary<string, object> DescribeTasks(int maxWave) {
			var tasks = new Dictionary<string, object>();
			foreach (string type in runs.SelectMany(r => r.taskMetrics.Keys).Distinct()) {
				var withType = runs.Where(r => r.taskMetrics.ContainsKey(type)).ToList();
				var completed = withType.Where(r => r.taskCompletedAtWave.TryGetValue(type, out int wave) && wave >= 0).ToList();

				var perWave = new Dictionary<string, object>();
				for (int k = 1; k <= maxWave; k++) {
					var values = withType
						.Select(r => r.waves.FirstOrDefault(w => w.wave == k))
						.Where(w => w != null && w.taskMetrics.ContainsKey(type))
						.Select(w => (float)w.taskMetrics[type])
						.OrderBy(v => v).ToArray();
					if (values.Length == 0)
						continue;
					perWave[k.ToString()] = new Dictionary<string, float> {
						["runs"] = values.Length,
						["p25"] = Percentile(values, 0.25f),
						["median"] = Percentile(values, 0.5f),
						["p75"] = Percentile(values, 0.75f),
					};
				}

				tasks[type] = new Dictionary<string, object> {
					["target"] = Median(withType.Where(r => r.taskTargets.ContainsKey(type)).Select(r => (float)r.taskTargets[type])),
					["availableShare"] = withType.Count(r => r.availableTaskTypes.Contains(type)) / (float)withType.Count,
					["final"] = Describe(withType.Select(r => (float)r.taskMetrics[type])),
					["completionShare"] = completed.Count / (float)withType.Count,
					["completedAtWave"] = Describe(completed.Select(r => (float)r.taskCompletedAtWave[type])),
					["perWave"] = perWave,
				};
			}
			return tasks;
		}

		public void WriteSummary() {
			var summary = new Dictionary<string, object> {
				["runs"] = runs.Count,
				["generatedAt"] = DateTime.Now.ToString("s"),
			};
			if (runs.Count == 0) {
				Write(summary);
				return;
			}

			var metrics = new Dictionary<string, object>();
			void Metric(string name, Func<RunStats, float> selector, Func<RunStats, bool> filter = null) {
				var source = filter == null ? runs : runs.Where(filter).ToList();
				metrics[name] = Describe(source.Select(selector));
			}
			Metric("maxWaveReached", r => r.maxWaveReached);
			Metric("goldCollected", r => r.goldCollected);
			Metric("goldSpent", r => r.goldSpent);
			Metric("bossesKilled", r => r.bossesKilled);
			Metric("unitsKilled", r => r.unitsKilled);
			Metric("chestsOpened", r => r.chestsOpened);
			Metric("castleUpgrades", r => r.castleUpgrades);
			Metric("merchantVisits", r => r.merchantVisits);
			Metric("merchantPurchases", r => r.merchantPurchases);
			Metric("towersBuilt", r => r.towersBuilt);
			Metric("upgradesApplied", r => r.upgradesApplied);
			Metric("spellsCast", r => r.spellsCast);
			Metric("leaks", r => r.leaks);
			Metric("wavesLeaked", r => r.wavesLeaked);
			Metric("firstLeakWave", r => r.firstLeakWave, r => r.firstLeakWave >= 0);
			Metric("accruedXP", r => r.accruedXP);
			Metric("accruedCrystals", r => r.accruedCrystals);
			Metric("crystalDrops", r => r.crystalDrops);
			Metric("finalCoins", r => r.finalCoins);
			Metric("finalTowersDps", r => r.finalTowersDps);
			Metric("matchDurationGameSec", r => r.matchDurationGameSec);
			summary["metrics"] = metrics;

			int maxWave = runs.Max(r => r.maxWaveReached);
			// Доля прогонов, доживших до старта волны k — прямой ответ для таска ReachWave(k)
			var survival = new Dictionary<string, float>();
			for (int k = 1; k <= maxWave; k++)
				survival[k.ToString()] = runs.Count(r => r.maxWaveReached >= k) / (float)runs.Count;
			summary["reachWaveSurvival"] = survival;

			// Медианы состояния на старте волны k (только по прогонам, дожившим до неё)
			var perWave = new Dictionary<string, object>();
			for (int k = 1; k <= maxWave; k++) {
				var snaps = runs.Select(r => r.waves.FirstOrDefault(w => w.wave == k)).Where(w => w != null).ToList();
				if (snaps.Count == 0)
					continue;
				perWave[k.ToString()] = new Dictionary<string, float> {
					["runs"] = snaps.Count,
					["castleNormalizedMedian"] = Median(snaps.Select(s => s.castleNormalized)),
					["coinsMedian"] = Median(snaps.Select(s => (float)s.coins)),
					["goldCollectedMedian"] = Median(snaps.Select(s => (float)s.goldCollected)),
					["goldSpentMedian"] = Median(snaps.Select(s => (float)s.goldSpent)),
					["chestsMedian"] = Median(snaps.Select(s => (float)s.chests)),
					["towersMedian"] = Median(snaps.Select(s => (float)s.towers)),
					["upgradesMedian"] = Median(snaps.Select(s => (float)s.upgrades)),
					["killsMedian"] = Median(snaps.Select(s => (float)s.kills)),
					["bossesMedian"] = Median(snaps.Select(s => (float)s.bosses)),
					["merchantPurchasesMedian"] = Median(snaps.Select(s => (float)s.merchantPurchases)),
					["spellsMedian"] = Median(snaps.Select(s => (float)s.spells)),
					["towersDpsMedian"] = Median(snaps.Select(s => s.towersDps)),
					["gameTimeMedian"] = Median(snaps.Select(s => s.gameTime)),
					["accruedXPMedian"] = Median(snaps.Select(s => (float)s.accruedXP)),
					["accruedCrystalsMedian"] = Median(snaps.Select(s => (float)s.accruedCrystals)),
				};
			}
			summary["perWave"] = perWave;

			summary["islandProgressPercent"] = runs[0].islandProgressPercent;
			summary["purchasedUpgrades"] = runs[0].purchasedUpgrades;
			summary["tasks"] = DescribeTasks(maxWave);

			summary["reachedInfiniteShare"] = runs.Count(r => r.reachedInfinite) / (float)runs.Count;
			summary["endReasons"] = runs.GroupBy(r => r.endReason ?? "").ToDictionary(g => g.Key, g => g.Count());
			summary["towersBuiltByType"] = Merge(runs.Select(r => r.towersBuiltByType));
			summary["spellsCastByType"] = Merge(runs.Select(r => r.spellsCastByType));
			summary["chestsOpenedByTier"] = Merge(runs.Select(r => r.chestsOpenedByTier));
			// Кристаллы: суммарно по серии, без множителя тасков
			int totalKills = runs.Sum(r => r.unitsKilled);
			int totalCrystals = runs.Sum(r => r.accruedCrystals);
			summary["crystals"] = new Dictionary<string, object> {
				["total"] = totalCrystals,
				["drops"] = runs.Sum(r => r.crystalDrops),
				["perKill"] = totalKills > 0 ? totalCrystals / (float)totalKills : 0f,
				["perRun"] = totalCrystals / (float)runs.Count,
				["byUnitType"] = Merge(runs.Select(r => r.crystalsByUnitType)),
			};
			summary["killsByUnitType"] = Merge(runs.Select(r => r.killsByUnitType));
			summary["leaksByUnitType"] = Merge(runs.Select(r => r.leaksByUnitType));

			Write(summary);
		}

		private void Write(Dictionary<string, object> summary) {
			File.WriteAllText(Path.Combine(SessionFolder, "summary.json"),
				JsonConvert.SerializeObject(summary, Formatting.Indented));
		}

		private static Dictionary<string, int> Merge(IEnumerable<Dictionary<string, int>> dicts) {
			var res = new Dictionary<string, int>();
			foreach (var d in dicts) {
				if (d == null)
					continue;
				foreach (var kv in d)
					RunStats.Inc(res, kv.Key, kv.Value);
			}
			return res;
		}

		private static Dictionary<string, float> Describe(IEnumerable<float> values) {
			var a = values.OrderBy(v => v).ToArray();
			if (a.Length == 0)
				return null;
			float mean = a.Average();
			float variance = a.Length > 1 ? a.Sum(v => (v - mean) * (v - mean)) / (a.Length - 1) : 0f;
			return new Dictionary<string, float> {
				["count"] = a.Length,
				["mean"] = mean,
				["median"] = Percentile(a, 0.5f),
				["min"] = a[0],
				["max"] = a[a.Length - 1],
				["stddev"] = Mathf.Sqrt(variance),
				["p10"] = Percentile(a, 0.10f),
				["p25"] = Percentile(a, 0.25f),
				["p75"] = Percentile(a, 0.75f),
				["p90"] = Percentile(a, 0.90f),
			};
		}

		private static float Median(IEnumerable<float> values) {
			var a = values.OrderBy(v => v).ToArray();
			return a.Length == 0 ? 0f : Percentile(a, 0.5f);
		}

		// Линейная интерполяция между соседними элементами отсортированного массива
		private static float Percentile(float[] sorted, float p) {
			if (sorted.Length == 1)
				return sorted[0];
			float pos = p * (sorted.Length - 1);
			int lo = Mathf.FloorToInt(pos);
			int hi = Mathf.Min(lo + 1, sorted.Length - 1);
			return Mathf.Lerp(sorted[lo], sorted[hi], pos - lo);
		}

		private static string Sanitize(string s) {
			if (string.IsNullOrEmpty(s))
				return "level";
			foreach (char c in Path.GetInvalidFileNameChars())
				s = s.Replace(c, '_');
			return s;
		}
	}
}
#endif
