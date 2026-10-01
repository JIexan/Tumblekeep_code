using UnityEngine;

namespace Gameplay.Relics {
	// «Золотой ночной горшок»: каждый n-й «наградной» килл приносит золото ×coinsMultiplier.
	// Не ITriggeredRelicEffector: потребляется RelicsManager.ModifyKillCoins (счётчик киллов
	// живёт в менеджере), т.к. должен изменить сумму монет ДО начисления.
	[System.Serializable]
	public class NthKillCoinsRelicEffector : BaseRelicEffector {
		[SerializeField] int n = 20;
		[SerializeField] float coinsMultiplier = 10f;

		public int N => n;
		public float CoinsMultiplier => coinsMultiplier;

#if UNITY_EDITOR
		public void EditorInit(int n, float coinsMultiplier) {
			this.n = n;
			this.coinsMultiplier = coinsMultiplier;
		}
#endif
	}
}
