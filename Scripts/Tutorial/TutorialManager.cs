using System.Text.RegularExpressions;
using Gameplay.Cutscenes;
using UnityEngine;

namespace Gameplay.Tutorial {
	// Последовательность туториала одной сцены (хаб и бой — свои инстансы со своими
	// шагами; сценный компонент, а не SO — шаги ссылаются на объекты сцены).
	// Флоу шага: [молчаливое ожидание showConditions] -> (behaviour.ShouldSkip — закрыть
	// молча) -> (катсцена) -> (behaviour.AfterCutscene) -> реплика с привязкой -> первое
	// сработавшее completeCondition (или «Ок» на пузыре) -> снять реплику, записать guid
	// в сейв, следующий шаг. Шаг без реплики и без условий закрытия завершается сразу после
	// катсцены/behaviour. Завершённые шаги пропускаются при загрузке.
	// Условия могут срабатывать синхронно прямо в Subscribe (проверка уже-истинного
	// состояния) — циклы подписки сторожат смену текущего шага.
	public class TutorialManager : MonoBehaviour, IDialogueLineOwner {
		[SerializeField] System.Collections.Generic.List<TutorialStep> steps = new();
		[SerializeField] UI.Tutorial.UiHintFollower uiHintPrefab;
		[Tooltip("Контейнер пузырей вне скрываемого HUD (хаб прячет HUD на глобальной карте, " +
			"а трекинг-панели живут под ним). Пусто — пузыри остаются под контроллером трекинга.")]
		[SerializeField] RectTransform hintsRoot;
		[Tooltip("Контейнер пузырей ПОВЕРХ окон — для шагов с aboveWindows (цель внутри окна). " +
			"Пусто — такие шаги падают в hintsRoot и окажутся под окном.")]
		[SerializeField] RectTransform hintsRootTop;

		public static bool IsHintActive { get; private set; }
		public System.Collections.Generic.IReadOnlyList<TutorialStep> Steps => steps;

		private TutorialStep currentStep;
		private UI.Tutorial.UiHintFollower activeUiHint;
		private TrackingPanelsController trackingController;
		private bool stepFinishing;
		// Условие показа сработало: шаг ушёл в катсцену/реплику, повторные срабатывания игнорируются
		private bool stepTriggered;

		private void Start() {
			trackingController = FindObjectOfType<TrackingPanelsController>();
			SavesManager.Subscribe(OnProfileLoaded);
		}

		private void OnDestroy() {
			SavesManager.Unsubscribe(OnProfileLoaded);
			CleanupCurrentStep();
		}

		private void OnProfileLoaded(object _) {
			if (currentStep == null)
				StartNextStep();
		}

		// Фаза ожидания: с пустыми showConditions шаг стартует сразу, иначе молча ждёт
		// первое сработавшее (оно может отдать рантайм-цель)
		private void StartNextStep() {
			currentStep = null;
			stepFinishing = false;
			stepTriggered = false;
			IsHintActive = false;

			var tutorial = SavesManager.Profile?.tutorial;
			if (tutorial == null)
				return;

			foreach (var candidate in steps) {
				if (candidate == null || tutorial.IsStepCompleted(candidate.Guid))
					continue;
				currentStep = candidate;
				break;
			}

			var step = currentStep;
			if (step == null)
				return;

			if (step.ShowConditions.Count == 0) {
				OnShowTriggered(null);
				return;
			}

			foreach (var condition in step.ShowConditions) {
				condition?.Subscribe(OnShowTriggered);
				// Условие сработало синхронно в Subscribe — остальные не подписываем
				if (currentStep != step || stepTriggered)
					return;
			}
		}

		private void OnShowTriggered(Transform runtimeTarget) {
			if (currentStep == null || stepTriggered)
				return;
			stepTriggered = true;

			var step = currentStep;
			foreach (var condition in step.ShowConditions)
				condition?.Unsubscribe();

			if (step.Behaviour != null && step.Behaviour.ShouldSkip()) {
				CompleteCurrentStep();
				return;
			}

			if (step.Cutscene != null && !(step.Cutscene.Once && step.Cutscene.WasWatched))
				CutsceneController.Instance.Play(step.Cutscene, () => AfterCutscene(step, runtimeTarget));
			else
				AfterCutscene(step, runtimeTarget);
		}

		// Колбеки катсцены/behaviour приходят позже — шаг за это время мог смениться
		// (чит сброса туториала), тогда продолжение отбрасывается
		private void AfterCutscene(TutorialStep step, Transform runtimeTarget) {
			if (currentStep != step)
				return;
			if (step.Behaviour == null) {
				ShowHint(step, runtimeTarget);
				return;
			}
			step.Behaviour.AfterCutscene(() => {
				if (currentStep == step)
					ShowHint(step, runtimeTarget);
			});
		}

