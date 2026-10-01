using UnityEngine;

public class BallisticMotion : BaseMotion {

	private Vector3 startPosition;
	private Vector3 startVelocity;
	private float gravity;
	private float flightTime;
	private bool initialized;

	public void Initialize(Vector3 pos, float gravity, Vector3 velocity) {
		transform.position = pos;
		startPosition = pos;
		startVelocity = velocity;
		this.gravity = gravity;
		flightTime = 0f;
		initialized = true;
		enabled = true;
	}

	void FixedUpdate () {
		if (!initialized)
			return;

		flightTime += Time.fixedDeltaTime;

		// Аналитическая траектория от точки выстрела. Положение снаряда не зависит
		// ни от текущего transform.position, ни от того, к чему снаряд приаттачен.
		// Раньше тут была верле-интеграция по (curPos - lastPos): любое внешнее смещение
		// снаряда (вращение/анимация башни-родителя) подмешивалось в скорость
		// и с каждым кадром всё сильнее уводило снаряд с траектории.
		Vector3 position = startPosition
			+ startVelocity * flightTime
			- 0.5f * gravity * flightTime * flightTime * Vector3.up;

		Vector3 velocity = startVelocity - gravity * flightTime * Vector3.up;

		transform.position = position;
		if (velocity.sqrMagnitude > 0.0001f)
			transform.forward = velocity;
	}

}
