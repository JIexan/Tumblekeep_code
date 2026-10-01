using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Параметр чего-либо, используемый на время боя, хранит в себе отдельно исходное значение и бонусы
/// </summary>
[System.Serializable]
public class DynamicFloat: ISerializationCallbackReceiver {
	[SerializeField] protected float value;
	protected float factor = 1f;
	protected float bonus = 0f;

	private float calculatedValue;
	private int calculatedRoundedValue;

	private class AppliedBuff {
		public BuffType type;
		public int buffCount;
	}

	private List<AppliedBuff> appliedBuffs = new List<AppliedBuff>();

	public DynamicFloat() {
		factor = 1f;
		bonus = 0f;
	}

	public DynamicFloat(float value) : this() {
		this.value = value;
		UpdateValue();
	}

	protected void UpdateValue() {
		calculatedValue = (value + bonus) * factor;
		calculatedRoundedValue = Mathf.RoundToInt((value + bonus) * factor);
	}

	public virtual void AddBonus(float bonus) {
		this.bonus += bonus;
		UpdateValue();
	}

	public virtual void AddPercent(int percent) {
		factor += percent / 100f;
		UpdateValue();
	}

	public virtual void SubPercent(int percent) {
		factor -= percent / 100f;
		UpdateValue();
	}

	public void AddPercent(float percent, BuffType buffType, int buffLimit) {
		AppliedBuff buff = GetBuff(buffType);
		if(buff == null) {
			buff = new AppliedBuff() {
				type = buffType
			};
			appliedBuffs.Add(buff);
		}
		if (buff.buffCount < buffLimit) {
			factor *= percent;
			UpdateValue();
		}

		buff.buffCount++;
	}

	public void SubPercent(float percent, BuffType buffType) {
		AppliedBuff buff = GetBuff(buffType);
		if (buff == null) {
			return;
		}
		buff.buffCount--;
		if (buff.buffCount <= 0) {
			appliedBuffs.Remove(buff);
			factor *= 1 / percent;
			UpdateValue();
		}
	}

	public virtual float Get() {
		return calculatedValue;
	}

	public int GetRounded() {
		return calculatedRoundedValue;
	}

	public float GetPercent() {
		return factor;
	}

	public override string ToString() {
		return GetRounded().ToString();
	}

	public void OnBeforeSerialize() {
		
	}

	public void OnAfterDeserialize() {
		UpdateValue();
	}

	public static implicit operator float(DynamicFloat value) {
		return value.Get();
	}


	private AppliedBuff GetBuff(BuffType type) {
		foreach (var buff in appliedBuffs) {
			if (buff.type == type)
				return buff;
		}
		return null;
	}

}