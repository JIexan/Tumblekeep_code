using Gameplay.Cutscenes;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>A castle weapon with one explicitly requested shot per click. Never acquires enemies.</summary>
public sealed class CastleCrossbow : MonoBehaviour
{
    [SerializeField] private Castle castle;
    [SerializeField] private Transform weaponBase;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject loadedArrow;
    [SerializeField] private CastleCrossbowBolt boltPrefab;
    [SerializeField] private TowerRadius markerPrefab;
    [SerializeField, Min(1f)] private float range = 250f;
    [SerializeField, Min(1f)] private float projectileSpeed = 120f;
    [SerializeField, Min(1f)] private float rotationSpeed = 180f;
    [SerializeField, Min(0.1f)] private float baseReloadTime = 2.75f;
    private TowerRadius cursorMarker;
    private TowerRadius commandMarker;
    private Vector3 aimPoint;
    private Vector3 commandPoint;
    private bool hasAim;
    private bool shotRequested;
    private bool windingUp;
    private float reloadProgress = 1f;
    private Vector3 restPivotEuler;

    public Castle Owner => castle;
    public float Range => range;
    public float ProjectileSpeed => projectileSpeed;
    public float Damage => castle != null ? castle.GetCharacteristicValue(CharacteristicType.ProjectileDamage) : 0f;
    public float ReloadTime => baseReloadTime * 100f / (100f + Mathf.Max(0f, castle.GetCharacteristicValue(CharacteristicType.Speed)));
    public bool IsReloading => reloadProgress < 1f;
    public bool HasPendingShot => shotRequested;
    public Vector3 AimPoint => shotRequested ? commandPoint : aimPoint;
    public Vector3 CursorPoint => aimPoint;
    public int ShotsFired { get; private set; }

    private bool CanOperate => isActiveAndEnabled && castle != null && castle.CurrentHP > 0f &&
        GameController.Instance != null && GameController.Instance.IsGameStarted &&
        Time.timeScale > 0f && !CutsceneController.IsPlaying;

    private void Awake() {
        if (castle == null)
            castle = GetComponentInParent<Castle>();
        restPivotEuler = weaponPivot.localEulerAngles;
    }

    // Called by InputController before ordinary world clicks and camera drags.
    public bool HandlePlayerInput() {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        var game = GameController.Instance;
        bool draggingSpell = game != null && game.uiController != null &&
            game.uiController.spellDragPanel.gameObject.activeInHierarchy;
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool canAim = shift && CanOperate && !draggingSpell && CameraController.Instance != null && CameraController.Instance.active;
        if (canAim && !overUI) {
            Vector3 point = CameraController.Instance.GetPointOnLand(Input.mousePosition);
            UpdatePlayerAim(point, Input.GetMouseButtonDown(0));
        } else {
            HideCursor();
        }
        return canAim;
    }

    // The cursor preview remains independent of the accepted command during windup and reload.
    public void UpdatePlayerAim(Vector3 point, bool fire) {
        if (!CanOperate || !CanAimAt(point)) {
            HideCursor();
            return;
        }
        SetAimPoint(point);
        if (fire)
            RequestShot(point);
        if (cursorMarker == null)
            cursorMarker = CreateMarker("Castle crossbow cursor");
        if (cursorMarker != null) {
            cursorMarker.transform.position = point + Vector3.up * 0.3f;
            cursorMarker.SetColor(new ParticleSystem.MinMaxGradient(IsReloading || shotRequested ?
                new Color(1f, 0.45f, 0.15f) : new Color(0.25f, 1f, 0.6f)));
            if (!cursorMarker.gameObject.activeSelf)
                cursorMarker.Play();
        }
    }

