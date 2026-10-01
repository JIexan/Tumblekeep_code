using System;

public class PeriodicEffect {
	public Unit unit;
	public float value1;
	public float interval;
	public float timer;
	public float duration;
	public UnitEffect effectType;
	public Action<Unit, float> action;
	/// <summary>
	/// Возвращает true если эффект истек и должен быть удален
	/// </summary>
	/// <returns></returns>
	internal bool Update(float deltaTime) {
		timer -= deltaTime;
		duration -= deltaTime;
		if (timer <= 0f) {
			action?.Invoke(unit, value1);
			timer += interval;
		}

		bool end = duration <= 0f;
		
		if(end)
			unit.OnPeriodicEffectRemoved(effectType);

		return end;
	}

}
