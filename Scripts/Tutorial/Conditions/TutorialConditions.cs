using System;
using DG.Tweening;
using Gameplay.RestorationSystem;
using UnityEngine;

namespace Gameplay.Tutorial {
	// Условия шага туториала. Используются в двух ролях: showConditions (когда показать
	// реплику) и completeConditions (когда её снять и записать шаг в сейв). Событийные
	// (не поллинг): Subscribe вешается на шину/события, Unsubscribe обязателен — менеджер
	// зовёт его при завершении шага и при уничтожении. Семантика на шаге — ANY:
	// первое сработавшее условие срабатывает за весь список.
	// Satisfy(Transform) может отдать рантайм-объект (башню, сундук) — менеджер
	// использует его как цель world-реплики, когда worldTarget шага пуст.
	// Условие обязано корректно срабатывать и на уже-истинное состояние прямо
	// в Subscribe (шаг мог стать текущим позже события).
	// Новый тип: отнаследоваться, добавить в [SerializeReference]-список шага
	// в инспекторе (дропдаун даёт Odin).
	[System.Serializable]
	public abstract class TutorialCondition {
		protected Action<Transform> onSatisfied;

		public virtual void Subscribe(Action<Transform> onSatisfied) {
			this.onSatisfied = onSatisfied;
		}

		public abstract void Unsubscribe();

		protected void Satisfy() {
			onSatisfied?.Invoke(null);
		}

		protected void Satisfy(Transform target) {
			onSatisfied?.Invoke(target);
		}
	}

	// Открыто окно определённого типа
	[System.Serializable]
	public class WindowOpenedCondition : TutorialCondition {
		[SerializeField] WindowType window;

