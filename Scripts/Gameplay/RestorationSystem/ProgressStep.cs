using Gameplay.RestorationSystem.Actions;
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.RestorationSystem {
	public class ProgressStep : MonoBehaviour {
		[SerializeField] string id;
		[SerializeField] int cost;
		[SerializeReference] List<BaseProgressAction> actions = new();

		// Graph plumbing (edited by the Progress Graph window, hidden from the node inspector).
		// guid is the save identity — never reuse it between steps, renaming id must not touch it.
		[SerializeField, HideInInspector] string guid;
		[SerializeField, HideInInspector] List<ProgressStep> prerequisites = new();
		[SerializeField, HideInInspector] Vector2 nodePosition;

		public string Title => "ProgressStepTitle" + id;
		public int Cost => cost;
		public ProgressRegion Region => parent;
		public string Guid => guid;
		public List<ProgressStep> Prerequisites => prerequisites;
		public Vector2 NodePosition => nodePosition;

		// Шаг открывает контент/зону (есть Unlock*-экшен) — окно заданий вешает на него индикатор
		public bool HasUnlocks {
			get {
				foreach (var action in actions) {
					if (action is IUnlockAction)
						return true;
				}
				return false;
			}
		}

		private ProgressRegion parent;

#if UNITY_EDITOR
		private void OnValidate() {
			// Covers AddComponent / Step Builder. Ctrl+D duplication keeps the source guid —
			// that case is handled by ProgressGraphController.EnsureUniqueGuids.
			if (string.IsNullOrEmpty(guid))
				guid = System.Guid.NewGuid().ToString("N");
		}
#endif

		public void SetParent(ProgressRegion region) {
			parent = region;
		}

		// Instantly apply the step's final state (used when restoring saved progress).
		public void Apply() {
			foreach (var action in actions) {
				action?.Apply();
			}
		}

		public void CompleteWithEffects() {
			SavesManager.Profile.SpendItem(ItemType.Crystal, cost);
			CameraController.Instance.AnimateToPosition(transform.position, 0f, 1f, () => {
				PlayActions(() => {
					SavesManager.Profile.playerLocation.CompleteStep(parent.Id, guid);
					SavesManager.Instance.SaveProfile();
					InGameEventManager.SendEvent(InGameEvents.OnQuestCompleted, this);
					// Попапы «контент открыт» — после всех экшенов шага, т.е. после его катсцены, если она есть
					Gameplay.Unlocks.ContentUnlockAnnouncer.ShowPending();
				});
			});
		}

		private void PlayActions(Action onAllComplete) {
			if (actions == null || actions.Count == 0) {
				onAllComplete?.Invoke();
				return;
			}

			int remaining = actions.Count;
			void OnOneDone() {
				remaining--;
				if (remaining == 0)
					onAllComplete?.Invoke();
			}

			// Launch actions in order, each shifted by its StartDelay relative to the
			// previous one — a staggered "chain". The step still finishes only once
			// every action has reported completion (remaining hits zero).
			float chainDelay = 0f;
			foreach (var action in actions) {
				if (action == null) {
					OnOneDone();
					continue;
				}

				chainDelay += Mathf.Max(0f, action.StartDelay);
				var current = action;
				if (chainDelay > 0f)
					DOVirtual.DelayedCall(chainDelay, () => current.Play(OnOneDone), false);
				else
					current.Play(OnOneDone);
			}
		}

		public void OnClick() {
			InGameEventManager.SendEvent(InGameEvents.OnQuestClicked, this);
		}

		public bool CanComplete() {
			return SavesManager.Profile.HasItem(ItemType.Crystal, cost);
		}
	}
}
