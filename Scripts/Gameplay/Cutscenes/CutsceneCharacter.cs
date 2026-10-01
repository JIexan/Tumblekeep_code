using UnityEngine;

namespace Gameplay.Cutscenes {
	// Сюжетный персонаж в мире (модель + Animator). Модели временные (капсулы) —
	// при появлении финальных меняется только содержимое префаба, интерфейс тот же.
	// Ожидаемые параметры аниматора: bool "Walk", bool "Talk" (+ произвольные триггеры);
	// отсутствующий параметр просто игнорируется — заглушки живут без клипов.
	public class CutsceneCharacter : MonoBehaviour {
		[SerializeField] CharacterId characterId;
		[SerializeField] Animator animator;

		public CharacterId Id => characterId;

		public void SetWalking(bool value) => SetBool("Walk", value);
		public void SetTalking(bool value) => SetBool("Talk", value);

		public void SetTrigger(string name) {
			if (animator != null && HasParameter(name))
				animator.SetTrigger(name);
		}

		// Мгновенный поворот к точке (по Y)
		public void FaceInstant(Vector3 worldPoint) {
			Vector3 direction = worldPoint - transform.position;
			direction.y = 0f;
			if (direction.sqrMagnitude > 0.0001f)
				transform.rotation = Quaternion.LookRotation(direction);
		}

		private void SetBool(string name, bool value) {
			if (animator != null && HasParameter(name))
				animator.SetBool(name, value);
		}

		private bool HasParameter(string name) {
			foreach (var p in animator.parameters) {
				if (p.name == name)
					return true;
			}
			return false;
		}
	}
}
