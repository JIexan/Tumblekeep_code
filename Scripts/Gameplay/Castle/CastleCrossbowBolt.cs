using UnityEngine;

/// <summary>A fast arcing bolt that detonates at its commanded point, never on passing units.</summary>
public sealed class CastleCrossbowBolt : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float damageRadius = 2f;
    [SerializeField, Min(0.1f)] private float arcHeight = 10f;
    [SerializeField, Min(0f)] private float impactLifetime = 2f;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private GameObject impactEffect;
    [SerializeField, Min(0.1f)] private float impactEffectBaseRadius = 4f;

    private Vector3 origin;
    private Vector3 velocity;
    private Vector3 target;
    private float gravity;
    private float flightTime;
    private float elapsed;
    private float damage;
    private bool flying;
    private TowerRadius targetMarker;

    public float DamageRadius => damageRadius;
    public float ArcHeight => arcHeight;

    // Fix flight time first, then solve gravity and upward velocity for an apex above both ends.
    public Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 end, float speed, out float duration, out float shotGravity) {
        duration = Mathf.Max(0.5f, Vector3.Distance(start, end) / Mathf.Max(1f, speed));
        float apex = Mathf.Max(start.y, end.y) + Mathf.Max(0.1f, arcHeight);
        float rise = Mathf.Sqrt(apex - start.y);
        float fall = Mathf.Sqrt(apex - end.y);
        shotGravity = 2f * (rise + fall) * (rise + fall) / (duration * duration);
        Vector3 launchVelocity = (end - start) / duration;
        launchVelocity.y = Mathf.Sqrt(2f * shotGravity * (apex - start.y));
        return launchVelocity;
    }

    public void Launch(Vector3 point, float speed, float shotDamage, TowerRadius marker = null) {
        origin = transform.position;
        target = point;
        targetMarker = marker;
        damage = shotDamage; // An upgrade after launch never changes a bolt already in flight.
        velocity = CalculateLaunchVelocity(origin, target, speed, out flightTime, out gravity);
        elapsed = 0f;
        transform.rotation = Quaternion.LookRotation(velocity);
        flying = true;
        if (trail != null) {
            trail.Clear();
            trail.emitting = true;
        }
    }

    private void Update() {
        if (!flying || Time.deltaTime <= 0f)
            return;
        elapsed = Mathf.Min(flightTime, elapsed + Time.deltaTime);
        transform.position = origin + velocity * elapsed + Vector3.down * (0.5f * gravity * elapsed * elapsed);
        Vector3 direction = velocity + Vector3.down * (gravity * elapsed);
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
        if (elapsed >= flightTime) {
            transform.position = target;
            Impact();
        }
    }

    private void Impact() {
        flying = false;
        if (trail != null)
            trail.emitting = false;
        ClearTargetMarker();

        // Snapshot before dealing damage: deaths can remove enemies from the game's live list.
        // Match the ground circle, including jumping enemies above it.
        if (GameController.Instance != null) {
            foreach (var unit in GameController.Instance.GetAllEnemiesInRadius(target, damageRadius)) {
                if (unit != null && !unit.isDead)
                    unit.DamageWithEffect(null, damage, unit.transform.position + unit.aimOffset, Vector3.up, DamageType.Physical);
            }
        }
        if (impactEffect != null) {
            var effect = Instantiate(impactEffect, target, Quaternion.identity);
            effect.transform.localScale *= damageRadius / impactEffectBaseRadius;
            Destroy(effect, 5f);
        }
        var audio = GameAudioManager.Instance;
        audio?.Play(audio.GetTower(TowerType.Arrow)?.impactId, transform.position);
        Destroy(gameObject, impactLifetime);
    }

    private void ClearTargetMarker() {
        if (targetMarker == null)
            return;
        CastleCrossbow.DestroyMarker(targetMarker);
        targetMarker = null;
    }

    private void OnDestroy() => ClearTargetMarker();
}
