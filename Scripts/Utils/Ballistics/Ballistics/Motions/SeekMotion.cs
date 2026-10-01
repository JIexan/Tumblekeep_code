using UnityEngine;

public class SeekMotion : BaseMotion {
	private float maxSpeed = 10f;          // Постоянная скорость движения
	private float maneuverability = 2f;    // Маневренность

	private Unit target;
	private Vector3 velocity;
	private Vector3 steeringForce;

	public void Initialize(Unit target, float maxSpeed, float maneuverability) {
		if (target == null) {
			Debug.LogWarning("SteeringProjectile: target is null!");
			enabled = false;
			return;
		}

		this.target = target;
		this.maxSpeed = maxSpeed;
		this.maneuverability = maneuverability;
		velocity = transform.forward * maxSpeed; // Начальное направление из forward объекта
		enabled = true;
	}

	void FixedUpdate() {
		/*if (target == null) {
			Destroy(gameObject);
			return;
		}*/

		// 1. Вычисляем желаемую скорость (Seek с предсказанием)
		Vector3 desiredVelocity = CalculateSeekWithPrediction();
		desiredVelocity = desiredVelocity.normalized * maxSpeed;

		// 2. Вычисляем силу управления на основе маневренности
		steeringForce = desiredVelocity - velocity;
		steeringForce = Vector3.ClampMagnitude(steeringForce, maneuverability * Time.fixedDeltaTime);

		// 3. Применяем силу к скорости
		velocity += steeringForce;
		velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

		// 4. Обновляем позицию
		transform.position += velocity * Time.fixedDeltaTime;

		// 5. Поворачиваем в направлении движения с учетом маневренности
		if (velocity.sqrMagnitude > 0.01f) {
			// Маневренность влияет на скорость поворота
			transform.forward = velocity;
			/*float rotationSpeed = maneuverability * 2f; // Множитель можно настроить
			Quaternion targetRotation = Quaternion.LookRotation(velocity);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);*/
		}
	}

	private Vector3 CalculateSeekWithPrediction() {
		if(target == null) {
			return transform.forward;
		}

		Vector3 predictedPosition = target.transform.position;

		// Предсказываем позицию цели с учетом её скорости и нашего времени сближения
		float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
		float timeToReach = distanceToTarget / Mathf.Max(maxSpeed, 0.1f);
		predictedPosition += target.Velocity * timeToReach;

		return predictedPosition - transform.position;
	}

}
