using DG.Tweening;
using System;
using UnityEngine;

public class CameraController : Singleton<CameraController>
{
    [SerializeField] Bounds borderWorld;
	public float groundY = 0;

	public bool active = true;

#if CHEATS
	public bool LimitsDisabled { get; private set; }

	public void ToggleLimits() {
		LimitsDisabled = !LimitsDisabled;
		ResetInertia();
		if (!LimitsDisabled) {
			cameraContainer.position = ClampPosition(cameraContainer.position);
			if (trackingObject != null)
				UpdateTrackingObjectPosition();
		}
	}
#endif

	public float sensivity = 0.5f;
	public bool invertX = false;
	public bool invertY = false;
    public Transform cameraContainer;
	public Transform localCamera;
	public float zoomSensivity;
    
    private Vector3 lastPosition;
	private Vector3 startMousePos;

	public float inertiaDrag = 1f;
	private Vector3 prevPosition;
	private Vector3 velocity = Vector3.zero;

	bool scrollStarted = false;

	public float maxShakeDesgees;
	public float maxShakeMove;

	public float noiseSpeed;
	public float shakeDuration = 1f;
	public float cameraTrauma = 0f;
	private float noiseSeed = 0f;

	Vector3 originalRotation;
	private float noiseTime = 0f;

	private Vector3 origCameraPosition;
	private Vector3 origCameraRotation;

	private GameObject trackingObject;
	private Plane trackingPlane;
	private Plane landPlane = new Plane(Vector3.up, -0.3f);

	private Camera cam;
	private Plane groundPlane;
	void Start()
    {
		InputController.Instance.OnBeginDrag += OnBeginDrag;
		InputController.Instance.OnDrag += OnDrag;
		InputController.Instance.OnEndDrag += OnEndDrag;
		InputController.Instance.OnZoom += OnZoom;
		cam = GetComponent<Camera>();
		groundPlane = new Plane(Vector3.up, new Vector3(0, groundY, 0));
	}

	public void OnBeginDrag() {
		if (!active)
			return;

		lastPosition = cameraContainer.position;
		startMousePos = GetWorldPosition();
		prevPosition = cameraContainer.position;
		scrollStarted = true;
	}

	public void OnDrag() {
		if (!active)
			return;

		if (!scrollStarted)
			return;

		Vector3 direction = startMousePos - GetWorldPosition();
		direction.y = 0;
		prevPosition = cameraContainer.position;
		cameraContainer.position = ClampPosition(cameraContainer.position + direction);
	}

	private Vector3 GetWorldPosition() {
		Ray mousePos = cam.ScreenPointToRay(Input.mousePosition);
		groundPlane.Raycast(mousePos, out var distance);
		return mousePos.GetPoint(distance);
	}

	public void OnEndDrag() {
		if (!active)
			return;

		if (!scrollStarted)
			return;

		velocity = cameraContainer.position - prevPosition;
		if (velocity == Vector3.zero)
			scrollStarted = false;
	}

	public void Shake(float trauma) {
		if (!active)
			return;

		if (cameraTrauma == 0) {
			originalRotation = localCamera.localRotation.eulerAngles;
			if (noiseSeed == 0f)
				noiseSeed = UnityEngine.Random.Range(0f, 100f);
			noiseTime = UnityEngine.Random.Range(0f, 100f);
		}

		cameraTrauma += trauma;
		if (cameraTrauma > 1f)
			cameraTrauma = 1f;
	}

	public void OnZoom(float delta) {
		if (!active)
			return;

		Vector3 moveTo = cameraContainer.position + cameraContainer.forward * delta * zoomSensivity;
#if CHEATS
		if (LimitsDisabled) {
			cameraContainer.position = moveTo;
			UpdateTrackingObjectPosition();
			return;
		}
#endif
		if (borderWorld.Contains(moveTo)) {
			cameraContainer.position = moveTo;
		} else if (borderWorld.max.y > moveTo.y && borderWorld.min.y < moveTo.y) {
			cameraContainer.position = ClampPosition(moveTo);
		}
		UpdateTrackingObjectPosition();
	}

