using UnityEngine;

/// <summary>
/// Процедурная верёвка на цепочке костей (без PhysX).
/// Каждая кость цепочки — Verlet-частица в мировых координатах. Корень прибит к rootBone,
/// остальные тянутся гравитацией, сопротивлением воздуха относительно ветра (базовый ветер + шумовые порывы)
/// и ограничениями длины/изгиба. Потом кости поворачиваются так, чтобы boneAxis смотрел на следующую частицу.
/// Инерция от движения носителя (разгон, повороты, качка вверх-вниз) получается сама — частицы живут в мире.
/// </summary>
[DefaultExecutionOrder(100)]
public class RopeSimulator : MonoBehaviour {

	[Header("Кости")]
	[Tooltip("Первая кость цепочки. Дальше берётся первый ребёнок каждой кости, пока не дойдём до tip (или до листа).")]
	public Transform rootBone;
	[Tooltip("Последний объект цепочки (мешочек). Если пусто — берётся лист цепочки.")]
	public Transform tip;
	[Tooltip("Запасная ось кости «на следующую кость» — используется только если дочерняя кость стоит в нуле родителя. Обычно направление берётся из реальной позиции ребёнка.")]
	public Vector3 boneAxis = Vector3.up;

	[Header("Симуляция")]
	[Tooltip("Шаг симуляции. Кадр разбивается на подшаги, чтобы поведение не зависело от FPS.")]
	public float substep = 1f / 60f;
	[Tooltip("Максимум подшагов за кадр (при просадке FPS верёвка замедлится, а не взорвётся).")]
	public int maxSubsteps = 4;
	[Tooltip("Итерации ограничений длины/изгиба за подшаг.")]
	public int constraintIterations = 4;
	public float gravity = 9.81f;
	[Tooltip("Потеря скорости в секунду (0 — без затухания).")]
	[Range(0f, 1f)] public float damping = 0.1f;
	[Tooltip("Масса мешочка относительно звена верёвки. Тяжелее — сильнее качается маятником и меньше сдувается.")]
	public float tipMass = 4f;

	[Header("Аэродинамика")]
	[Tooltip("Сопротивление воздуха для звеньев верёвки (1/с). При полёте вперёд именно оно сдувает верёвку назад.")]
	public float airDrag = 3f;
	[Tooltip("Сопротивление воздуха для мешочка (1/с).")]
	public float tipAirDrag = 1f;
	[Tooltip("Постоянный ветер в мире (м/с).")]
	public Vector3 wind = Vector3.zero;
	[Tooltip("Амплитуда порывов (м/с). Именно они заставляют верёвку «развеваться» при равномерном полёте.")]
	public float gustStrength = 3f;
	[Tooltip("Частота порывов (Гц).")]
	public float gustFrequency = 1.2f;
	[Tooltip("Насколько порывы отличаются вдоль верёвки (0 — вся верёвка качается синхронно).")]
	public float gustSpatialScale = 0.35f;

	[Header("Форма")]
	[Tooltip("Пружина к позе покоя (ускорение на метр отклонения). Не даёт верёвке складываться пополам.")]
	public float restSpring = 0f;
	[Tooltip("Максимальный угол между соседними звеньями.")]
	[Range(0f, 180f)] public float maxBendAngle = 70f;
	[Tooltip("Если корень за кадр прыгнул дальше этого — считаем телепортом и сбрасываем верёвку.")]
	public float teleportDistance = 5f;

	[Header("Отладка")]
	public bool drawGizmos;

	Transform[] bones;          // rootBone ... tip
	Quaternion[] restLocalRotation;
	Vector3[] restDirLocal;     // направление на следующую кость в системе координат родителя rootBone
	Vector3[] aimAxisLocal;     // ось кости, смотрящая на следующую кость (локально для кости)
	float[] restLength;
	float[] invMass;

	Vector3[] pos;
	Vector3[] prevPos;
	Vector3 prevAnchor;
	float accumulator;
	float noiseSeed;
	bool initialized;

