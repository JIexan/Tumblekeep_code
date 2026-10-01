using UnityEngine;
using DG.Tweening;
using TMPro;

public enum FloatingTextMotion {
	Toss,    // подброс по параболе в случайную сторону
	FloatUp  // спокойный подъём вверх
}

[System.Serializable]
public class FloatingTextSettings {
	[Header("Внешний вид")]
	public Color textColor = Color.white;
	public float fontSize = 2f;        // размер при sizeT = 0
	public float fontSizeMax = 2f;     // размер при sizeT = 1 (равен fontSize — без скейла)
	public float maxTiltDegrees = 0f;  // случайный наклон текста, +- градусы
	public float spawnJitter = 0f;     // случайный сдвиг точки спавна в плоскости экрана

	[Header("Маппинг значения в размер (используется менеджером)")]
	public float valueForMinSize = 1f;
	public float valueForMaxSize = 40f;

	[Header("Появление")]
	public float appearDuration = 0.15f;
	public Ease appearEase = Ease.OutBack;
	public float appearFromScale = 0.3f;

	[Header("Движение")]
	public FloatingTextMotion motion = FloatingTextMotion.Toss;
	public float moveDuration = 0.75f;
	public float tossHorizontalMin = 0.4f;
	public float tossHorizontalMax = 1f;
	public float tossJumpPowerMin = 0.8f;
	public float tossJumpPowerMax = 1.5f;
	public float tossEndYOffset = -0.2f;
	public float floatUpDistance = 2.5f;
	public Ease floatUpEase = Ease.OutQuad;

	[Header("Исчезновение (<= 0 — без фейда)")]
	public float fadeOutDuration = 0.25f;
}

/// <summary>
/// Один всплывающий текст из пула: анимируется единым DOTween-сиквенсом,
/// билбордится к камере с небольшим случайным наклоном.
/// </summary>
public class FloatingText : MonoBehaviour {
	[SerializeField] private TMP_Text textMesh;

	private FloatingTextSystem _system;
	private Transform _cam;
	private Sequence _seq;
	private float _tiltZ;

	public void Initialize(FloatingTextSystem system) {
		_system = system;
		gameObject.SetActive(false);
	}

	public void Show(string text, Vector3 worldPosition, FloatingTextSettings settings, float sizeT, Transform cam) {
		_seq?.Kill();
		_seq = null;

		_cam = cam;
		_tiltZ = Random.Range(-settings.maxTiltDegrees, settings.maxTiltDegrees);

		// Горизонтальная ось разлёта — вдоль экрана, чтобы дуги читались с любого ракурса камеры
		Vector3 right = Vector3.right;
		if (_cam != null) {
			right = _cam.right;
			right.y = 0f;
			right = right.sqrMagnitude > 0.001f ? right.normalized : Vector3.right;
		}

		Vector3 start = worldPosition;
		if (settings.spawnJitter > 0f) {
			start += (right * Random.Range(-1f, 1f) + Vector3.up * Random.Range(-1f, 1f)) * settings.spawnJitter;
		}

		transform.position = start;
		ApplyBillboard();

		textMesh.text = text;
		textMesh.fontSize = Mathf.Lerp(settings.fontSize, settings.fontSizeMax, Mathf.Clamp01(sizeT));
		// Рождаемся сразу непрозрачными: появление читается по scale-попу, фейд — только в конце
		textMesh.color = settings.textColor;
		transform.localScale = Vector3.one * settings.appearFromScale;
		gameObject.SetActive(true);

		// Все твины строго внутри одного сиквенса — ResetObject убивает его целиком, без утечек
		_seq = DOTween.Sequence();
		_seq.Join(transform.DOScale(1f, settings.appearDuration).SetEase(settings.appearEase));

		if (settings.motion == FloatingTextMotion.Toss) {
			// Ручная парабола: горизонталь линейна, высота y = 4 * power * t * (1 - t) — читается как подброс
			float side = Random.value < 0.5f ? -1f : 1f;
			float h = side * Random.Range(settings.tossHorizontalMin, settings.tossHorizontalMax);
			float power = Random.Range(settings.tossJumpPowerMin, settings.tossJumpPowerMax);
			float endY = settings.tossEndYOffset;
			_seq.Join(DOVirtual.Float(0f, 1f, settings.moveDuration, t => {
				transform.position = start
					+ right * (h * t)
					+ Vector3.up * (endY * t + 4f * power * t * (1f - t));
			}).SetEase(Ease.Linear));
		} else {
			_seq.Join(transform.DOMoveY(start.y + settings.floatUpDistance, settings.moveDuration).SetEase(settings.floatUpEase));
		}

		float fade = Mathf.Min(settings.fadeOutDuration, settings.moveDuration);
		if (fade > 0f) {
			_seq.Insert(Mathf.Max(0f, settings.moveDuration - fade), textMesh.DOFade(0f, fade));
		}

		_seq.OnComplete(ReturnToPool);
	}

	/// <summary>
	/// Останавливает анимацию и деактивирует объект. Вызывается системой
	/// при возврате в пул и при переиспользовании самого старого текста.
	/// </summary>
	public void ResetObject() {
		_seq?.Kill();
		_seq = null;
		gameObject.SetActive(false);
	}

	private void ReturnToPool() {
		_seq = null; // сиквенс завершился сам — убивать нечего
		_system.ReturnToPool(this);
	}

	private void LateUpdate() {
		ApplyBillboard();
	}

	private void ApplyBillboard() {
		if (_cam != null) {
			transform.rotation = _cam.rotation * Quaternion.Euler(0f, 0f, _tiltZ);
		}
	}
}
