using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.Marketing {
	[DisallowMultipleComponent]
	[AddComponentMenu("Marketing/Cinematic Camera Path")]
	public class CinematicCameraPath : MonoBehaviour {
		[Tooltip("Leave empty to use the main gameplay camera.")]
		public Camera targetCamera;
		[Tooltip("Optional subject to keep in frame. Otherwise use point rotations.")]
		public Transform lookAt;
		[Min(0.1f)] public float duration = 12f;
		public bool loop;
		public bool useUnscaledTime = true;
		public bool hideUI = true;
		public bool hideCursor = true;
		[Tooltip("Time to distance along the path. Linear gives constant speed; ease in/out gives a gentle start and finish.")]
		public AnimationCurve timing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		[Tooltip("Number of length samples per Bezier segment.")]
		[Range(16, 256)] public int samplesPerSegment = 64;

		private readonly List<CinematicCameraPoint> points = new List<CinematicCameraPoint>();
		private readonly List<float> distances = new List<float>();
		private readonly List<float> parameters = new List<float>();
		private readonly List<Canvas> hiddenCanvases = new List<Canvas>();
		private readonly List<Tween> pausedTweens = new List<Tween>();
		private Camera playingCamera;
		private CameraController controller;
		private InputController inputController;
		private bool savedControllerActive, savedControllerEnabled, savedInputEnabled, savedCursorVisible;
		private Vector3 savedLocalPosition;
		private Quaternion savedLocalRotation;
		private float savedFov, savedOrthoSize, elapsed;
		public static CinematicCameraPath Active { get; private set; }
		public bool IsPlaying => Active == this;
		public bool IsHolding { get; private set; }
		public IReadOnlyList<CinematicCameraPoint> Points => points;
		public int SegmentCount => Mathf.Max(0, points.Count - (loop ? 0 : 1));
		public float Length => distances.Count > 0 ? distances[distances.Count - 1] : 0f;

		// Child order in the Hierarchy is the shot order. Only direct children are waypoints.
		public void RefreshPoints() {
			points.Clear();
			foreach (Transform child in transform) {
				var point = child.GetComponent<CinematicCameraPoint>();
				if (point != null && point.isActiveAndEnabled)
					points.Add(point);
			}
		}

		public Vector3 GetHandle(int index, bool outgoing) {
			var point = points[index];
			if (!point.automaticHandles)
				return point.transform.TransformPoint(outgoing ? point.handleOut : point.handleIn);
			int previous = loop ? (index + points.Count - 1) % points.Count : Mathf.Max(0, index - 1);
			int next = loop ? (index + 1) % points.Count : Mathf.Min(points.Count - 1, index + 1);
			Vector3 tangent = (points[next].transform.position - points[previous].transform.position) / 6f;
			if (!loop && (index == 0 || index == points.Count - 1))
				tangent *= 2f;
			return point.transform.position + (outgoing ? tangent : -tangent);
		}

		public Vector3 EvaluateSegment(int segment, float t) {
			int next = (segment + 1) % points.Count;
			float u = 1f - t;
			return u * u * u * points[segment].transform.position
				+ 3f * u * u * t * GetHandle(segment, true)
				+ 3f * u * t * t * GetHandle(next, false)
				+ t * t * t * points[next].transform.position;
		}

		public bool Rebuild() {
			RefreshPoints();
			distances.Clear();
			parameters.Clear();
			if (points.Count < 2)
				return false;
			distances.Add(0f);
			parameters.Add(0f);
			Vector3 previous = points[0].transform.position;
			float length = 0f;
			int samples = Mathf.Clamp(samplesPerSegment, 16, 256);
			for (int segment = 0; segment < SegmentCount; segment++) {
				for (int i = 1; i <= samples; i++) {
					float t = i / (float)samples;
					Vector3 position = EvaluateSegment(segment, t);
					length += Vector3.Distance(previous, position);
					distances.Add(length);
					parameters.Add(segment + t);
					previous = position;
				}
			}
			return length > 0.001f;
		}

		public bool Evaluate(float progress, out Vector3 position, out Quaternion rotation, out float fov, out float orthoSize) {
			position = transform.position;
			rotation = transform.rotation;
			fov = 41f;
			orthoSize = 10f;
			if (points.Count < 2 || Length <= 0.001f || points.Exists(p => p == null))
				return false;
			float distance = Mathf.Clamp01(progress) * Length;
			int upper = distances.BinarySearch(distance);
			if (upper < 0) upper = ~upper;
			upper = Mathf.Clamp(upper, 1, distances.Count - 1);
			float fraction = Mathf.InverseLerp(distances[upper - 1], distances[upper], distance);
			float parameter = Mathf.Lerp(parameters[upper - 1], parameters[upper], fraction);
			int segment = Mathf.Min(Mathf.FloorToInt(parameter), SegmentCount - 1);
			float t = parameter - segment;
			int next = (segment + 1) % points.Count;
			position = EvaluateSegment(segment, t);
			// Ease orientation/lens at each waypoint so turning does not snap at a join.
			float blend = Mathf.SmoothStep(0f, 1f, t);
			rotation = Quaternion.Slerp(points[segment].transform.rotation, points[next].transform.rotation, blend);
			if (lookAt != null && (lookAt.position - position).sqrMagnitude > 0.0001f)
				rotation = Quaternion.LookRotation(lookAt.position - position, Vector3.up);
			fov = Mathf.Lerp(points[segment].fieldOfView, points[next].fieldOfView, blend);
			orthoSize = Mathf.Lerp(points[segment].orthographicSize, points[next].orthographicSize, blend);
			return true;
		}

		public bool Play() {
			if (!Application.isPlaying || !isActiveAndEnabled)
				return false;
			if (Cutscenes.CutsceneController.IsPlaying) {
				Debug.LogWarning("[Cinematic camera] Wait for the cutscene to finish.", this);
				return false;
			}
			var camera = targetCamera != null ? targetCamera : Camera.main;
			if (camera == null || !Rebuild()) {
				Debug.LogWarning("[Cinematic camera] A camera and at least two distinct child waypoints are required.", this);
				return false;
			}
			StopActive();
			playingCamera = camera;
			controller = FindObjectOfType<CameraController>();
			inputController = FindObjectOfType<InputController>();
			savedLocalPosition = camera.transform.localPosition;
			savedLocalRotation = camera.transform.localRotation;
			savedFov = camera.fieldOfView;
			savedOrthoSize = camera.orthographicSize;
			if (controller != null) {
				savedControllerActive = controller.active;
				savedControllerEnabled = controller.enabled;
				controller.ResetInertia();
				controller.active = false;
				controller.enabled = false;
				PauseTweens(controller.cameraContainer);
				PauseTweens(controller.localCamera);
			}
			PauseTweens(camera.transform);
			if (inputController != null) {
				savedInputEnabled = inputController.enabled;
				inputController.enabled = false;
			}
			if (hideUI) {
				foreach (var canvas in FindObjectsOfType<Canvas>()) {
					if (!canvas.enabled) continue;
					hiddenCanvases.Add(canvas);
					canvas.enabled = false;
				}
			}
			savedCursorVisible = Cursor.visible;
			if (hideCursor) Cursor.visible = false;
			Active = this;
			elapsed = 0f;
			IsHolding = false;
			Apply(0f);
			return true;
		}

		private void PauseTweens(Transform target) {
			if (target == null) return;
			var tweens = DOTween.TweensByTarget(target, true);
			if (tweens == null) return;
			foreach (var tween in tweens) {
				if (pausedTweens.Contains(tween)) continue;
				pausedTweens.Add(tween);
				tween.Pause();
			}
		}

		private void LateUpdate() {
			if (!IsPlaying) return;
			if (playingCamera == null || Input.GetKeyDown(KeyCode.Escape)
				|| Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.BackQuote)) {
				Stop();
				return;
			}
			if (!IsHolding) elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
			float normalizedTime = elapsed / Mathf.Max(0.1f, duration);
			if (loop) normalizedTime = Mathf.Repeat(normalizedTime, 1f);
			else if (normalizedTime >= 1f) IsHolding = true;
			float progress = timing == null || timing.length == 0 ? normalizedTime : timing.Evaluate(Mathf.Clamp01(normalizedTime));
			if (!Apply(IsHolding ? 1f : progress)) Stop();
		}

		private bool Apply(float progress) {
			if (!Evaluate(progress, out var position, out var rotation, out var fov, out var orthoSize))
				return false;
			playingCamera.transform.SetPositionAndRotation(position, rotation);
			playingCamera.fieldOfView = fov;
			playingCamera.orthographicSize = orthoSize;
			return true;
		}

		public static void StopActive() {
			if (Active != null) Active.Stop();
		}

		public void Stop() {
			if (!IsPlaying) return;
			if (playingCamera != null) {
				playingCamera.transform.localPosition = savedLocalPosition;
				playingCamera.transform.localRotation = savedLocalRotation;
				playingCamera.fieldOfView = savedFov;
				playingCamera.orthographicSize = savedOrthoSize;
			}
			if (controller != null) {
				controller.ResetInertia();
				controller.active = savedControllerActive;
				controller.enabled = savedControllerEnabled;
			}
			if (inputController != null) inputController.enabled = savedInputEnabled;
			foreach (var canvas in hiddenCanvases)
				if (canvas != null) canvas.enabled = true;
			hiddenCanvases.Clear();
			foreach (var tween in pausedTweens)
				if (tween.IsActive()) tween.Play();
			pausedTweens.Clear();
			Cursor.visible = savedCursorVisible;
			playingCamera = null;
			Active = null;
			IsHolding = false;
		}

		private void OnDisable() { Stop(); }
		private void OnDestroy() { Stop(); }

		private void OnDrawGizmos() {
			if (IsPlaying) return;
			RefreshPoints();
			if (points.Count < 2) return;
			Gizmos.color = new Color(0.2f, 0.85f, 1f);
			for (int segment = 0; segment < SegmentCount; segment++) {
				Vector3 previous = EvaluateSegment(segment, 0f);
				for (int i = 1; i <= 32; i++) {
					Vector3 position = EvaluateSegment(segment, i / 32f);
					Gizmos.DrawLine(previous, position);
					previous = position;
				}
			}
		}
	}
}
