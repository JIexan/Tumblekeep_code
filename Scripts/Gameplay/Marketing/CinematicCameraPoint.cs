using UnityEngine;

namespace Gameplay.Marketing {
	[DisallowMultipleComponent]
	[AddComponentMenu("Marketing/Cinematic Camera Point")]
	public class CinematicCameraPoint : MonoBehaviour {
		[Tooltip("Automatically make a smooth curve through neighbouring points. Disable to edit Bezier handles.")]
		public bool automaticHandles = true;
		[Tooltip("Bezier handle offsets in this point's local coordinates.")]
		public Vector3 handleIn = Vector3.back * 3f;
		public Vector3 handleOut = Vector3.forward * 3f;
		[Range(1f, 179f)] public float fieldOfView = 41f;
		[Min(0.01f)] public float orthographicSize = 10f;
	}
}