		private void ShowHint(TutorialStep step, Transform runtimeTarget) {
			RectTransform root = step.AboveWindows && hintsRootTop != null ? hintsRootTop : hintsRoot;

			var worldTarget = step.WorldTarget != null ? step.WorldTarget : runtimeTarget;
			switch (step.AnchorMode) {
				case TutorialAnchorMode.World when worldTarget != null && trackingController != null: {
					var panel = trackingController.Track(HintKey(step), TrackingPanelType.Tutorial, worldTarget);
					if (panel != null && root != null) {
						// Init кэширует геометрию родителя (offsetMin, рект клампа) —
						// после пересадки пересчитываем от нового контейнера
						panel.transform.SetParent(root, false);
						panel.Init(worldTarget, root.rect.size, null);
					}
					if (panel is UI.Tutorial.TutorialHintPanel hint)
						hint.Setup(step, OnOkClicked);
					IsHintActive = panel != null;
					break;
				}
				case TutorialAnchorMode.Ui when uiHintPrefab != null && trackingController != null: {
					Transform parent = root != null ? root : trackingController.transform;
					activeUiHint = Instantiate(uiHintPrefab, parent);
					// uiAnchorId пуст — фолловер следит за рантайм-целью от условия показа
					activeUiHint.Init(step, runtimeTarget as RectTransform, OnOkClicked);
					IsHintActive = true;
					break;
				}
			}

			// Ни реплики, ни условий — шаг состоял из катсцены/behaviour и уже отработал
			if (!IsHintActive && step.CompleteConditions.Count == 0) {
				CompleteCurrentStep();
				return;
			}

			foreach (var condition in step.CompleteConditions) {
				condition?.Subscribe(OnStepSatisfied);
				// Шаг завершился синхронно в Subscribe — остальные не подписываем
				if (currentStep != step)
					return;
			}
		}

		// Кнопка «Ок» на пузыре (TutorialStep.okButton) — закрыть шаг, не выполняя действие
		private void OnOkClicked() {
			CompleteCurrentStep();
		}

		private void OnStepSatisfied(Transform _) {
			CompleteCurrentStep();
		}

		private void CompleteCurrentStep() {
			// Несколько условий могут сработать в один кадр — завершаем один раз
			if (currentStep == null || stepFinishing)
				return;
			stepFinishing = true;

			var finished = currentStep;
			CleanupCurrentStep();

			SavesManager.Profile.tutorial.CompleteStep(finished.Guid);
			SavesManager.Instance.SaveProfile();

			StartNextStep();
		}

		private void CleanupCurrentStep() {
			if (currentStep == null)
				return;

			foreach (var condition in currentStep.ShowConditions)
				condition?.Unsubscribe();
			foreach (var condition in currentStep.CompleteConditions)
				condition?.Unsubscribe();

			if (trackingController != null)
				trackingController.Untrack(HintKey(currentStep));
			if (activeUiHint != null) {
				Destroy(activeUiHint.gameObject);
				activeUiHint = null;
			}
			IsHintActive = false;
		}

		// Перезапуск последовательности с первого незавершённого по сейву шага
		// (читы: после сброса прогресса туториала не требует перезагрузки сцены)
		public void RestartFromSave() {
			CleanupCurrentStep();
			StartNextStep();
		}

		private static string HintKey(TutorialStep step) => "TutorialHint_" + step.Guid;

		// conversationId реплики шага — его человекочитаемый id;
		// propertyPath вида "steps.Array.data[3].line" -> steps[3]
		public string GetConversationId(string linePropertyPath) {
			var match = Regex.Match(linePropertyPath ?? "", @"steps\.Array\.data\[(\d+)\]");
			if (match.Success && int.TryParse(match.Groups[1].Value, out int index) && index >= 0 && index < steps.Count)
				return steps[index]?.Id;
			return null;
		}

#if UNITY_EDITOR
		private void OnValidate() {
			foreach (var step in steps)
				step?.EditorEnsureIds();
		}

		public void EditorSetSteps(System.Collections.Generic.List<TutorialStep> steps, UI.Tutorial.UiHintFollower uiHintPrefab,
			RectTransform hintsRoot = null, RectTransform hintsRootTop = null) {
			this.steps = steps ?? new System.Collections.Generic.List<TutorialStep>();
			this.uiHintPrefab = uiHintPrefab;
			this.hintsRoot = hintsRoot;
			this.hintsRootTop = hintsRootTop;
		}
#endif
	}
}
