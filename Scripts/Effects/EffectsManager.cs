using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum VisualEffectType {
	Blood,
	Heal,
	MechanicalDamage,
	Electricity,
	Freeze,
	Fire,
	Poison
}
public class EffectsManager : Singleton<EffectsManager> {
	[System.Serializable]
	private class EffectPreloder {
		public int count;
		public GameObject obj;
		public VisualEffectType type;
	}

	[SerializeField] EffectPreloder[] effects;

	private Dictionary<VisualEffectType, GameObject> objects;

    // Start is called before the first frame update
    void Start()
    {
		objects = new Dictionary<VisualEffectType, GameObject>(effects.Length);
		foreach (var p in effects) {
			SimplePool.Preload(p.obj, p.count);
			objects.Add(p.type, p.obj);
		}
    }
	private ParticleSystemEffect CreateEffect(VisualEffectType type) {
		if(!objects.ContainsKey(type)) {
			Debug.LogError("Unknown type of effect " + type.ToString());
			return null;
		}
		GameObject obj = SimplePool.Spawn(objects[type]);
		ParticleSystemEffect effect = obj.GetComponent<ParticleSystemEffect>();
		if(effect == null) {
			Debug.LogError("No ParticleSystemEffect component on effect prefab. Please fix.");
			return null;
		}
		return effect;
	}

	public ParticleSystemEffect Spawn(VisualEffectType type) {
		var effect = CreateEffect(type);
		if (effect != null) {
			effect.Init(false);
		}

		return effect;
	}

	public ParticleSystemEffect Spawn(VisualEffectType type, float duration) {
		var effect = CreateEffect(type);
		if (effect != null) {
			effect.SetDuration(duration);
			effect.SetLifetimeMultiplier(duration);
			effect.Init(false);
		}

		return effect;
	}

	public ParticleSystemEffect Spawn(VisualEffectType type, Transform parent) {
		ParticleSystemEffect obj = Spawn(type);
		if (obj != null)
			obj.transform.parent = parent;
		return obj;
	}

	public ParticleSystemEffect Spawn(VisualEffectType type, Transform parent, Vector3 position) {
		ParticleSystemEffect obj = Spawn(type, parent);
		if (obj != null)
			obj.transform.position = position;
		return obj;
	}

	public ParticleSystemEffect Spawn(VisualEffectType type, Transform parent, Vector3 position, Quaternion rotation) {
		ParticleSystemEffect obj = Spawn(type, parent, position);
		if (obj != null)
			obj.transform.rotation = rotation;
		return obj;
	}

	public ParticleSystemEffect Spawn(VisualEffectType type, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale) {
		ParticleSystemEffect obj = Spawn(type, parent, position, rotation);
		if (obj != null)
			obj.transform.localScale = scale;
		return obj;
	}

	public ParticleSystemEffect SpawnLooping(VisualEffectType type) {
		var effect = CreateEffect(type);
		if(effect != null)
			effect.Init(true);

		return effect;
	}

	public ParticleSystemEffect SpawnLooping(VisualEffectType type, Transform parent) {
		ParticleSystemEffect obj = SpawnLooping(type);
		if (obj != null)
			obj.transform.parent = parent;
		return obj;
	}

	public ParticleSystemEffect SpawnLooping(VisualEffectType type, Transform parent, Vector3 position) {
		ParticleSystemEffect obj = SpawnLooping(type, parent);
		if (obj != null)
			obj.transform.position = position;
		return obj;
	}

	public ParticleSystemEffect SpawnLooping(VisualEffectType type, Transform parent, Vector3 position, Quaternion rotation) {
		ParticleSystemEffect obj = SpawnLooping(type, parent, position);
		if (obj != null)
			obj.transform.rotation = rotation;
		return obj;
	}

	public ParticleSystemEffect SpawnLooping(VisualEffectType type, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale) {
		ParticleSystemEffect obj = SpawnLooping(type, parent, position, rotation);
		if (obj != null)
			obj.transform.localScale = scale;
		return obj;
	}

}