		public WindowOpenedCondition() { }
		public WindowOpenedCondition(WindowType window) { this.window = window; }

		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnWindowOpened, OnEvent);
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnWindowOpened, OnEvent);
		}

		private void OnEvent(object obj) {
			if (obj is WindowType type && type == window)
				Satisfy();
		}
	}

	// Построена любая башня
	[System.Serializable]
	public class TowerBuiltCondition : TutorialCondition {
		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnTowerBuilt, OnEvent);
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnTowerBuilt, OnEvent);
		}

		private void OnEvent(object obj) {
			Satisfy(obj is Component tower ? tower.transform : null);
		}
	}

	// Началась волна (minWave = 0 — любая)
	[System.Serializable]
	public class WaveStartedCondition : TutorialCondition {
		[SerializeField] int minWave;

		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnWaveStarted, OnEvent);
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnWaveStarted, OnEvent);
		}

		private void OnEvent(object obj) {
			if (minWave <= 0 || (obj is int wave && wave >= minWave))
				Satisfy();
		}
	}

	// Завершён шаг восстановления (пустой guid — любой)
	[System.Serializable]
	public class QuestStepCompletedCondition : TutorialCondition {
		[SerializeField] string stepGuid;

		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnQuestCompleted, OnEvent);
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnQuestCompleted, OnEvent);
		}

		private void OnEvent(object obj) {
			if (string.IsNullOrEmpty(stepGuid) || (obj is ProgressStep step && step.Guid == stepGuid))
				Satisfy();
		}
	}

	// Клик по UI-элементу с TutorialUiAnchor (тому же, к которому привязана реплика)
	[System.Serializable]
	public class AnchorClickedCondition : TutorialCondition {
		[SerializeField] string anchorId;

		public AnchorClickedCondition() { }
		public AnchorClickedCondition(string anchorId) { this.anchorId = anchorId; }

		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			TutorialUiAnchor.OnAnchorClicked += OnClicked;
		}

		public override void Unsubscribe() {
			TutorialUiAnchor.OnAnchorClicked -= OnClicked;
		}

		private void OnClicked(string id) {
			if (string.IsNullOrEmpty(anchorId) || id == anchorId)
				Satisfy();
		}
	}

	// Любой клик по миру (клики над UI InputController не репортит)
	[System.Serializable]
	public class TapAnywhereCondition : TutorialCondition {
		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			if (InputController.Instance != null)
				InputController.Instance.OnClick += Satisfy;
		}

		public override void Unsubscribe() {
			if (InputController.Instance != null)
				InputController.Instance.OnClick -= Satisfy;
		}
	}

	// По таймеру (unscaled — окна могут ставить паузу)
	[System.Serializable]
	public class TimerCondition : TutorialCondition {
		[SerializeField] float seconds = 5f;

		private Tween tween;

		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			tween = DOVirtual.DelayedCall(seconds, Satisfy, true);
		}

		public override void Unsubscribe() {
			tween?.Kill();
			tween = null;
		}
	}

	// Клик по свободному месту постройки (открылась панель выбора башни)
	[System.Serializable]
	public class BuildSpotClickedCondition : TutorialCondition {
		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnBuildSpotClicked, OnEvent);
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnBuildSpotClicked, OnEvent);
		}

		private void OnEvent(object obj) {
			Satisfy(obj is Component spot ? spot.transform : null);
		}
	}

	// Какая-то башня набрала XP на апгрейд. События «стало можно» нет — проверяем
	// состояние при подписке и после каждого килла/постройки (XP растёт только там).
	// Отдаёт цель — башню, готовую к усилению.
	[System.Serializable]
	public class TowerUpgradeReadyCondition : TutorialCondition {
		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnUnitKilled, OnEvent);
			InGameEventManager.Subscribe(InGameEvents.OnTowerBuilt, OnEvent);
			Check();
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnUnitKilled, OnEvent);
			InGameEventManager.Unsubscribe(InGameEvents.OnTowerBuilt, OnEvent);
		}

		private void OnEvent(object obj) {
			Check();
		}

		private void Check() {
			var gc = GameController.Instance;
			if (gc == null)
				return;
			foreach (var tower in gc.GetAllTowers()) {
				if (tower != null && tower.towerType != TowerType.Placeholder && tower.CanUpgrade) {
					Satisfy(tower.transform);
					return;
				}
			}
		}
	}

	// На поле выпал наградной сундук. Отдаёт цель — сундук (при подписке — первый из уже лежащих).
	[System.Serializable]
	public class ChestDroppedCondition : TutorialCondition {
		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnChestDropped, OnEvent);
			Check();
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnChestDropped, OnEvent);
		}

		private void OnEvent(object obj) {
			Satisfy(obj is Component chest ? chest.transform : null);
		}

		private void Check() {
			var chests = GameController.Instance != null ? GameController.Instance.Chests : null;
			if (chests == null || chests.ActiveChests == null)
				return;
			foreach (var chest in chests.ActiveChests) {
				if (chest != null) {
					Satisfy(chest.transform);
					return;
				}
			}
		}
	}

	// У игрока появился свиток. Отдаёт цель — строку первого свитка в HUD (RectTransform):
	// к ней привязывается UI-реплика шага с пустым uiAnchorId. Строку спрашиваем у
	// SpellsPanelController с созданием — порядок подписчиков OnItemCountChanged не
	// определён, контроллер мог ещё не успеть её завести.
	[System.Serializable]
	public class SpellObtainedCondition : TutorialCondition {
		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnItemCountChanged, OnEvent);
			Check();
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnEvent);
		}

		private void OnEvent(object obj) {
			if (obj is ItemType type && SaveDataProfile.IsSpell(type))
				Check();
		}

		private void Check() {
			var profile = SavesManager.Profile;
			if (profile == null)
				return;
			var owned = profile.GetOwnedSpellsInOrder();
			if (owned.Count == 0)
				return;
			var controller = UnityEngine.Object.FindObjectOfType<SpellsPanelController>();
			var panel = controller != null ? controller.GetOrCreatePanel(owned[0]) : null;
			if (panel != null)
				Satisfy(panel.transform);
		}
	}

	// Игрок потащил свиток из HUD (drag всегда заканчивается кастом — отмены нет).
	// Реплику снимаем уже на старте, чтобы пузырь не закрывал поле при прицеливании.
	[System.Serializable]
	public class SpellDragStartedCondition : TutorialCondition {
		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnSpellDragStarted, OnEvent);
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnSpellDragStarted, OnEvent);
		}

		private void OnEvent(object obj) {
			Satisfy();
		}
	}

	// Окно закрыто штатно (крестик/клик мимо). HideWindow/HideAllWindows событие не шлют —
	// держать рядом страховочное условие, если окно могут погасить в обход
	[System.Serializable]
	public class WindowClosedCondition : TutorialCondition {
		[SerializeField] WindowType window;

		public WindowClosedCondition() { }
		public WindowClosedCondition(WindowType window) { this.window = window; }

		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnWindowClosed, OnEvent);
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnWindowClosed, OnEvent);
		}

		private void OnEvent(object obj) {
			if (obj is WindowType type && type == window)
				Satisfy();
		}
	}

	// Хаб: игрок уже побывал в бою (флаг SaveDataTutorial.battleVisited ставит GameController)
	// и зоны хаба догружены — фронтир шагов известен (нужен подарку кристаллов и реплике таска).
	[System.Serializable]
	public class ReturnedFromBattleCondition : TutorialCondition {
		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnHubProgressReady, OnEvent);
			Check();
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnHubProgressReady, OnEvent);
		}

		private void OnEvent(object obj) {
			Check();
		}

		private void Check() {
			var tutorial = SavesManager.Profile?.tutorial;
			var hub = PlayerProgressController.Instance;
			if (tutorial != null && tutorial.battleVisited && hub != null && hub.IsInitialized)
				Satisfy();
		}
	}

	// Хаб: в зоне есть шаг восстановления, на который хватает кристаллов. Отдаёт цель —
	// самый дешёвый такой шаг (над ним висит панелька таска): после него у игрока останется
	// больше всего кристаллов на апгрейд острова.
	[System.Serializable]
	public class QuestAffordableCondition : TutorialCondition {
		[SerializeField] RegionId region = RegionId.Dock;

		public QuestAffordableCondition() { }
		public QuestAffordableCondition(RegionId region) { this.region = region; }

		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnHubProgressReady, OnEvent);
			InGameEventManager.Subscribe(InGameEvents.OnQuestCompleted, OnEvent);
			InGameEventManager.Subscribe(InGameEvents.OnItemCountChanged, OnEvent);
			Check();
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnHubProgressReady, OnEvent);
			InGameEventManager.Unsubscribe(InGameEvents.OnQuestCompleted, OnEvent);
			InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnEvent);
		}

		private void OnEvent(object obj) {
			Check();
		}

		private void Check() {
			var hub = PlayerProgressController.Instance;
			if (hub == null || !hub.IsInitialized)
				return;
			var step = hub.GetCheapestFrontierStep(region);
			if (step != null && step.CanComplete())
				Satisfy(step.transform);
		}
	}

	// Хаб: клик по панельке таска — открылся попап «Выполнить» (он встаёт ровно туда,
	// где висит реплика, поэтому реплику снимаем уже по клику, а не по выполнению)
	[System.Serializable]
	public class QuestPopupOpenedCondition : TutorialCondition {
		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnQuestPopupOpened, OnEvent);
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnQuestPopupOpened, OnEvent);
		}

		private void OnEvent(object obj) {
			Satisfy();
		}
	}

	// Апгрейд острова можно купить прямо сейчас (все требования, включая цену). Требования
	// читают сейв, поэтому проверяем при подписке и на всё, что их двигает: шаг зоны,
	// кристаллы, опыт острова.
	[System.Serializable]
	public class UpgradeAvailableCondition : TutorialCondition {
		[SerializeField] string levelId = "Level1";
		[SerializeField] string upgradeId;

		public UpgradeAvailableCondition() { }
		public UpgradeAvailableCondition(string levelId, string upgradeId) {
			this.levelId = levelId;
			this.upgradeId = upgradeId;
		}

		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnQuestCompleted, OnEvent);
			InGameEventManager.Subscribe(InGameEvents.OnItemCountChanged, OnEvent);
			InGameEventManager.Subscribe(InGameEvents.OnIslandXPChanged, OnEvent);
			Check();
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnQuestCompleted, OnEvent);
			InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnEvent);
			InGameEventManager.Unsubscribe(InGameEvents.OnIslandXPChanged, OnEvent);
		}

		private void OnEvent(object obj) {
			Check();
		}

		private void Check() {
			if (Gameplay.LevelUpgrades.LevelUpgradesManager.CanPurchase(levelId, upgradeId))
				Satisfy();
		}
	}

	// Клик по наградному сундуку (открылась панель покупки)
	[System.Serializable]
	public class ChestClickedCondition : TutorialCondition {
		public override void Subscribe(Action<Transform> onSatisfied) {
			base.Subscribe(onSatisfied);
			InGameEventManager.Subscribe(InGameEvents.OnChestClicked, OnEvent);
		}

		public override void Unsubscribe() {
			InGameEventManager.Unsubscribe(InGameEvents.OnChestClicked, OnEvent);
		}

		private void OnEvent(object obj) {
			Satisfy(obj is Component chest ? chest.transform : null);
		}
	}
}