	Transform Space => rootBone.parent != null ? rootBone.parent : rootBone;

	void OnEnable() {
		initialized = false;
	}

	void Initialize() {
		if (rootBone == null) {
			enabled = false;
			return;
		}

		// собираем цепочку
		var list = new System.Collections.Generic.List<Transform>();
		var t = rootBone;
		while (t != null) {
			list.Add(t);
			if (t == tip || t.childCount == 0)
				break;
			t = t.GetChild(0);
		}
		if (list.Count < 2) {
			enabled = false;
			return;
		}

		bones = list.ToArray();
		int n = bones.Length;

		restLocalRotation = new Quaternion[n];
		restDirLocal = new Vector3[n - 1];
		aimAxisLocal = new Vector3[n - 1];
		restLength = new float[n - 1];
		invMass = new float[n];
		pos = new Vector3[n];
		prevPos = new Vector3[n];

		for (int i = 0; i < n; i++) {
			restLocalRotation[i] = bones[i].localRotation;
			pos[i] = prevPos[i] = bones[i].position;
			invMass[i] = i == 0 ? 0f : (i == n - 1 ? 1f / Mathf.Max(0.01f, tipMass) : 1f);
		}
		for (int i = 0; i < n - 1; i++) {
			Vector3 d = bones[i + 1].position - bones[i].position;
			restLength[i] = d.magnitude;
			restDirLocal[i] = Space.InverseTransformDirection(d.normalized);
			// реальная ось «на ребёнка» в локальных координатах кости (ребёнок может быть смещён от boneAxis)
			Vector3 local = bones[i + 1].localPosition;
			aimAxisLocal[i] = local.sqrMagnitude > 1e-8f ? local.normalized : boneAxis.normalized;
		}

		prevAnchor = rootBone.position;
		noiseSeed = Random.value * 1000f;
		accumulator = 0f;
		initialized = true;
	}

	/// <summary>Сбросить верёвку в позу покоя (например после телепорта).</summary>
	[ContextMenu("Reset rope")]
	public void ResetRope() {
		if (!initialized) {
			Initialize();
			return;
		}
		for (int i = 0; i < bones.Length; i++)
			bones[i].localRotation = restLocalRotation[i];
		for (int i = 0; i < bones.Length; i++)
			pos[i] = prevPos[i] = bones[i].position;
		prevAnchor = rootBone.position;
		accumulator = 0f;
	}

	void LateUpdate() {
		Simulate(Time.deltaTime, Time.time);
	}

	/// <summary>Один кадр симуляции. Вынесено из LateUpdate, чтобы можно было гонять верёвку вручную (тесты, превью).</summary>
	public void Simulate(float dt, float time) {
		if (!initialized) {
			Initialize();
			if (!initialized)
				return;
		}

		// Возвращаем кости в позу покоя: так позиция корня и направления покоя считаются от анимации/родителя,
		// а не от нашего прошлого кадра.
		for (int i = 0; i < bones.Length; i++)
			bones[i].localRotation = restLocalRotation[i];

		Vector3 anchor = rootBone.position;
		if ((anchor - prevAnchor).sqrMagnitude > teleportDistance * teleportDistance) {
			ResetRope();
			anchor = rootBone.position;
		}

		if (dt > 0f) {
			accumulator = Mathf.Min(accumulator + dt, substep * maxSubsteps);
			int steps = Mathf.FloorToInt(accumulator / substep);
			if (steps > 0) {
				accumulator -= steps * substep;
				for (int s = 1; s <= steps; s++) {
					pos[0] = Vector3.Lerp(prevAnchor, anchor, (float)s / steps);
					Step(substep, time);
				}
				prevAnchor = anchor;
			} else {
				// подшаг ещё не набрался — корень всё равно держим на месте
				pos[0] = anchor;
			}
		} else {
			pos[0] = anchor;
		}

		ApplyToBones();
	}

