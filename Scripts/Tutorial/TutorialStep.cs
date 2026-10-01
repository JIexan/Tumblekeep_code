using System.Collections.Generic;
using Gameplay.Cutscenes;
using UnityEngine;

namespace Gameplay.Tutorial {
	public enum TutorialAnchorMode {
		None = 0,   // реплика без привязки не показывается (шаг = только катсцена/условия/behaviour)
		World = 1,  // над объектом мира (через систему tracking-панелей)
		Ui = 2,     // над UI-элементом с TutorialUiAnchor
	}

	// Шаг туториала: условия показа -> опциональная катсцена -> туториальная реплика с привязкой
	// -> условия закрытия. Условия — [SerializeReference]-списки, семантика ANY. id — человекочитаемый,
	// идёт в ключ локализации реплики (Dialogue_{id}_{lineId}); guid — save-идентичность.
	// showConditions пуст — шаг стартует сразу, как стал текущим; иначе молча ждёт первое
	// сработавшее условие показа (например «башня готова к апгрейду»). Условие показа может
	// отдать рантайм-цель (Satisfy(Transform)) — она станет worldTarget реплики, если
	// сериализованный worldTarget пуст; у UI-реплики с пустым uiAnchorId так же подхватывается
	// рантайм-RectTransform (строка свитка в HUD).
	// Шаг без реплики (anchorMode None) и без условий закрытия завершается сам — сразу после
	// катсцены и behaviour.
	[System.Serializable]
	public class TutorialStep {
		[SerializeField] string id;
		[SerializeField] Cutscene cutscene;
		[SerializeField] DialogueLine line;
		[Tooltip("Приписка курсивом под репликой — пояснение «от игры», а не слова персонажа. " +
			"Черновик ru; текст берётся по ключу Dialogue_{id}_note. Пусто — приписки нет.")]
		[SerializeField, TextArea(1, 3)] string noteDraftRu;
		[Tooltip("Кнопка «Ок» на пузыре: закрывает шаг (с записью в сейв) без выполнения действия.")]
		[SerializeField] bool okButton;
		[SerializeField] TutorialAnchorMode anchorMode = TutorialAnchorMode.World;
		[SerializeField] Transform worldTarget;
		[SerializeField] string uiAnchorId;
		[Tooltip("Сдвиг пузыря от цели, пиксели референсного разрешения.")]
		[SerializeField] Vector2 offset = new Vector2(0f, 120f);
		[Tooltip("Пивот пузыря: какая его точка встаёт в цель+сдвиг. (0.5,0.5) — центр; " +
			"(1,0) — правый нижний угол (пузырь растёт влево-вверх, для целей у правого края).")]
		[SerializeField] Vector2 bubblePivot = new Vector2(0.5f, 0.5f);
		[Tooltip("Пузырь рисуется поверх окон (цель — элемент внутри окна). Нужен hintsRootTop у менеджера.")]
		[SerializeField] bool aboveWindows;
		[SerializeReference] List<TutorialCondition> showConditions = new();
		[SerializeReference] List<TutorialCondition> completeConditions = new();
		[Tooltip("Логика шага, не выражаемая условиями: пропуск ненужного шага, награда после катсцены.")]
		[SerializeReference] TutorialStepBehaviour behaviour;

		[SerializeField, HideInInspector] string guid;

		public string Id => id;
		public string Guid => guid;
		public Cutscene Cutscene => cutscene;
		public DialogueLine Line => line;
		public bool HasNote => !string.IsNullOrEmpty(noteDraftRu);
		public string NoteDraftRu => noteDraftRu;
		public string NoteLocalizationKey => $"Dialogue_{id}_note";
		public bool OkButton => okButton;
		public TutorialAnchorMode AnchorMode => anchorMode;
		public Transform WorldTarget => worldTarget;
		public string UiAnchorId => uiAnchorId;
		public Vector2 Offset => offset;
		public Vector2 BubblePivot => bubblePivot;
		public bool AboveWindows => aboveWindows;
		public List<TutorialCondition> ShowConditions => showConditions;
		public List<TutorialCondition> CompleteConditions => completeConditions;
		public TutorialStepBehaviour Behaviour => behaviour;

#if UNITY_EDITOR
		// Вызывается из OnValidate менеджера
		public void EditorEnsureIds() {
			if (string.IsNullOrEmpty(guid))
				guid = System.Guid.NewGuid().ToString("N");
			line?.EnsureLineId();
		}

		public void EditorInit(string id, DialogueLine line, TutorialAnchorMode anchorMode,
			Transform worldTarget, string uiAnchorId, List<TutorialCondition> conditions, Cutscene cutscene = null,
			List<TutorialCondition> showConditions = null, Vector2? offset = null, Vector2? bubblePivot = null,
			string noteDraftRu = null, bool okButton = false, string guid = null,
			bool aboveWindows = false, TutorialStepBehaviour behaviour = null) {
			this.id = id;
			this.noteDraftRu = noteDraftRu;
			this.okButton = okButton;
			this.aboveWindows = aboveWindows;
			this.behaviour = behaviour;
			// Сохранённый guid — чтобы пересборка билдером не сбрасывала пройденность шага
			if (!string.IsNullOrEmpty(guid))
				this.guid = guid;
			this.line = line;
			this.anchorMode = anchorMode;
			this.worldTarget = worldTarget;
			this.uiAnchorId = uiAnchorId;
			this.cutscene = cutscene;
			completeConditions = conditions ?? new List<TutorialCondition>();
			this.showConditions = showConditions ?? new List<TutorialCondition>();
			if (offset.HasValue)
				this.offset = offset.Value;
			if (bubblePivot.HasValue)
				this.bubblePivot = bubblePivot.Value;
			EditorEnsureIds();
		}
#endif
	}
}
