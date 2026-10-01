using System.Globalization;
using UnityEngine;

/// <summary>
/// Фасад всплывающих текстов: семантические пресеты (урон, крит, хил, XP, урон замку, инфо)
/// и ленивый бутстрап из Resources — работает в любой сцене без ручной расстановки.
/// </summary>
public class FloatingTextManager : MonoBehaviour {
	private const string PrefabResourcePath = "FloatingText/FloatingTextSystem";

	[SerializeField] private FloatingTextSystem system;

	[SerializeField] private FloatingTextSettings damageSettings = new FloatingTextSettings {
		textColor = new Color(1f, 0.89f, 0.27f),
		fontSize = 4f,
		fontSizeMax = 7.2f,
		valueForMinSize = 1f,
		valueForMaxSize = 40f,
		maxTiltDegrees = 12f,
		spawnJitter = 0.15f,
		appearDuration = 0.15f,
		appearFromScale = 0.3f,
		motion = FloatingTextMotion.Toss,
		moveDuration = 0.75f,
		tossHorizontalMin = 0.4f,
		tossHorizontalMax = 1f,
		tossJumpPowerMin = 0.8f,
		tossJumpPowerMax = 1.5f,
		tossEndYOffset = -0.2f,
		fadeOutDuration = 0.25f
	};

	[SerializeField] private FloatingTextSettings criticalSettings = new FloatingTextSettings {
		textColor = new Color(1f, 0.55f, 0.1f),
		fontSize = 5.6f,
		fontSizeMax = 8.8f,
		valueForMinSize = 5f,
		valueForMaxSize = 80f,
		maxTiltDegrees = 18f,
		spawnJitter = 0f,
		appearDuration = 0.12f,
		appearFromScale = 0.1f,
		motion = FloatingTextMotion.Toss,
		moveDuration = 0.9f,
		tossHorizontalMin = 0.6f,
		tossHorizontalMax = 1.2f,
		tossJumpPowerMin = 1.2f,
		tossJumpPowerMax = 1.8f,
		tossEndYOffset = -0.1f,
		fadeOutDuration = 0.3f
	};

	[SerializeField] private FloatingTextSettings healSettings = new FloatingTextSettings {
		textColor = new Color(0.42f, 1f, 0.42f),
		fontSize = 4f,
		fontSizeMax = 5.6f,
		valueForMinSize = 1f,
		valueForMaxSize = 40f,
		maxTiltDegrees = 5f,
		spawnJitter = 0.1f,
		appearDuration = 0.15f,
		appearFromScale = 0.4f,
		motion = FloatingTextMotion.FloatUp,
		moveDuration = 0.9f,
		floatUpDistance = 1.6f,
		fadeOutDuration = 0.3f
	};

	[SerializeField] private FloatingTextSettings xpSettings = new FloatingTextSettings {
		textColor = new Color(1f, 0.84f, 0f),
		fontSize = 5f,
		fontSizeMax = 5f,
		appearDuration = 0.2f,
		appearFromScale = 0.5f,
		motion = FloatingTextMotion.FloatUp,
		moveDuration = 0.8f,
		floatUpDistance = 2.5f,
		fadeOutDuration = 0.3f
	};

	[SerializeField] private FloatingTextSettings castleDamageSettings = new FloatingTextSettings {
		textColor = new Color(1f, 0.23f, 0.19f),
		fontSize = 6.4f,
		fontSizeMax = 9.6f,
		valueForMinSize = 1f,
		valueForMaxSize = 8f,
		maxTiltDegrees = 8f,
		spawnJitter = 0.2f,
		appearDuration = 0.18f,
		appearFromScale = 0.2f,
		motion = FloatingTextMotion.Toss,
		moveDuration = 1f,
		tossHorizontalMin = 0.8f,
		tossHorizontalMax = 1.6f,
		tossJumpPowerMin = 1.5f,
		tossJumpPowerMax = 2.2f,
		tossEndYOffset = -0.3f,
		fadeOutDuration = 0.35f
	};

	[SerializeField] private FloatingTextSettings infoSettings = new FloatingTextSettings {
		textColor = new Color(0.3f, 0.87f, 1f),
		fontSize = 4f,
		fontSizeMax = 4f,
		appearDuration = 0.2f,
		appearFromScale = 0.5f,
		motion = FloatingTextMotion.FloatUp,
		moveDuration = 1f,
		floatUpDistance = 2f,
		fadeOutDuration = 0.3f
	};

	private static FloatingTextManager _instance;
	private static bool _quitting;

	public static FloatingTextManager Instance {
		get {
			if (_instance == null && Application.isPlaying && !_quitting) {
				Bootstrap();
			}
			return _instance;
		}
	}

	private static void Bootstrap() {
		GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
		if (prefab == null) {
			Debug.LogError($"FloatingTextManager: префаб не найден в Resources по пути '{PrefabResourcePath}'");
			return;
		}
		Instantiate(prefab); // Awake синхронно выставит _instance
	}

	// Сброс статиков на случай отключённого domain reload в редакторе
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStatics() {
		_instance = null;
		_quitting = false;
	}

	private void Awake() {
		if (_instance == null) {
			_instance = this;
		} else if (_instance != this) {
			Destroy(gameObject);
		}
	}

	private void OnDestroy() {
		if (_instance == this) {
			_instance = null;
		}
	}

	private void OnApplicationQuit() {
		_quitting = true;
	}

	public void ShowDamage(float amount, Vector3 position, bool isCritical = false) {
		FloatingTextSettings settings = isCritical ? criticalSettings : damageSettings;
		system.ShowText(FormatAmount(amount), position, settings, GetSizeT(settings, amount));
	}

	public void ShowHeal(float amount, Vector3 position) {
		system.ShowText("+" + FormatAmount(amount), position, healSettings, GetSizeT(healSettings, amount));
	}

	public void ShowCastleDamage(float amount, Vector3 position) {
		system.ShowText(FormatAmount(amount), position, castleDamageSettings, GetSizeT(castleDamageSettings, amount));
	}

	public void ShowXP(int amount, Vector3 position) {
		system.ShowText(amount.ToString(), position, xpSettings);
	}

	public void ShowInfo(string message, Vector3 position) {
		system.ShowText(message, position, infoSettings);
	}

	// Чем больше число — тем крупнее текст; sqrt смягчает кривую, средние значения уже заметны
	private static float GetSizeT(FloatingTextSettings settings, float amount) {
		return Mathf.Sqrt(Mathf.Clamp01(Mathf.InverseLerp(settings.valueForMinSize, settings.valueForMaxSize, amount)));
	}

	// Целые — без дробной части, дробные — один знак (урон замку после брони бывает 0.5)
	private static string FormatAmount(float value) {
		float rounded = Mathf.Round(value * 10f) / 10f;
		if (rounded % 1f == 0f) {
			return ((int)rounded).ToString();
		}
		return rounded.ToString("0.0", CultureInfo.InvariantCulture);
	}
}