	/// <summary>Позиции частиц в мире (корень ... мешочек). Только для чтения/отладки.</summary>
	public Vector3[] Particles => pos;

	void Step(float dt, float worldTime) {
		int n = pos.Length;
		float time = worldTime * gustFrequency + noiseSeed;
		float dampFactor = Mathf.Max(0f, 1f - damping * dt);
		Vector3 gravityAccel = Vector3.down * gravity;
		Transform space = Space;

		// интеграция
		for (int i = 1; i < n; i++) {
			bool isTip = i == n - 1;
			Vector3 velocity = (pos[i] - prevPos[i]) / dt;

			// ветер с порывами: три независимых шума с фазовым сдвигом вдоль верёвки
			float phase = i * gustSpatialScale;
			Vector3 gust = new Vector3(
				Mathf.PerlinNoise(time, phase) - 0.5f,
				Mathf.PerlinNoise(time + 37.1f, phase + 11.3f) - 0.5f,
				Mathf.PerlinNoise(time + 73.7f, phase + 23.9f) - 0.5f) * (2f * gustStrength);
			Vector3 airVelocity = wind + gust;

			float drag = isTip ? tipAirDrag : airDrag;
			Vector3 accel = gravityAccel + (airVelocity - velocity) * drag;

			if (restSpring > 0f) {
				Vector3 restPos = pos[i - 1] + space.TransformDirection(restDirLocal[i - 1]) * restLength[i - 1];
				accel += (restPos - pos[i]) * restSpring;
			}

			Vector3 next = pos[i] + (pos[i] - prevPos[i]) * dampFactor + accel * (dt * dt);
			prevPos[i] = pos[i];
			pos[i] = next;
		}

		// ограничения
		float cosMax = Mathf.Cos(maxBendAngle * Mathf.Deg2Rad);
		for (int it = 0; it < constraintIterations; it++) {
			for (int i = 1; i < n; i++) {
				// длина звена
				Vector3 d = pos[i] - pos[i - 1];
				float len = d.magnitude;
				if (len > 1e-6f) {
					float w = invMass[i - 1] + invMass[i];
					if (w > 0f) {
						Vector3 correction = d * ((len - restLength[i - 1]) / len / w);
						pos[i - 1] += correction * invMass[i - 1];
						pos[i] -= correction * invMass[i];
					}
				}

				// ограничение изгиба относительно предыдущего звена
				if (i >= 2 && maxBendAngle < 180f) {
					Vector3 prevDir = (pos[i - 1] - pos[i - 2]).normalized;
					Vector3 dir = pos[i] - pos[i - 1];
					float dirLen = dir.magnitude;
					if (dirLen > 1e-6f) {
						Vector3 dirN = dir / dirLen;
						if (Vector3.Dot(prevDir, dirN) < cosMax) {
							Vector3 axis = Vector3.Cross(prevDir, dirN);
							if (axis.sqrMagnitude < 1e-8f)
								axis = Vector3.Cross(prevDir, Vector3.up);
							Vector3 clamped = Quaternion.AngleAxis(maxBendAngle, axis.normalized) * prevDir;
							pos[i] = pos[i - 1] + clamped * dirLen;
						}
					}
				}
			}
		}
	}

	void ApplyToBones() {
		for (int i = 0; i < bones.Length - 1; i++) {
			Transform bone = bones[i];
			Vector3 target = pos[i + 1] - pos[i];
			if (target.sqrMagnitude < 1e-8f)
				continue;
			Vector3 current = bone.TransformDirection(aimAxisLocal[i]);
			bone.rotation = Quaternion.FromToRotation(current, target) * bone.rotation;
		}
	}

	void OnDrawGizmosSelected() {
		if (!drawGizmos || pos == null)
			return;
		Gizmos.color = Color.yellow;
		for (int i = 0; i < pos.Length; i++) {
			Gizmos.DrawSphere(pos[i], 0.03f);
			if (i > 0)
				Gizmos.DrawLine(pos[i - 1], pos[i]);
		}
	}
}
