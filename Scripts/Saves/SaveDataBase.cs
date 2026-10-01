using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static SaveDataTechnologyTree;

public class MinValueAttribute : Attribute {
	public float value;

	public MinValueAttribute(float v) {
		value = v;
	}
}

[System.Serializable]
public class SaveDataBase {

	public virtual void CheckValues() {
		FieldInfo[] objectFields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
		for (int i = 0; i < objectFields.Length; i++) {
			if(typeof(SaveDataBase).IsAssignableFrom(objectFields[i].FieldType)) {
				if(objectFields[i].GetValue(this) != null)
					(objectFields[i].GetValue(this) as SaveDataBase).CheckValues();
			}
			RangeAttribute rangeAttribute = Attribute.GetCustomAttribute(objectFields[i], typeof(RangeAttribute)) as RangeAttribute;
			MinValueAttribute minAttribute = Attribute.GetCustomAttribute(objectFields[i], typeof(MinValueAttribute)) as MinValueAttribute;
			if (rangeAttribute != null) {
				CheckField(objectFields[i], rangeAttribute.min, rangeAttribute.max);
			}
			if(minAttribute != null) {
				CheckField(objectFields[i], minAttribute.value);
			}
		}
	}

	//здесь хотелось бы сделать общи тип Т, но для этого надо много буков. Мне лень
	private void CheckField(FieldInfo field, float min, float max) {
		if (field.FieldType.Equals(typeof(float))) {
			float value = (float)field.GetValue(this);
			if (value < min) {
				field.SetValue(this, min);
			}
			if (value > max)
				field.SetValue(this, max);
		}
		else if (field.FieldType.Equals(typeof(int))) {
			int value = (int)field.GetValue(this);
			if (value < (int)min) {
				field.SetValue(this, (int)min);
			}
			if (value > (int)max)
				field.SetValue(this, (int)max);
		}
		else if (field.FieldType.Equals(typeof(ObscuredInt))) {
			ObscuredInt value = (ObscuredInt)field.GetValue(this);
			ObscuredInt min_z = new ObscuredInt((int)min);
			ObscuredInt max_z = new ObscuredInt((int)max);

			if (value < min_z) {
				field.SetValue(this, min_z);
			}
			if (value > max_z)
				field.SetValue(this, max_z);
		}
	}

	private void CheckField(FieldInfo field, float min) {
		if (field.FieldType.Equals(typeof(float))) {
			float value = (float)field.GetValue(this);
			if (value < min)
				field.SetValue(this, min);

		} else if (field.FieldType.Equals(typeof(int))) {
			int value = (int)field.GetValue(this);
			if (value < (int)min)
				field.SetValue(this, (int)min);

		} else if (field.FieldType.Equals(typeof(ObscuredInt))) {
			ObscuredInt value = (ObscuredInt)field.GetValue(this);
			ObscuredInt min_z = new ObscuredInt((int)min);
			if (value < min_z) 
				field.SetValue(this, min_z);
		}
	}

    protected Dictionary<idTechTree, ObscuredInt> CheckDictionary(Dictionary<idTechTree, ObscuredInt> target, int currentCount, int fillValue)
    {
        if (target == null)
        {
            target = new Dictionary<idTechTree, ObscuredInt>();
            for (int i = 0; i < currentCount; i++)
                target.Add((idTechTree)i,0);
        }

        if (target.Count < currentCount)
        {
            for (int i = target.Count; i < currentCount; i++)            
                target.Add((idTechTree)i, 0);            
        }
        
        return target;
    }

    protected ObscuredInt[] CheckArray(ObscuredInt[] target, int currentCount, int fillValue) {
		if (target == null) {
			target = new ObscuredInt[currentCount];
			for (int i = 0; i < currentCount; i++) {
				target[i] = fillValue;
			}
		}

		if (target.Length < currentCount) {
			ObscuredInt[] tmp = new ObscuredInt[currentCount];
			Array.Copy(target, tmp, target.Length);
			for (int i = target.Length; i < tmp.Length; i++) {
				tmp[i] = fillValue;
			}
			target = tmp;
		}

		return target;
	}

	protected T[] CheckArray<T>(T[] target, int currentCount) where T : SaveDataBase {
		if (target == null) {
			target = new T[currentCount];
			for (int i = 0; i < currentCount; i++) {
				target[i] = (T)Activator.CreateInstance(typeof(T));
			}
		}

		if (target.Length < currentCount) {
			T[] tmp = new T[currentCount];
			Array.Copy(target, tmp, target.Length);
			for (int i = target.Length; i < tmp.Length; i++) {
				target[i] = (T)Activator.CreateInstance(typeof(T));
			}
			target = tmp;
		}

		return target;
	}

	protected bool[] CheckArray(bool[] target, int currentCount, bool fillValue) {
		if (target == null) {
			target = new bool[currentCount];
			for (int i = 0; i < currentCount; i++) {
				target[i] = fillValue;
			}
		}

		if (target.Length < currentCount) {
			bool[] tmp = new bool[currentCount];
			Array.Copy(target, tmp, target.Length);
			for (int i = target.Length; i < tmp.Length; i++) {
				tmp[i] = fillValue;
			}
			target = tmp;
		}

		return target;
	}

	protected ObscuredInt[] CheckArray(ObscuredInt[] target, int currentCount, int[] defaultArray, int fillValue) {
		if (target == null) {
			target = new ObscuredInt[defaultArray.Length];
			for(int i = 0; i < defaultArray.Length; i++) {
				target[i] = defaultArray[i];
			}
		}

		if (target.Length < currentCount) {
			ObscuredInt[] tmp = new ObscuredInt[currentCount];
			Array.Copy(target, tmp, target.Length);
			for (int i = target.Length; i < tmp.Length; i++) {
				tmp[i] = fillValue;
			}
			target = tmp;
		}

		return target;
	}
}