    void Update()
    {
		if (!active)
			return;

		if (velocity != Vector3.zero && scrollStarted) {
			cameraContainer.position = ClampPosition(cameraContainer.position + velocity);
			velocity = Vector3.MoveTowards(velocity, Vector3.zero, inertiaDrag * Time.deltaTime);
			if (velocity == Vector3.zero)
				scrollStarted = false;
		}

		if (cameraTrauma > 0) {
			cameraTrauma -= Time.deltaTime * (1f / shakeDuration);
			noiseTime += Time.deltaTime * noiseSpeed;
			noiseSeed += Time.deltaTime * noiseSpeed;
			float shake = cameraTrauma * cameraTrauma * cameraTrauma;
			Vector3 eulerAngles = new Vector3();
			eulerAngles.x = shake * maxShakeDesgees * (Mathf.PerlinNoise(noiseSeed, noiseTime) * 2f - 1f);
			eulerAngles.y = shake * maxShakeDesgees * (Mathf.PerlinNoise(noiseSeed + 10f, noiseTime) * 2f - 1f);
			eulerAngles.z = shake * maxShakeDesgees * (Mathf.PerlinNoise(noiseSeed + 20f, noiseTime) * 2f - 1f);

			localCamera.localRotation = Quaternion.Euler(eulerAngles);

			eulerAngles.x = shake * maxShakeMove * (Mathf.PerlinNoise(noiseSeed - 10f, noiseTime) * 2f - 1f);
			eulerAngles.y = shake * maxShakeMove * (Mathf.PerlinNoise(noiseSeed - 20f, noiseTime) * 2f - 1f);
			eulerAngles.z = shake * maxShakeMove * (Mathf.PerlinNoise(noiseSeed - 30f, noiseTime) * 2f - 1f);

			localCamera.localPosition = eulerAngles;

		}
	}

	private Vector3 ClampPosition(Vector3 pos) {
#if CHEATS
		if (LimitsDisabled)
			return pos;
#endif
		if(borderWorld.Contains(pos))
			return pos;

		return borderWorld.ClosestPoint(pos);
	}

	private void OnDrawGizmos() {
		Gizmos.DrawWireCube(borderWorld.center, borderWorld.extents * 2);
	}

	public void FadeIn(float duration) {
		//ехать назад
		active = false;
		velocity = Vector3.zero;
		scrollStarted = false;
		Vector3 finalPos = transform.position + transform.forward * -80f;
		cameraContainer.DOMove(finalPos, duration).OnUpdate(() => { UpdateTrackingObjectPosition(); });
	}

	public void FadeOut(float duration) {
		//ехать вперед

		Vector3 direction = transform.forward;
		float verticalComponent = Vector3.Dot(direction, Vector3.up);
		float t = borderWorld.max.y / verticalComponent;
		Vector3 finalPos = trackingObject.transform.position + t * direction;

		cameraContainer.DOMove(finalPos, duration).OnUpdate(() => { UpdateTrackingObjectPosition(); }).OnComplete(() => { active = true; });
	}


	public Tween AnimateToPosition(Vector3 positionOnLand, float zoomLevel, float time, Action onComplete, bool clamp = true) {
		Vector3 finalPos = GetPositionOverLand(positionOnLand, zoomLevel, clamp);
		return cameraContainer.DOMove(finalPos, time).OnUpdate(() => { UpdateTrackingObjectPosition(); }).OnComplete(() => onComplete?.Invoke());
	}

	// Instant counterpart of AnimateToPosition (cutscene skip / restoring saved state).
	public void JumpToPosition(Vector3 positionOnLand, float zoomLevel, bool clamp = true) {
		cameraContainer.position = GetPositionOverLand(positionOnLand, zoomLevel, clamp);
	}

