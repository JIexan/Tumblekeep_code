using System;
using UnityEngine;

public abstract class BaseShip : MonoBehaviour
{
	protected enum ShipState {
		Gone,
		Moving,
		Arriving,
		Arrived,
		Departing,
		MovingAway
	}

	[SerializeField] protected ParticleSystem foam;
	[SerializeField] protected float wayPointDistance = 20f;
	[SerializeField] protected float rotationSpeed;
	[SerializeField] protected float startSpeed;
	[SerializeField] protected float minSpeed;
	[SerializeField] protected float brakingDistance;

	protected ShipState state = ShipState.Gone;
	protected Transform finalPoint;
	protected int enterPathId;
	protected int exitPathId;

	protected Waypoint currentWaypoint;
	protected Quaternion targetRotation;
	protected float targetAngle;
	protected Vector3 targetPosition;

	protected float speed;
	protected bool braking = false;
	protected bool accelerating = false;
	protected float acceleratingDistance = 0f;
	protected float currentDistance = 0f;
	protected Vector3 previousPosition = Vector3.zero;

	public event Action OnArrived;
	public event Action OnDeparted;

	public bool Departed => state == ShipState.Departing || state == ShipState.Gone || state == ShipState.MovingAway;
	public bool IsArrived => state == ShipState.Arrived;

	public virtual void Setup(Transform targetPoint, int enterPath, int exitPath) {
		finalPoint = targetPoint;
		enterPathId = enterPath;
		exitPathId = exitPath;
	}

	public virtual void Arrive() {
		currentWaypoint = LevelDesriptor.Instance.GetShipRoute(enterPathId);
		transform.position = currentWaypoint.position;
		transform.rotation = Quaternion.LookRotation(currentWaypoint.next.position - currentWaypoint.position);
		state = ShipState.Moving;
		speed = startSpeed;
		gameObject.SetActive(true);
		foam.Play();
	}

	public virtual void Depart() {
		state = ShipState.Departing;
		Vector3 toWaypoint = transform.position - finalPoint.position;
		targetAngle = Quaternion.LookRotation(finalPoint.forward).eulerAngles.y;
		targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
		targetPosition = LevelDesriptor.Instance.GetShipRoute(exitPathId).position;
		foam.Play();
		OnShipDeparted();
	}

	private readonly GameAudioLoop idleAudio = new GameAudioLoop();

	protected virtual void OnDisable() => idleAudio.Stop();

	protected virtual void Update() {
		idleAudio.Update(GameAudioManager.Instance?.GetId(GameSound.ShipIdle),
			transform.position, state != ShipState.Gone);
		UpdateSpeed();
		UpdateMovement();
	}

	protected virtual void FixedUpdate() {
		UpdateWaypoints();
	}

	private void UpdateSpeed() {
		if (braking) {
			currentDistance += (previousPosition - transform.position).magnitude;
			speed = minSpeed + (startSpeed - minSpeed) * (1 - (currentDistance / brakingDistance));
			if (speed <= minSpeed)
				braking = false;
		}

		if (accelerating) {
			currentDistance += (previousPosition - transform.position).magnitude;
			speed = minSpeed + (startSpeed - minSpeed) * (currentDistance / acceleratingDistance);
			if (speed >= startSpeed || currentDistance / acceleratingDistance > 1f)
				accelerating = false;
		}
	}

	private void UpdateMovement() {
		switch (state) {
			case ShipState.Moving:
			case ShipState.MovingAway:
				previousPosition = transform.position;
				transform.position = Vector3.MoveTowards(transform.position, transform.position + transform.forward, speed * Time.deltaTime);

				if (targetAngle != transform.rotation.eulerAngles.y) {
					transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
				}
				break;

			case ShipState.Arriving:
			case ShipState.Departing:
				previousPosition = transform.position;
				transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

				if (targetAngle != transform.rotation.eulerAngles.y) {
					transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
				}

				if ((targetPosition - transform.position).sqrMagnitude < 0.3f) {
					if (state == ShipState.Arriving) {
						state = ShipState.Arrived;
						OnShipArrived();
					}
					if (state == ShipState.Departing) {
						StartMovingAway();
					}
				}
				break;
		}
	}

	private void StartMovingAway() {
		currentWaypoint = LevelDesriptor.Instance.GetShipRoute(exitPathId);
		acceleratingDistance = (currentWaypoint.next.position - transform.position).magnitude;
		currentWaypoint = currentWaypoint.next;
		targetPosition = currentWaypoint.position;
		accelerating = true;
		currentDistance = 0f;
		state = ShipState.MovingAway;
	}

	private void UpdateWaypoints() {
		if (currentWaypoint == null)
			return;

		Vector3 toWaypoint = currentWaypoint.position - transform.position;
		targetAngle = Quaternion.LookRotation(toWaypoint).eulerAngles.y;
		targetRotation = Quaternion.Euler(0f, targetAngle, 0f);

		if (toWaypoint.magnitude < wayPointDistance) {
			currentWaypoint = currentWaypoint.next;

			if (state == ShipState.Moving && currentWaypoint != null && currentWaypoint.next == null) {
				brakingDistance = (currentWaypoint.position - transform.position).magnitude;
				braking = true;
				currentDistance = 0f;
			}

			if (currentWaypoint == null) {
				if (state == ShipState.Moving) {
					state = ShipState.Arriving;
					toWaypoint = finalPoint.position - transform.position;
					targetAngle = Quaternion.LookRotation(finalPoint.forward).eulerAngles.y;
					targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
					targetPosition = finalPoint.position;
				}
				if (state == ShipState.MovingAway) {
					state = ShipState.Gone;
					gameObject.SetActive(false);
					Destroy(gameObject);
				}
			} else {
				toWaypoint = currentWaypoint.position - transform.position;
				targetAngle = Quaternion.LookRotation(toWaypoint).eulerAngles.y;
				targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
				targetPosition = currentWaypoint.position;
			}
		}
	}

	protected virtual void OnShipArrived() {
		foam.Stop(true, ParticleSystemStopBehavior.StopEmitting);
		OnArrived?.Invoke();
	}

	protected virtual void OnShipDeparted() {
		OnDeparted?.Invoke();
	}
}