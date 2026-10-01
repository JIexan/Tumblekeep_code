using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Зона сидения птиц: набор прямоугольных плоскостей в локальном пространстве трансформа.
/// Позиция/поворот берутся из трансформа, размеры прямоугольников — в метрах (скейл трансформа на размеры не влияет).
/// Прямоугольник можно наклонять (скат крыши): слоты лежат на наклонной плоскости, а птицы на них сидят ровно —
/// поворот птицы считается только по yaw в мире (GetUprightRotation).
/// Зоны сами регистрируются в статическом реестре в OnEnable/OnDisable — скрытие объекта
/// (например, ToggleObjectAction в хабе) автоматически убирает зону и её слоты.
/// </summary>
public class BirdPerchZone : MonoBehaviour
{
	[Serializable]
	public class PerchRect
	{
		public Vector3 localCenter;
		[Tooltip("Поворот прямоугольника в локальных осях зоны, градусы: Y — вокруг вертикали, X/Z — наклон под скат крыши")]
		public Vector3 localEuler;
		[HideInInspector] public float localYRotation;   // устаревшее (только yaw) — переносится в localEuler.y при загрузке
		[Min(0.5f)] public float width = 2f;   // вдоль локального X прямоугольника
		[Min(0.5f)] public float height = 2f;  // вдоль локального Z прямоугольника
		[Range(0f, 1f), Tooltip("Доля слотов, занятая птицами на старте сцены. 0 — пустая площадка: птицы прилетят сюда только в геймплее")]
		public float initialFill = 0.35f;
	}

	public struct Slot
	{
		public int rectIndex;
		public Vector3 offset;  // смещение от центра прямоугольника в его локальных осях (y = 0)
		public float yaw;       // базовый поворот птицы на слоте, градусы вокруг вертикали
	}

	public enum SlotState : byte { Free, Reserved, Occupied }

	// Шаг сетки слотов по умолчанию (используется превью в редакторе; рантайм берёт из BirdsSettings)
	public const float DefaultSlotSpacing = 0.45f;
	private const float JitterFraction = 0.22f;

	[SerializeField] private List<PerchRect> rects = new List<PerchRect>();
	[SerializeField, Tooltip("Виды птиц этой зоны с весами — стартовое население и прилетающие сюда стайки. Пусто — виды по умолчанию из BirdsSettings")]
	private List<BirdSpeciesWeight> species = new List<BirdSpeciesWeight>();

	// --- Статический реестр активных зон ---
	public static readonly List<BirdPerchZone> ActiveZones = new List<BirdPerchZone>();
	public static event Action<BirdPerchZone> ZoneEnabled;
	public static event Action<BirdPerchZone> ZoneDisabled;

	// --- Скэр-статистика (пишет BirdsManager) ---
	[NonSerialized] public float lastScareTime = float.NegativeInfinity;
	[NonSerialized] public float scareHeat;
	[NonSerialized] public float abandonedUntil = float.NegativeInfinity;

	private Slot[] slots;
	private SlotState[] slotStates;
	private object[] slotOccupants;
	private float slotSpacing = DefaultSlotSpacing;

	public List<PerchRect> Rects => rects;
	public List<BirdSpeciesWeight> Species => species;
	public int SlotCount => slots?.Length ?? 0;
	public float SlotSpacing => slotSpacing;

	public int FreeSlotCount
	{
		get
		{
			if (slotStates == null)
				return 0;
			int count = 0;
			for (int i = 0; i < slotStates.Length; i++)
				if (slotStates[i] == SlotState.Free)
					count++;
			return count;
		}
	}

	public Vector3 WorldCenter
	{
		get
		{
			if (rects.Count == 0)
				return transform.position;
			Vector3 sum = Vector3.zero;
			for (int i = 0; i < rects.Count; i++)
				sum += GetRectWorldCenter(i);
			return sum / rects.Count;
		}
	}

	public float BoundingRadius
	{
		get
		{
			Vector3 center = WorldCenter;
			float maxSqr = 0f;
			for (int i = 0; i < rects.Count; i++)
			{
				PerchRect r = rects[i];
				Vector3 c = GetRectWorldCenter(i);
				Quaternion rot = GetRectWorldRotation(i);
				Vector3 ext = rot * new Vector3(r.width * 0.5f, 0f, r.height * 0.5f);
				Vector3 ext2 = rot * new Vector3(r.width * 0.5f, 0f, -r.height * 0.5f);
				maxSqr = Mathf.Max(maxSqr,
					(c + ext - center).sqrMagnitude, (c - ext - center).sqrMagnitude,
					(c + ext2 - center).sqrMagnitude, (c - ext2 - center).sqrMagnitude);
			}
			return Mathf.Sqrt(maxSqr);
		}
	}