    public bool CanAimAt(Vector3 point) {
        if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsNaN(point.z) ||
            float.IsInfinity(point.x) || float.IsInfinity(point.y) || float.IsInfinity(point.z))
            return false;
        Vector3 offset = point - weaponBase.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= range * range;
    }

    public void SetAimPoint(Vector3 point) {
        if (!CanAimAt(point))
            return;
        aimPoint = point;
        hasAim = true;
    }

    /// <returns>True only when this click schedules a shot. Reload clicks aim without queuing.</returns>
    public bool RequestShot(Vector3 point) {
        if (!CanOperate || shotRequested || !CanAimAt(point))
            return false;
        SetAimPoint(point);
        if (IsReloading)
            return false;
        commandPoint = point;
        shotRequested = true;
        commandMarker = CreateMarker("Castle crossbow impact target");
        if (commandMarker != null) {
            commandMarker.transform.position = commandPoint + Vector3.up * 0.3f;
            commandMarker.SetColor(new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.2f)));
        }
        return true;
    }

    private void Update() {
        if (animator != null)
            animator.speed = CanOperate ? 1f : 0f;
        if (!CanOperate) {
            if (castle == null || GameController.Instance == null || !GameController.Instance.IsGameStarted || castle.CurrentHP <= 0f)
                CancelShot();
            return;
        }
        if (IsReloading) {
            reloadProgress = Mathf.Min(1f, reloadProgress + Time.deltaTime / ReloadTime);
            if (loadedArrow != null)
                loadedArrow.SetActive(reloadProgress >= 0.8f);
        }
        if (animator != null)
            animator.SetFloat("reloadMultiplier", 0.3f / ReloadTime);
    }

    // Apply aiming after animation evaluation; reload animation must not override the aim axes.
    private void LateUpdate() {
        if (!CanOperate || !hasAim)
            return;
        Vector3 velocity = boltPrefab.CalculateLaunchVelocity(muzzle.position, AimPoint, projectileSpeed, out _, out _);
        Quaternion rotation = Quaternion.LookRotation(velocity);
        Quaternion yaw = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
        Quaternion pitch = Quaternion.Euler(rotation.eulerAngles.x, restPivotEuler.y, restPivotEuler.z);
        weaponBase.rotation = Quaternion.RotateTowards(weaponBase.rotation, yaw, rotationSpeed * Time.deltaTime);
        weaponPivot.localRotation = Quaternion.RotateTowards(weaponPivot.localRotation, pitch, rotationSpeed * Time.deltaTime);
        if (shotRequested && !windingUp && !IsReloading &&
            Quaternion.Angle(weaponBase.rotation, yaw) < 1f && Quaternion.Angle(weaponPivot.localRotation, pitch) < 1f) {
            windingUp = true;
            animator.SetTrigger("Start");
        }
    }

    // Same animation event as ArcherWeapon, on the same object as the copied Animator.
    public void OnTentionComplete() {
        if (!shotRequested || !windingUp || IsReloading || !CanOperate)
            return;
        var bolt = Instantiate(boltPrefab, muzzle.position, muzzle.rotation);
        bolt.Launch(commandPoint, projectileSpeed, Damage, commandMarker);
        commandMarker = null; // The bolt keeps its marker at the target until landing.
        ShotsFired++;
        shotRequested = false;
        windingUp = false;
        reloadProgress = 0f;
        if (loadedArrow != null)
            loadedArrow.SetActive(false);
        var audio = GameAudioManager.Instance;
        audio?.Play(audio.GetTower(TowerType.Arrow)?.shotId, muzzle.position);
    }

    private TowerRadius CreateMarker(string markerName) {
        if (markerPrefab == null)
            return null;
        var marker = Instantiate(markerPrefab);
        marker.name = markerName;
        marker.Init(boltPrefab.DamageRadius);
        marker.Play();
        return marker;
    }

    private void HideCursor() {
        if (cursorMarker != null)
            cursorMarker.Stop();
    }

    // Every marker created here owns the mesh generated by TowerRadius.Init.
    internal static void DestroyMarker(TowerRadius marker) {
        if (marker == null)
            return;
        marker.Stop();
        var filter = marker.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
            Destroy(filter.sharedMesh);
        Destroy(marker.gameObject);
    }

    private void CancelShot() {
        shotRequested = false;
        windingUp = false;
        if (animator != null)
            animator.ResetTrigger("Start");
        DestroyMarker(commandMarker);
        commandMarker = null;
        HideCursor();
    }

    private void OnDisable() {
        CancelShot();
    }

    private void OnDestroy() {
        DestroyMarker(cursorMarker);
    }
}
