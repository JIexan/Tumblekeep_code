using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour {

	private static T _instance;
	public static T Instance {
		get {
			if (_instance == null) {
				_instance = FindObjectOfType<T>();
				if (_instance == null) {
					//_instance = new GameObject(typeof(T).Name + "Singleton", typeof(T)).GetComponent<T>();
					Debug.LogWarning("Singleton not found in scene: " + typeof(T).Name);
				}
				//DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}

	protected virtual void Awake() {
		// Instance мог быть закэширован через FindObjectOfType до нашего Awake (чужой OnEnable) —
		// это мы сами, а не дубликат; уничтожаем только настоящий второй экземпляр
		if (_instance != null && _instance != GetComponent<T>()) {
			Destroy(this);
		} else {
			_instance = GetComponent<T>();
			//DontDestroyOnLoad(gameObject);
		}
	}

}