	private void OnEnable()
	{
		ActiveZones.Add(this);
		ZoneEnabled?.Invoke(this);
	}

	private void OnDisable()
	{
		ActiveZones.Remove(this);
		ZoneDisabled?.Invoke(this);
	}

	private void Awake()
	{
		MigrateLegacyRotation();
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		MigrateLegacyRotation();
	}
#endif

	// Старый формат хранил только yaw (localYRotation) — переносим в localEuler.y
	private void MigrateLegacyRotation()
	{
		for (int i = 0; i < rects.Count; i++)
		{
			PerchRect r = rects[i];
			if (r.localYRotation != 0f)
			{
				r.localEuler.y += r.localYRotation;
				r.localYRotation = 0f;
			}
		}
	}

	// --- Прямоугольники в мире ---

	public Vector3 GetRectWorldCenter(int rectIndex) => transform.TransformPoint(rects[rectIndex].localCenter);

	public Quaternion GetRectWorldRotation(int rectIndex) =>
		transform.rotation * Quaternion.Euler(rects[rectIndex].localEuler);

	/// <summary>Yaw прямоугольника в мире, градусы: его локальный Z, спроецированный на горизонт.</summary>
	public float GetRectWorldYaw(int rectIndex)
	{
		Quaternion rot = GetRectWorldRotation(rectIndex);
		Vector3 fwd = rot * Vector3.forward;
		fwd.y = 0f;
		if (fwd.sqrMagnitude < 1e-6f)   // плоскость поставлена вертикально — берём её локальный up
		{
			fwd = rot * Vector3.up;
			fwd.y = 0f;
		}
		return Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
	}

	/// <summary>Поворот птицы на слоте: только yaw в мире — на наклонных прямоугольниках птицы сидят ровно.</summary>
	public Quaternion GetUprightRotation(int rectIndex, float slotYaw) =>
		Quaternion.Euler(0f, GetRectWorldYaw(rectIndex) + slotYaw, 0f);

	// --- Слоты ---

	public void RebuildSlots(float spacing)
	{
		slotSpacing = spacing;
		var list = new List<Slot>();
		for (int i = 0; i < rects.Count; i++)
			GenerateSlots(rects[i], i, spacing, list);
		slots = list.ToArray();
		slotStates = new SlotState[slots.Length];
		slotOccupants = new object[slots.Length];
	}

	/// <summary>
	/// Генерация слотов прямоугольника: сетка по центру + детерминированный джиттер (хеш от индекса) —
	/// превью в редакторе и рантайм дают одинаковые точки. Статическая чистая функция.
	/// </summary>
	public static void GenerateSlots(PerchRect rect, int rectIndex, float spacing, List<Slot> output)
	{
		int cols = Mathf.Max(1, Mathf.FloorToInt(rect.width / spacing));
		int rows = Mathf.Max(1, Mathf.FloorToInt(rect.height / spacing));
		float startX = -(cols - 1) * spacing * 0.5f;
		float startZ = -(rows - 1) * spacing * 0.5f;
		float jitter = spacing * JitterFraction;

		for (int row = 0; row < rows; row++)
		{
			for (int col = 0; col < cols; col++)
			{
				int seed = rectIndex * 7919 + row * 271 + col;
				output.Add(new Slot
				{
					rectIndex = rectIndex,
					offset = new Vector3(
						startX + col * spacing + (Hash01(seed) * 2f - 1f) * jitter,
						0f,
						startZ + row * spacing + (Hash01(seed + 131) * 2f - 1f) * jitter),
					yaw = Hash01(seed + 977) * 360f
				});
			}
		}
	}

	public Vector3 GetSlotWorldPos(int slotIndex)
	{
		Slot s = slots[slotIndex];
		return GetRectWorldCenter(s.rectIndex) + GetRectWorldRotation(s.rectIndex) * s.offset;
	}

	public Quaternion GetSlotWorldRotation(int slotIndex)
	{
		Slot s = slots[slotIndex];
		return GetUprightRotation(s.rectIndex, s.yaw);
	}

