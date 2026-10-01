using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class CharacteristicsCollection : ISerializationCallbackReceiver, IEnumerable
{
	[System.Serializable]
	private class CharacterististicsDictionary : SerializableDictionary<CharacteristicType, Characteristic> { }

	[Indent(-1)]
	[SerializeField] private CharacterististicsDictionary characteristics = new CharacterististicsDictionary();

	public int Count => characteristics.Count;

	public void Upgrade(CharacteristicUpgradeData data) {
		var characteristic = Get(data.characteristicType);
		if (characteristic != null) {
			characteristic.AddBonus(data.additionalValue);
			characteristic.AddPercent(data.persentValue);
		}
	}

	public void ModifyValue(CharacteristicType type, float multiplier) {
		var characteristic = Get(type);
		if (characteristic != null) {
			characteristic.ModifyByPersent(multiplier);
		}
	}

	public Characteristic Get(CharacteristicType type) {
		if (characteristics.ContainsKey(type))
			return characteristics[type];
		return null;
	}

	public float GetValue(CharacteristicType type) {
		if (characteristics.ContainsKey(type))
			return characteristics[type];
		return 0f;
	}

	public bool Contains(CharacteristicType type) {
		return characteristics.ContainsKey(type);
	}

	public void Add(CharacteristicType type, bool setMinValue = false) {
		var newCharacteristic = new Characteristic(0f) { type = type };
		if(setMinValue)
			newCharacteristic.SetBaseValueToMin();
		characteristics.Add(type, newCharacteristic);
	}

	public Characteristic GetRandomNotMaxed(List<CharacteristicType> exceptions) {
		var matchingItems = characteristics.Values.Where(item => item.IsMaxed && (exceptions != null ? !exceptions.Contains(item.type) : true)).ToList();

		if (matchingItems.Count == 0)
			return null;

		int randomIndex = UnityEngine.Random.Range(0, matchingItems.Count);
		var res = matchingItems[randomIndex];
		exceptions.Add(res.type);
		return res;
	}

	public void OnBeforeSerialize() {
	}

	public void OnAfterDeserialize() {
		foreach(var characteristic in characteristics) {
			characteristic.Value.type = characteristic.Key;
		}
	}

	public void FilterTypes(List<CharacteristicType> possible) {
		foreach (var characteristic in characteristics) {
			if(possible.Contains(characteristic.Key))
				possible.Remove(characteristic.Key);
		}
	}

	public IEnumerator<KeyValuePair<CharacteristicType, Characteristic>> GetEnumerator() {
		return ((IDictionary<CharacteristicType, Characteristic>)characteristics).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return ((IDictionary<CharacteristicType, Characteristic>)characteristics).GetEnumerator();
	}
}
