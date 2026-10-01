using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.RestorationSystem {
	// Карта очерёдности открытия зон хаба: зона открывается, когда ВСЕ её условия
	// (регион-предшественник достиг minPercent) выполнены. Ветвление естественно:
	// несколько зон могут ссылаться на один и тот же предшественник.
	// Зоны без правила открываются только через startUnlocked или UnlockRegionAction.
	// Проверяет PlayerProgressController — на старте хаба и после каждого шага.
	[CreateAssetMenu(fileName = "RegionUnlockSettings", menuName = "Settings/RegionUnlockSettings")]
	public class RegionUnlockSettings : ScriptableObject {
		[System.Serializable]
		public class Condition {
			[SerializeField] RegionId requiredRegion;
			[SerializeField, Range(0, 100)] int minPercent = 50;

			public RegionId RequiredRegion => requiredRegion;
			public int MinPercent => minPercent;

			public bool IsMet() {
				return RegionProgressUtility.GetPercent(requiredRegion) >= minPercent;
			}

#if UNITY_EDITOR
			public void EditorInit(RegionId requiredRegion, int minPercent) {
				this.requiredRegion = requiredRegion;
				this.minPercent = minPercent;
			}
#endif
		}

		[System.Serializable]
		public class Rule {
			[SerializeField] RegionId region;
			[SerializeField] List<Condition> conditions = new();

			public RegionId Region => region;
			public List<Condition> Conditions => conditions;

			public bool AllConditionsMet() {
				foreach (var condition in conditions) {
					if (condition == null || !condition.IsMet())
						return false;
				}
				return true;
			}

#if UNITY_EDITOR
			public void EditorInit(RegionId region, List<Condition> conditions) {
				this.region = region;
				this.conditions = conditions ?? new List<Condition>();
			}
#endif
		}

		[SerializeField] List<Rule> rules = new();

		public List<Rule> Rules => rules;

#if UNITY_EDITOR
		public void EditorSetRules(List<Rule> rules) {
			this.rules = rules ?? new List<Rule>();
		}

		private void OnValidate() {
			var seen = new HashSet<RegionId>();
			foreach (var rule in rules) {
				if (rule == null)
					continue;
				if (!seen.Add(rule.Region))
					Debug.LogWarning($"[RegionUnlockSettings] '{name}': дублирующееся правило для зоны {rule.Region}.", this);
				foreach (var condition in rule.Conditions) {
					if (condition != null && condition.RequiredRegion == rule.Region)
						Debug.LogWarning($"[RegionUnlockSettings] '{name}': зона {rule.Region} требует сама себя.", this);
				}
			}
		}
#endif
	}
}
