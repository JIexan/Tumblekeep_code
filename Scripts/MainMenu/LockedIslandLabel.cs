using UnityEngine;

// Надпись «пока недоступно» над закрытым островом на карте: мировой текст, всегда развёрнут к камере.
// Префаб собирает Tools/Levels/Build Locked Island Label, инстансит GlobalMapController
public class LockedIslandLabel : MonoBehaviour {
	private Transform cameraTransform;

	private void LateUpdate() {
		if (cameraTransform == null) {
			if (Camera.main == null)
				return;
			cameraTransform = Camera.main.transform;
		}
		transform.rotation = cameraTransform.rotation;
	}
}
