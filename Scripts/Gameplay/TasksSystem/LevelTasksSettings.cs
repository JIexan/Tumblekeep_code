using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.TasksSystem {
	// Описание одного вида доп. таска уровня: когда доступен и как цель растёт с прокачкой острова.
	[System.Serializable]
	public class TaskDefinition {
		[SerializeField] TaskType type;
		[SerializeField] TaskGroup group;
		// false => нужен анлок из купленных апгрейдов уровня (LevelUpgradeEntry.unlockedTasks)
		[SerializeField] bool availableFromStart;
		[Min(0f)]
		[SerializeField] float weight = 1f;
		// X: прокачка острова 0..1, Y: 0..1 — доля пути от valueRange.x до valueRange.y
		[SerializeField] AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
		[SerializeField] Vector2 valueRange = new Vector2(1f, 10f);
		// Разброс цели: 0.1 = ±10%
		[Range(0f, 0.5f)]
		[SerializeField] float spread = 0.1f;
		[Min(1)]
		[SerializeField] int roundTo = 1;
		// Доп. параметр вида таска (окно в секундах у DamagedInWindow); в описание идёт как {1}
		[SerializeField] float param;
		// Формат-строка локализации: {0} — цель, {1} — param
		[SerializeField] string descriptionKey;

		public TaskType Type => type;
		public TaskGroup Group => group;
		public bool AvailableFromStart => availableFromStart;
		public float Weight => weight;
		public float Param => param;
		public string DescriptionKey => descriptionKey;

		// Цель без разброса — для логов автоплея и читов
		public int GetMedianTarget(float progress01) {
			return Round(Evaluate(progress01));
		}

		public int GetTarget(float progress01) {
			return Round(Evaluate(progress01) * (1f + Random.Range(-spread, spread)));
		}

		private float Evaluate(float progress01) {
			return Mathf.Lerp(valueRange.x, valueRange.y, Mathf.Clamp01(curve.Evaluate(Mathf.Clamp01(progress01))));
		}

		private int Round(float value) {
			int step = Mathf.Max(1, roundTo);
			return Mathf.Max(step, Mathf.RoundToInt(value / step) * step);
		}

#if UNITY_EDITOR
		public void EditorInit(TaskType type, TaskGroup group, bool availableFromStart, float weight,
			Vector2 valueRange, float spread, int roundTo, float param, AnimationCurve curve = null) {
			this.type = type;
			this.group = group;
			this.availableFromStart = availableFromStart;
			this.weight = weight;
			this.curve = curve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
			this.valueRange = valueRange;
			this.spread = spread;
			this.roundTo = roundTo;
			this.param = param;
			this.descriptionKey = "Task_" + type;
		}
#endif
	}

	[CreateAssetMenu(fileName = "LevelTasksSettings", menuName = "Settings/LevelTasksSettings")]
	public class LevelTasksSettings : ScriptableObject {
		// Множитель наград и анимация VictoryWindow рассчитаны ровно на 2 доп. таска
		[Range(0, 2)]
		[SerializeField] int secondaryCount = 2;
		[ListDrawerSettings(ListElementLabelName = "Type")]
		[SerializeField] List<TaskDefinition> definitions = new();

		public int SecondaryCount => secondaryCount;
		public List<TaskDefinition> Definitions => definitions;

		public TaskDefinition GetDefinition(TaskType type) {
			foreach (var definition in definitions) {
				if (definition != null && definition.Type == type)
					return definition;
			}
			return null;
		}

#if UNITY_EDITOR
		private void OnValidate() {
			var seen = new HashSet<TaskType>();
			foreach (var definition in definitions) {
				if (definition == null)
					continue;
				if (!seen.Add(definition.Type))
					Debug.LogWarning($"[LevelTasksSettings] '{name}': дублирующийся вид таска '{definition.Type}'.", this);
				if (definition.Type == TaskType.ReachWave || definition.Type == TaskType.KillBoss)
					Debug.LogWarning($"[LevelTasksSettings] '{name}': '{definition.Type}' — основной таск, в пул доп. тасков его класть нельзя.", this);
			}
		}
#endif
	}
}
