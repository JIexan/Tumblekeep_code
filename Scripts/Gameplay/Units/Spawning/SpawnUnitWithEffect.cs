using DG.Tweening;
using UnityEngine;
using UnitySteer.Behaviors;

public class SpawnUnitWithEffect : MonoBehaviour {
	public GameObject spheresContainer;
	public Transform innerSphere;
	public Transform outerSphere;
	public ParticleSystem ps;

	public AnimationCurve appearCurve;
	public AnimationCurve lifeCurve;

	public float loopLength;

	public Vector3 innerSphereRotation;
	public Vector3 outerSphereRotation;

	public SteerForPoint steering;

	public SpawningPortal portal;

	private float timer = 0;
	private bool scalingEnabled = false;

	private UnitType unitType;
	private Waypoint nextWaypoint;
	private int unitsCount;
	private float appearDelay;
	private int waveId;
	private float hpMultiplier = 1f;
	private float rewardMultiplier = -1f;

	Unit u;

	// Множители ХП/наград — от мага-спавнера, чтобы мусор из портала не отставал от волны
	public void Init(int waveId, UnitType unitType, Waypoint nextWaypoint, Vector3 finalPos, int unitsCount, float appearDelay, float hpMultiplier = 1f, float rewardMultiplier = -1f) {
		this.waveId = waveId;
		this.unitType = unitType;
		this.hpMultiplier = hpMultiplier;
		this.rewardMultiplier = rewardMultiplier;
		this.nextWaypoint = nextWaypoint;
		this.unitsCount = unitsCount;
		this.appearDelay = appearDelay;

		steering.TargetPoint = finalPos;
		steering.OnArrival += (_) => SpawnPortal();

		spheresContainer.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
		spheresContainer.transform.DOScale(Vector3.one, 0.6f).SetEase(appearCurve).OnComplete(() => { scalingEnabled = true; });
		timer = 0;

		steering.Vehicle.TurnTime = 2f;
		DOTween.To(() => steering.Vehicle.TurnTime, x => steering.Vehicle.TurnTime = x, 0.1f, 3f).From(2f).SetEase(Ease.InCirc);
	}

	private void SpawnPortal() {
		steering.enabled = false;
		steering.Vehicle.CanMove = false;
		SpawningPortal p = Instantiate(portal, transform.position, Quaternion.identity);
		p.Init(waveId, unitType, nextWaypoint, unitsCount, appearDelay, hpMultiplier, rewardMultiplier);
		scalingEnabled = false;
		ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
		spheresContainer.transform.DOScale(Vector3.zero, 1f).SetEase(appearCurve).OnComplete(() => { Destroy(gameObject); });
	}


	public void Update() {
		innerSphere.Rotate(innerSphereRotation * Time.deltaTime);
		outerSphere.Rotate(outerSphereRotation * Time.deltaTime);

		if(steering.enabled && transform.position.y <= 0.1f) {
			SpawnPortal();
		}

		if (scalingEnabled) {
			float tmp = lifeCurve.Evaluate(timer / loopLength);
			innerSphere.localScale = new Vector3(tmp, tmp, tmp) * 2.0f;
			outerSphere.localScale = new Vector3(tmp, tmp, tmp) * 2.5f;

			timer += Time.deltaTime;
			if (timer >= loopLength)
				timer -= loopLength;
		}
	}
}
