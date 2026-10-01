using UnityEngine;

/// <summary>
/// Зона спелла «Ядовитое облако»: висит на дороге заданное время и периодически отравляет
/// всех врагов внутри радиуса (по горизонтали). Визуал — дочерние ParticleSystem,
/// у которых shape-радиус подгоняется под радиус зоны
/// </summary>
public class PoisonCloudZone : MonoBehaviour
{
	[Tooltip("Системы частиц облака; у них shape.radius ставится равным радиусу зоны")]
	[SerializeField] private ParticleSystem[] particles;
	[Tooltip("Радиус, при котором rateOverTime частиц равен значению в префабе; дальше масштабируется по площади")]
	[SerializeField, Min(0.1f)] private float baseRadius = 4f;

	private float radius;
	private float damagePerSecond;
	private float lifetime;
	private float tickTimer;
	private bool stopped;

	private StatusEffectsSettings Settings => GameConfigs.Instance.StatusEffectsSettings;

	public void Init(float radius, float damagePerSecond, float lifetime) {
		this.radius = radius;
		this.damagePerSecond = damagePerSecond;
		this.lifetime = lifetime;
		tickTimer = 0f;

		float areaScale = (radius * radius) / (baseRadius * baseRadius);
		foreach (var ps in particles) {
			if (ps == null)
				continue;
			var shape = ps.shape;
			shape.radius = radius;
			var emission = ps.emission;
			var rate = emission.rateOverTime;
			rate.constant *= areaScale;
			rate.constantMin *= areaScale;
			rate.constantMax *= areaScale;
			emission.rateOverTime = rate;
			ps.Play(true);
		}
	}

	private void Update() {
		if (stopped) {
			if (!AnyParticleAlive())
				Destroy(gameObject);
			return;
		}

		lifetime -= Time.deltaTime;
		if (lifetime <= 0f) {
			Stop();
			return;
		}

		tickTimer -= Time.deltaTime;
		if (tickTimer > 0f)
			return;
		tickTimer += Settings != null ? Settings.cloudTickInterval : 0.5f;

		float poisonDuration = Settings != null ? Settings.cloudPoisonDuration : 3f;
		foreach (var u in GameController.Instance.GetAllEnemiesInRadius(transform.position, radius)) {
			u.ApplyPoison(null, damagePerSecond, poisonDuration);
		}
	}

	private void Stop() {
		stopped = true;
		foreach (var ps in particles) {
			if (ps != null)
				ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
		}
	}

	private bool AnyParticleAlive() {
		foreach (var ps in particles) {
			if (ps != null && ps.IsAlive(true))
				return true;
		}
		return false;
	}
}
