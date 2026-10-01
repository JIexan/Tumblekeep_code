using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class ObscuredIntConverter : JsonConverter {
	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
		writer.WriteValue((value as ObscuredInt).Get());
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
		int val = Convert.ToInt32(reader.Value);
		if (existingValue != null) {
			(existingValue as ObscuredInt).Set(val);
			return existingValue;
		} else
			return new ObscuredInt(val);
	}

	public override bool CanConvert(Type objectType) {
		return objectType == typeof(ObscuredInt);
	}
}

[Serializable]
public class ObscuredInt : IEquatable<ObscuredInt>, IFormattable {
	private const int DEFAULT_CRYPTOKEY = 696969;

	[SerializeField] private int currentCryptoKey;
	[SerializeField] private int hiddenValue;
	[SerializeField] private int hiddenValue2;
	[SerializeField] private int fakeValue;

	public ObscuredInt() {
		System.Random r = new System.Random();
		currentCryptoKey = r.Next(0, int.MaxValue);// (int)(Random.value * int.MaxValue);
		fakeValue = 0;
		hiddenValue2 = (~0) ^ currentCryptoKey;
		hiddenValue = 0 ^ currentCryptoKey;
	}

	public ObscuredInt(int value) {
		System.Random r = new System.Random();
		currentCryptoKey = r.Next(0, int.MaxValue); //(int)(Random.value * int.MaxValue);
		fakeValue = value;
		hiddenValue2 = (~value) ^ currentCryptoKey;
		hiddenValue = value ^ currentCryptoKey;
	}

	public void ApplyNewCryptoKey() {
		int value = Get();
		currentCryptoKey = (int)(UnityEngine.Random.value * int.MaxValue);
		Set(value);
	}

	public int Get() {
		int decrypted = hiddenValue ^ currentCryptoKey;
		int decrypted2 = ~(hiddenValue2 ^ currentCryptoKey);

		if (decrypted != decrypted2 || fakeValue != decrypted) {
			Debug.LogError("filthy cheater");
		}

		/*if (Detectors.ObscuredCheatingDetector.IsRunning && fakeValue != 0 && decrypted != fakeValue)
		{
			Detectors.ObscuredCheatingDetector.Instance.OnCheatingDetected();
		}*/

		return decrypted;
	}

	public void Set(int value) {
		hiddenValue = value ^ currentCryptoKey;
		hiddenValue2 = (~value) ^ currentCryptoKey;
		fakeValue = value;
	}

	#region operators, overrides, interface implementations
	public static implicit operator ObscuredInt(int value) {
		ObscuredInt obscured = new ObscuredInt(value);
		return obscured;
	}

	public static implicit operator int(ObscuredInt value) {
		return value.Get();
	}

	public static ObscuredInt operator ++(ObscuredInt input) {
		input.Set(input.Get() + 1);
		return input;
	}

	public static ObscuredInt operator --(ObscuredInt input) {
		input.Set(input.Get() - 1);
		return input;
	}

	public static bool operator ==(ObscuredInt obj1, ObscuredInt obj2) {
		if (ReferenceEquals(obj1, obj2)) {
			return true;
		}
		if (ReferenceEquals(obj1, null)) {
			return false;
		}
		if (ReferenceEquals(obj2, null)) {
			return false;
		}

		return obj1.Equals(obj2);
	}

	public static bool operator !=(ObscuredInt obj1, ObscuredInt obj2) {
		return !(obj1 == obj2);
	}


	public override bool Equals(object obj) {
		if (!(obj is ObscuredInt))
			return false;
		return Equals((ObscuredInt)obj);
	}

	public bool Equals(ObscuredInt obj) {
		if (currentCryptoKey == obj.currentCryptoKey) {
			return hiddenValue == obj.hiddenValue;
		}

		return Get() == obj.Get();
	}

	public override int GetHashCode() {
		return Get().GetHashCode();
	}

	public override string ToString() {
		return Get().ToString();
	}

	public string ToString(string format) {
		return Get().ToString(format);
	}

	public string ToString(IFormatProvider provider) {
		return Get().ToString(provider);
	}

	public string ToString(string format, IFormatProvider provider) {
		return Get().ToString(format, provider);
	}

	#endregion

}
