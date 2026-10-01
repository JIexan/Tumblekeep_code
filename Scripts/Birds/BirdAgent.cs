using DG.Tweening;
using UnityEngine;

/// <summary>
/// Одна птица. Plain C#-объект: никаких MonoBehaviour/Update — всех птиц крутит BirdsManager.
/// </summary>
public class BirdAgent
{
	public enum BirdState
	{
		Sitting,     // сидит на слоте
		TakingOff,   // ждёт своей очереди взлёта (стаггер)
		Flying,      // летит в стае
		Landing,     // отпущена на посадку, заходит на свой слот
		Touchdown,   // финальный твин до точного слота
		Leaving      // улетает с карты
	}

	public BirdState state;

	// Полёт
	public Vector3 pos;
	public Vector3 vel;
	public float roll;                 // сглаженный крен, градусы
	public Vector3 prevFlatVel;
	public float wanderSeed;
	public float scareBoostUntil;

	// Пул-инстансы моделей (одновременно живы только во время свапа)
	public Transform sittingTf;
	public Transform flyingTf;

	// Привязка к зоне
	public BirdPerchZone zone;
	public int slot = -1;

	public BirdFlock flock;

	// Вид (окрас) — выбирается при спавне из списка зоны или BirdsSettings; null — материал префаба как есть
	public BirdSpecies species;

	// Тайминги
	public float nextHopTime;
	public float releaseAt;            // момент взлёта/отпускания на посадку (стаггер)

	// Активный твин (прыжок/тачдаун) — убивать при любой смене состояния
	public Tween activeTween;

	// Служебное для тачдауна
	public Vector3 touchdownStartPos;
	public Quaternion touchdownStartRot;
	public bool swapStarted;

	public void KillTween()
	{
		if (activeTween != null && activeTween.IsActive())
			activeTween.Kill();
		activeTween = null;
	}
}
