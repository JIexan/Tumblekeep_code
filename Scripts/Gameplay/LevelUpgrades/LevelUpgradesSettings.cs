using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.LevelUpgrades {
	// Реестр конфигов апгрейдов по уровням. Уровень без конфига — система прозрачна:
	// все башни доступны, бонусов нет, точки постройки живут по своему startUnlocked.
	[CreateAssetMenu(fileName = "LevelUpgradesSettings", menuName = "Settings/LevelUpgradesSettings")]
	public class LevelUpgradesSettings : ScriptableObject {
		[SerializeField] List<LevelUpgradesConfig> configs = new();

		public List<LevelUpgradesConfig> Configs => configs;

		public LevelUpgradesConfig GetConfig(string levelId) {
			if (string.IsNullOrEmpty(levelId))
				return null;
			foreach (var config in configs) {
				if (config != null && config.LevelId == levelId)
					return config;
			}
			return null;
		}

#if UNITY_EDITOR
		private void OnValidate() {
			var seen = new HashSet<string>();
			foreach (var config in configs) {
				if (config == null) {
					Debug.LogWarning($"[LevelUpgradesSettings] '{name}': null-элемент в списке конфигов.", this);
					continue;
				}
				if (string.IsNullOrEmpty(config.LevelId))
					Debug.LogWarning($"[LevelUpgradesSettings] '{name}': у конфига '{config.name}' пустой levelId.", this);
				else if (!seen.Add(config.LevelId))
					Debug.LogWarning($"[LevelUpgradesSettings] '{name}': дублирующийся levelId '{config.LevelId}'.", this);
			}
		}
#endif
	}
}
