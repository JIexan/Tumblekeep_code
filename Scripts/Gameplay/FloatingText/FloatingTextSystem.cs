using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Пул всплывающих текстов. Живёт в пределах сцены: создаётся лениво
/// FloatingTextManager-ом из Resources, вместе со сценой и умирает —
/// поэтому ссылка на камеру не протухает. Фасад для геймплея — FloatingTextManager.
/// </summary>
public class FloatingTextSystem : MonoBehaviour {
	[Header("References")]
	[SerializeField] private FloatingText _floatingTextPrefab;

	[Header("Pool Settings")]
	[SerializeField] private int _initialPoolSize = 12;
	[SerializeField] private int _maxPoolSize = 48;

	[Header("Default Settings")]
	[SerializeField] private FloatingTextSettings _defaultSettings = new FloatingTextSettings();

	private readonly Queue<FloatingText> _pool = new Queue<FloatingText>();
	private readonly List<FloatingText> _activeTexts = new List<FloatingText>();
	private Transform _poolContainer;
	private Transform _cameraTransform;

	// Камера разрешается лениво — на момент Awake её может ещё не быть
	private Transform CameraTransform {
		get {
			if (_cameraTransform == null && Camera.main != null) {
				_cameraTransform = Camera.main.transform;
			}
			return _cameraTransform;
		}
	}

	// Инициализация в Awake: бутстрап из Resources показывает первый текст в кадре создания
	private void Awake() {
		_poolContainer = new GameObject("FloatingTextPool").transform;
		_poolContainer.SetParent(transform);

		for (int i = 0; i < _initialPoolSize; i++) {
			_pool.Enqueue(CreateNewText());
		}
	}

	private FloatingText CreateNewText() {
		FloatingText text = Instantiate(_floatingTextPrefab, _poolContainer);
		text.Initialize(this);
		return text;
	}

	public void ShowText(string text, Vector3 worldPosition, FloatingTextSettings settings = null, float sizeT = 0f) {
		FloatingText floatingText = GetTextFromPool();
		floatingText.Show(text, worldPosition, settings ?? _defaultSettings, sizeT, CameraTransform);
		_activeTexts.Add(floatingText);
	}

	private FloatingText GetTextFromPool() {
		if (_pool.Count > 0) {
			return _pool.Dequeue();
		}

		if (_activeTexts.Count < _maxPoolSize) {
			return CreateNewText();
		}

		// Пул исчерпан — переиспользуем самый старый активный текст
		FloatingText oldest = _activeTexts[0];
		_activeTexts.RemoveAt(0);
		oldest.ResetObject();
		return oldest;
	}

	public void ReturnToPool(FloatingText floatingText) {
		if (!_activeTexts.Remove(floatingText)) {
			return; // уже переиспользован recycle-ом — двойной возврат не допускаем
		}

		floatingText.ResetObject();
		_pool.Enqueue(floatingText);
	}
}