	// clamp = false: point may lie outside borderWorld (cutscenes over open sea).
	private Vector3 GetPositionOverLand(Vector3 positionOnLand, float zoomLevel, bool clamp = true) {
		float maxHeight = borderWorld.max.y;
		float minHeight = borderWorld.min.y;
		float camHeight = Mathf.Lerp(minHeight, maxHeight, zoomLevel);

		Vector3 direction = transform.forward;
		float verticalComponent = Vector3.Dot(direction, Vector3.up);
		float t = camHeight / verticalComponent;
		Vector3 finalPos = positionOnLand + t * direction;

		if (clamp && borderWorld.max.y > finalPos.y && borderWorld.min.y < finalPos.y) {
			finalPos = ClampPosition(finalPos);
		}
		return finalPos;
	}

	// Drop leftover pan inertia/drag state. Cutscenes disable input (active = false),
	// but velocity keeps decaying and would resume the pan after the cutscene ends.
	public void ResetInertia() {
		velocity = Vector3.zero;
		scrollStarted = false;
	}

	public void MoveToPositionOnLand(Vector3 positionOnLand, bool clamp = false) {
		Vector3 direction = transform.forward;
		float verticalComponent = Vector3.Dot(direction, Vector3.up);
		float t = transform.position.y / verticalComponent;
		Vector3 finalPos = positionOnLand + t * direction;

		if (clamp && borderWorld.max.y > finalPos.y && borderWorld.min.y < finalPos.y) {
			finalPos = ClampPosition(finalPos);
		}
		cameraContainer.position = finalPos;
		UpdateTrackingObjectPosition();
	}

	public void MoveToLevelAndEnable(Vector3 position, Vector3 level, Vector3 rotation, float time) {
		origCameraPosition = cameraContainer.transform.position;
		origCameraRotation = cameraContainer.transform.eulerAngles;

		cameraContainer.transform.position = position;
		cameraContainer.transform.eulerAngles = rotation;
		UpdateTrackingObjectPosition();
		Vector3 offset = cameraContainer.position - trackingObject.transform.position;

		cameraContainer.transform.position = origCameraPosition;
		cameraContainer.transform.eulerAngles = origCameraRotation;

		Vector3 finalPos = level + offset;
		if (borderWorld.max.y > finalPos.y && borderWorld.min.y < finalPos.y) {
			finalPos = ClampPosition(finalPos);
		}
		MoveToPositionAndEnable(finalPos, rotation, time);
	}

	public void MoveToPositionAndEnable(Vector3 position, Vector3 rotation, float time) {
		origCameraPosition = cameraContainer.transform.position;
		origCameraRotation = cameraContainer.transform.eulerAngles;
		cameraContainer.DOMove(position, time).OnUpdate(() => { UpdateTrackingObjectPosition(); });
		cameraContainer.DORotate(rotation, time).OnComplete(() => { active = true; });
	}

	public void RevertCamera() {
		cameraContainer.DOMove(origCameraPosition, 1f).OnUpdate(() => { UpdateTrackingObjectPosition(); });
		cameraContainer.DORotate(origCameraRotation, 1f).OnComplete(() => { active = false; });
	}

	public GameObject CreateTrackingObject(float y) {
		trackingObject = new GameObject("tracking");
		trackingObject.transform.SetParent(cameraContainer);
		trackingPlane = new Plane(Vector3.up, -y);
		UpdateTrackingObjectPosition();
		return trackingObject;
	}

	private void UpdateTrackingObjectPosition() {
		Ray r = new Ray(cameraContainer.position, /*Quaternion.Euler(20f, 0, 0) * */cameraContainer.forward);
		if (trackingPlane.Raycast(r, out var enter)) {
			trackingObject.transform.position = r.GetPoint(enter);
		}
	}

	public Vector3 GetPointOnLand(Vector2 screenPosition) {
		Ray r = cam.ScreenPointToRay(screenPosition);
		if (landPlane.Raycast(r, out var enter)) {
			return r.GetPoint(enter);
		}

		return Vector3.zero;
	}
}