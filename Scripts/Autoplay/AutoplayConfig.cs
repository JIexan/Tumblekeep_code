#if AUTOPLAY
using System;
using UnityEngine;

namespace Autoplay {
	/// <summary>
	/// Настройки серии прогонов автоплей-бота. Сериализуется в JSON (окно редактора → EditorPrefs → бутстрап)
	/// и копируется в папку сессии рядом со статистикой.
	/// </summary>
	[Serializable]
	public class AutoplayConfig {
		public string levelId = "Level1";
		public string sceneName = "1_lvl_summer";
		public int runs = 20;
		public float timeScale = 5f;
		// 0 = случайный; фактический seed фиксируется на сессию и пишется в статистику
		public int seed = 0;
		// Страховка от бесконечных прогонов: прогон завершается на старте волны maxWave
		// или по игровому времени. 0 = без лимита (до смерти замка)
		public int maxWave = 0;
		public float maxGameSeconds = 0f;

		// Интервал принятия решений в игровых секундах (т.е. уже с учётом timeScale)
		public float decisionInterval = 0.5f;
		public bool rushWaves = true;
		// Пауза после зачистки волны перед нажатием на таймер следующей
		public float rushDelay = 1f;
		public float merchantChance = 0.5f;

		public float spellMinInterval = 6f;
		public int maxSpellsPerWave = 3;
		// Дистанция до конца маршрута, с которой утёкший юнит считается «у замка»
		public float leakDistance = 20f;
		public float healThreshold = 0.5f;
		public float protectMinIncomingDamage = 15f;

		public string outputFolder = "";
		public bool verbose = false;

		// Режимные флаги: серия из окна редактора / одиночный прогон из читов
		public bool loadScene = true;
		public bool autoRestart = true;
		public bool exitPlayModeWhenDone = true;

		public void Validate() {
			runs = Mathf.Max(1, runs);
			maxWave = Mathf.Max(0, maxWave);
			maxGameSeconds = Mathf.Max(0f, maxGameSeconds);
			timeScale = Mathf.Clamp(timeScale, 0.1f, 20f);
			decisionInterval = Mathf.Clamp(decisionInterval, 0.05f, 5f);
			rushDelay = Mathf.Max(0f, rushDelay);
			merchantChance = Mathf.Clamp01(merchantChance);
			spellMinInterval = Mathf.Max(0f, spellMinInterval);
			maxSpellsPerWave = Mathf.Max(0, maxSpellsPerWave);
			leakDistance = Mathf.Max(1f, leakDistance);
			healThreshold = Mathf.Clamp01(healThreshold);
			protectMinIncomingDamage = Mathf.Max(0f, protectMinIncomingDamage);
		}

		public AutoplayConfig Clone() => (AutoplayConfig)MemberwiseClone();
	}
}
#endif
