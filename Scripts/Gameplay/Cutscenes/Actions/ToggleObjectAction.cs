using System;
using UnityEngine;

namespace Gameplay.Cutscenes.Actions {
	// Включить/выключить объект сцены (показать персонажа перед катсценой, спрятать после).
	[System.Serializable]
	public class ToggleObjectAction : BaseCutsceneAction {
		[SerializeField] GameObject target;
		[SerializeField] bool active = true;

		public override void Apply() {
			if (target != null)
				target.SetActive(active);
		}

#if UNITY_EDITOR
		public void EditorInit(GameObject target, bool active) {
			this.target = target;
			this.active = active;
		}
#endif
	}
}
