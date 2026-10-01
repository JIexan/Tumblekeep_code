using DG.Tweening;
using UnityEngine;

public class RamUnit : Unit
{
	public RamSettings settings = null;

	public float wheelDiameter;

	public Transform[] leftWheels;
	public Transform[] rightWheels;

	private float wheelRotationSpeed;
	private bool rotatingRight;

	public void SetRamSettings(RamSettings settings) {
		this.settings = settings;
	}

	public override void Start() {
		base.Start();
		wheelRotationSpeed = (speed / (wheelDiameter * Mathf.PI)) * 360f;
	}

	public override void DeathRoutine() {
		//заспавнить юнитов которые сидят внутри тарана
		transform.DOMoveY(-2, deathTimer);

		if(settings != null) {
			foreach(var unit in settings.units) {
				for(int i = 0; i < unit.unitCount; i++) {
					GameController.Instance.SpawnUnit(waveId, transform.position, transform.rotation, unit.type, -1, currentWaypoint, ChildHpMultiplier, false, RewardMultiplier);
				}
			}
		}
	}

	public override void Update() {
		if(MovementEnabled) {
			Move();
		}

		UpdatePeriodicEffects();
	}

	protected override void OnNextWaypoint() {
		rotatingRight = GetRotateDirection(transform.rotation, targetRotation);
	}

	private bool GetRotateDirection(Quaternion from, Quaternion to) {
		float fromY = from.eulerAngles.y;
		float toY = to.eulerAngles.y;
		float clockWise = 0f;
		float counterClockWise = 0f;

		if (fromY <= toY) {
			clockWise = toY - fromY;
			counterClockWise = fromY + (360 - toY);
		} else {
			clockWise = (360 - fromY) + toY;
			counterClockWise = fromY - toY;
		}
		return (clockWise <= counterClockWise);
	}

	private void Move() {
		if (isDead) {
			deathTimer -= Time.deltaTime;
			if (deathTimer <= 0) {
				Destroy(gameObject);
			}
			return;
		}

		if (transform.rotation.eulerAngles.y - 0.1f < targetAngle && targetAngle < transform.rotation.eulerAngles.y + 0.1f) {
			MoveForward(speed * Time.deltaTime);
			RotateWheels(true, true);
		} else {
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * speed.GetPercent() * Time.deltaTime);
			RotateWheels(rotatingRight, !rotatingRight);
		}
	}

	private void RotateWheels(bool leftFront, bool rightFront) {
		foreach(var wheel in leftWheels) {
			wheel.transform.Rotate((leftFront ? Vector3.right : Vector3.left), wheelRotationSpeed * Time.deltaTime);
		}

		foreach (var wheel in rightWheels) {
			wheel.transform.Rotate((rightFront ? Vector3.left : Vector3.right), wheelRotationSpeed * Time.deltaTime);
		}
	}
}
