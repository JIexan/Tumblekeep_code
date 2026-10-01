using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Relics {
	// Эффектор — один эффект реликвии с собственным списком условий (все должны выполниться,
	// пустой список = всегда активен). Два семейства:
	//  - IRelicValueContributor: чистые данные, агрегируются RelicsManager по запросу (pull);
	//  - ITriggeredRelicEffector: исполняются менеджером по игровым событиям (push).
	[System.Serializable]
	public abstract class BaseRelicEffector {
		[SerializeReference] List<RelicCondition> conditions = new();

		public List<RelicCondition> Conditions => conditions;

		public bool ConditionsMet(in RelicContext ctx) {
			for (int i = 0; i < conditions.Count; i++) {
				if (conditions[i] != null && !conditions[i].Evaluate(ctx))
					return false;
			}
			return true;
		}

#if UNITY_EDITOR
		public void EditorSetConditions(List<RelicCondition> conditions) {
			this.conditions = conditions ?? new List<RelicCondition>();
		}
#endif
	}

	// Вклад в агрегат геймплейного значения. Проценты суммируются (итог = base * (1 + Σ%/100)),
	// плоские бонусы суммируются. Контекст заходит через условия, а не через геттеры.
	public interface IRelicValueContributor {
		RelicValue Target { get; }
		float GetPercent(int stacks);
		float GetFlat(int stacks);
	}

	public enum RelicTrigger {
		UnitKilled,
		WaveStarted
	}

	// Исполняемый по событию эффектор. Для UnitKilled ctx несёт убитого юнита
	// (только «наградные» киллы — утёкшие до замка юниты триггеры не дёргают).
	public interface ITriggeredRelicEffector {
		RelicTrigger Trigger { get; }
		void Execute(in RelicContext ctx, int stacks);
	}
}