	public SlotState GetSlotState(int slotIndex) => slotStates[slotIndex];
	public object GetSlotOccupant(int slotIndex) => slotOccupants[slotIndex];
	public int GetSlotRect(int slotIndex) => slots[slotIndex].rectIndex;

	public int GetRectSlotCount(int rectIndex)
	{
		if (slots == null)
			return 0;
		int count = 0;
		for (int i = 0; i < slots.Length; i++)
			if (slots[i].rectIndex == rectIndex)
				count++;
		return count;
	}

	/// <summary>Число слотов прямоугольника при заданном шаге — то же, что даёт GenerateSlots.</summary>
	public static int SlotCountFor(PerchRect rect, float spacing) =>
		Mathf.Max(1, Mathf.FloorToInt(rect.width / spacing)) * Mathf.Max(1, Mathf.FloorToInt(rect.height / spacing));

	/// <summary>Сколько птиц должно сидеть на зоне по настройкам прямоугольников (initialFill).</summary>
	public static int DesignedPopulation(List<PerchRect> rects, float spacing)
	{
		int total = 0;
		for (int r = 0; r < rects.Count; r++)
			total += Mathf.RoundToInt(SlotCountFor(rects[r], spacing) * rects[r].initialFill);
		return total;
	}

	public int DesignedPopulationCount => DesignedPopulation(rects, slotSpacing);

	/// <summary>
	/// Бронирует свободный слот рядом с точкой: из k=3 ближайших свободных выбирается случайный
	/// (чистый nearest сбивает всех птиц к ближнему краю; k-random даёт равномерное покрытие).
	/// </summary>
	public bool TryReserveSlot(Vector3 nearWorldPos, object occupant, out int slotIndex)
	{
		slotIndex = -1;
		if (slots == null || slots.Length == 0)
			return false;

		int best0 = -1, best1 = -1, best2 = -1;
		float d0 = float.MaxValue, d1 = float.MaxValue, d2 = float.MaxValue;
		for (int i = 0; i < slots.Length; i++)
		{
			if (slotStates[i] != SlotState.Free)
				continue;
			float d = (GetSlotWorldPos(i) - nearWorldPos).sqrMagnitude;
			if (d < d0) { d2 = d1; best2 = best1; d1 = d0; best1 = best0; d0 = d; best0 = i; }
			else if (d < d1) { d2 = d1; best2 = best1; d1 = d; best1 = i; }
			else if (d < d2) { d2 = d; best2 = i; }
		}
		if (best0 < 0)
			return false;

		int candidates = best2 >= 0 ? 3 : (best1 >= 0 ? 2 : 1);
		int pick = UnityEngine.Random.Range(0, candidates);
		slotIndex = pick == 0 ? best0 : (pick == 1 ? best1 : best2);
		slotStates[slotIndex] = SlotState.Reserved;
		slotOccupants[slotIndex] = occupant;
		return true;
	}

	public void OccupySlot(int slotIndex, object occupant)
	{
		slotStates[slotIndex] = SlotState.Occupied;
		slotOccupants[slotIndex] = occupant;
	}

	public void ReleaseSlot(int slotIndex)
	{
		if (slotStates == null || slotIndex < 0 || slotIndex >= slotStates.Length)
			return;
		slotStates[slotIndex] = SlotState.Free;
		slotOccupants[slotIndex] = null;
	}

	private static float Hash01(int seed)
	{
		uint h = (uint)seed * 2654435761u;
		h ^= h >> 13;
		h *= 2246822519u;
		h ^= h >> 16;
		return (h & 0xFFFFFF) / (float)0x1000000;
	}

#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
		for (int i = 0; i < rects.Count; i++)
		{
			PerchRect r = rects[i];
			Vector3 c = GetRectWorldCenter(i);
			Quaternion rot = GetRectWorldRotation(i);
			Vector3 right = rot * Vector3.right * (r.width * 0.5f);
			Vector3 fwd = rot * Vector3.forward * (r.height * 0.5f);
			Gizmos.DrawLine(c - right - fwd, c + right - fwd);
			Gizmos.DrawLine(c + right - fwd, c + right + fwd);
			Gizmos.DrawLine(c + right + fwd, c - right + fwd);
			Gizmos.DrawLine(c - right + fwd, c - right - fwd);
		}
	}
#endif
}
