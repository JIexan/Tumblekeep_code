using System;
using Gameplay.LevelUpgrades;
using Gameplay.RestorationSystem;
using Gameplay.Unlocks;
using UnityEngine;

namespace Gameplay.Tutorial {
	// Логика шага, которую не выразить условиями показа/закрытия. Вызывается менеджером:
	// ShouldSkip — сразу после срабатывания условия показа (до катсцены);
	// AfterCutscene — после катсцены, перед репликой (onDone обязателен, иначе шаг зависнет).
	// Новый тип: отнаследоваться и выбрать в поле behaviour шага (дропдаун даёт Odin).
	[Serializable]
	public abstract class TutorialStepBehaviour {
		// Учить уже нечему — шаг закрывается молча (с записью в сейв), без катсцены и реплики
		public virtual bool ShouldSkip() => false;

		public virtual void AfterCutscene(Action onDone) {
			onDone?.Invoke();
		}
	}

	// Подарок кристаллов «на первый апгрейд острова»: игроку должно хватать на апгрейд upgradeId
	// уровня levelId ПЛЮС на самый дешёвый доступный шаг зоны region (апгрейд открывается после
	// любого одного шага зоны). Хватает — шаг пропускается; не хватает — после катсцены-находки
	// начисляется недостача (не меньше minGift, чтобы сундук не выглядел издёвкой) и показывается
	// попап «получены кристаллы». Кристаллы сохраняются до попапа: закрыли игру на попапе —
	// в следующий раз шаг пропустится по ShouldSkip.
	[Serializable]
	public class UpgradeCrystalsGiftBehaviour : TutorialStepBehaviour {
		[SerializeField] string levelId = "Level1";
		[SerializeField] string upgradeId = "TowerPlace1";
		[SerializeField] RegionId region = RegionId.Dock;
		[SerializeField] int minGift = 3;

		public override bool ShouldSkip() {
			// Апгрейд уже куплен (старый сейв) — дарить не на что
			return LevelUpgradesManager.IsPurchased(levelId, upgradeId) || GetShortfall() <= 0;
		}

		public override void AfterCutscene(Action onDone) {
			int shortfall = GetShortfall();
			if (shortfall <= 0) {
				onDone?.Invoke();
				return;
			}

			int gift = Mathf.Max(shortfall, minGift);
			SavesManager.Profile.AddItem(ItemType.Crystal, gift);
			SavesManager.Instance.SaveProfile();

			ContentUnlockAnnouncer.Enqueue(ContentUnlock.ForItem(ItemType.Crystal, gift));
			ContentUnlockAnnouncer.ShowPending(onDone);
		}

		// Сколько кристаллов не хватает на «шаг зоны + апгрейд»; 0 и меньше — хватает
		private int GetShortfall() {
			int need = GetUpgradeCrystalCost() + GetCheapestStepCost();
			return need - SavesManager.Profile.GetItem(ItemType.Crystal);
		}

		private int GetUpgradeCrystalCost() {
			var upgrade = LevelUpgradesManager.GetConfig(levelId)?.GetUpgrade(upgradeId);
			if (upgrade == null) {
				Debug.LogWarning($"[UpgradeCrystalsGift] Нет апгрейда '{upgradeId}' у уровня '{levelId}'.");
				return 0;
			}
			int cost = 0;
			foreach (var requirement in upgrade.Requirements) {
				if (requirement is ItemUpgradeRequirement item && item.Item == ItemType.Crystal)
					cost += item.Count;
			}
			return cost;
		}

		private int GetCheapestStepCost() {
			var controller = PlayerProgressController.Instance;
			var step = controller != null ? controller.GetCheapestFrontierStep(region) : null;
			return step != null ? step.Cost : 0;
		}

#if UNITY_EDITOR
		public void EditorInit(string levelId, string upgradeId, RegionId region, int minGift) {
			this.levelId = levelId;
			this.upgradeId = upgradeId;
			this.region = region;
			this.minGift = minGift;
		}
#endif
	}
}
