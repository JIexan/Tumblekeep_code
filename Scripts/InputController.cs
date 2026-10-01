using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputController : Singleton<InputController>
{
	public float clickDelay = 0.3f;
	public float dragThreshold = 0.3f;

	public Action OnClick;
	public Action OnBeginDrag;
	public Action OnDrag;
	public Action OnEndDrag;
	public Action<float> OnZoom;

#if UNITY_STANDALONE || UNITY_EDITOR
	private Vector3 startMousePos;
#else
	private Vector2 startMousePos;
#endif

	private bool mouseDown = false;
	private bool dragStarted = false;
	private bool castleWeaponGesture;
	private Castle castle;

	private float timer = 0f;

	private Vector2 screenSize;

    public Canvas canvas;

	void Start() {
		castle = FindObjectOfType<Castle>();
		screenSize = new Vector2(1f / Screen.width, 1f / Screen.height);
	}

	void Update()
    {
		if (Input.touchCount == 3 || Input.GetKeyDown(KeyCode.U))
			if (canvas.gameObject.activeInHierarchy)
                canvas.gameObject.SetActive(false);
            else
                canvas.gameObject.SetActive(true);

        // Capture mouse-up even when Shift was released before the mouse button.
        bool aimingCastleWeapon = castle != null && castle.Crossbow != null &&
            castle.Crossbow.isActiveAndEnabled && castle.Crossbow.HandlePlayerInput();
        if (aimingCastleWeapon || castleWeaponGesture) {
            if (Input.GetMouseButtonDown(0))
                castleWeaponGesture = true;
            mouseDown = false;
            if (dragStarted) {
                dragStarted = false;
                OnEndDrag?.Invoke();
            }
            if (!Input.GetMouseButton(0))
                castleWeaponGesture = false;
            return;
        }

        if (mouseDown)
			timer += Time.deltaTime;
#if UNITY_STANDALONE || UNITY_EDITOR
		if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) {
			startMousePos = Input.mousePosition;
#else
		if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) {
			startMousePos = Input.GetTouch(0).position;
#endif
			mouseDown = true;
			timer = 0f;
#if UNITY_STANDALONE || UNITY_EDITOR
		} else if(Input.GetMouseButton(0)) {
			if (mouseDown && !dragStarted && Vector3.Scale((startMousePos - Input.mousePosition), screenSize).magnitude > dragThreshold && !EventSystem.current.IsPointerOverGameObject()) {

#else
		} else if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved) {
			if (mouseDown && !dragStarted && Vector2.Scale((startMousePos - Input.GetTouch(0).position), screenSize).magnitude > dragThreshold && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) {
#endif
				dragStarted = true;
				OnBeginDrag?.Invoke();
			}

			if(dragStarted) {
				OnDrag?.Invoke();
			}
		}
#if UNITY_STANDALONE || UNITY_EDITOR
		if (Input.GetMouseButtonUp(0)) {
			if(mouseDown && !dragStarted && timer <= clickDelay && !EventSystem.current.IsPointerOverGameObject()) {
#else
		if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) {
			if(mouseDown && !dragStarted && timer <= clickDelay && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) {
#endif
				OnClick?.Invoke();
			}

			mouseDown = false;

			if(dragStarted) {
				dragStarted = false;
				OnEndDrag?.Invoke();
			}
		}

#if UNITY_STANDALONE || UNITY_EDITOR
		float y = Input.mouseScrollDelta.y;
		if (y != 0) {
			OnZoom?.Invoke(y);
		}
#else
		if (Input.touchCount == 2) {
			Touch touchZero = Input.GetTouch(0);
			Touch touchOne = Input.GetTouch(1);

			Vector2 touchZeroPreviousPos = touchZero.position - touchZero.deltaPosition;
			Vector2 touchOnePreviousPos = touchOne.position - touchOne.deltaPosition;

			float prevMagnitude = Vector3.Scale((touchZeroPreviousPos - touchOnePreviousPos), screenSize).magnitude;
			float currentMagnitude = Vector3.Scale((touchZero.position - touchOne.position), screenSize).magnitude;

			OnZoom?.Invoke((currentMagnitude - prevMagnitude) * 30f);
		}
#endif
	}
}
