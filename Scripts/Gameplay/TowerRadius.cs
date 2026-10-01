using UnityEngine;
using static UnityEngine.ParticleSystem;

public class TowerRadius : MonoBehaviour
{
	[SerializeField] private RadialCuboidsGenerator dashes;
	[SerializeField] private MeshRenderer meshRenderer;
	[SerializeField] private ParticleSystem glow;

	[SerializeField] private MinMaxGradient gradient;

	[SerializeField, Min(0.1f)] private float cycleDuration = 3f;
	[SerializeField] private string colorPropertyName = "_Color";
	[SerializeField] private bool useSharedMaterial = false;

	private Material targetMaterial;
	
	private float currentTime;
	private float startOffset;

	public void Init(float radius) {
		dashes.Generate(radius);
		var main = glow.main;
		main.startSize = radius * 2;
	}

	public void SetColor(MinMaxGradient gradient) {
		this.gradient = gradient;
	}

	public void Play() {
		gameObject.SetActive(true);
		Initialize();
	}

	public void Stop() {
		gameObject.SetActive(false);
	}

	void Initialize() {
		meshRenderer = GetComponent<MeshRenderer>();

		if (meshRenderer == null) {
			Debug.LogError("MeshRenderer не найден на объекте!", this);
			enabled = false;
			return;
		}

		// Получаем материал в зависимости от настройки
		if (useSharedMaterial) {
			targetMaterial = meshRenderer.sharedMaterial;
		} else {
			targetMaterial = meshRenderer.material;
		}

		if (targetMaterial == null) {
			Debug.LogError("Материал не найден!", this);
			enabled = false;
			return;
		}

		// Проверяем наличие свойства
		if (!targetMaterial.HasProperty(colorPropertyName)) {
			Debug.LogError($"Материал не имеет свойства '{colorPropertyName}'!", this);
			enabled = false;
			return;
		}

		// Начальное смещение
		startOffset = 0f;
		currentTime = startOffset;
	}

	void Update() {
		if (targetMaterial == null || !targetMaterial.HasProperty(colorPropertyName))
			return;

		// Обновляем время
		currentTime += Time.deltaTime;

		// Зацикливаем время
		currentTime %= cycleDuration;

		// Обновляем цвет
		UpdateColor();
	}

	void UpdateColor() {
		if (targetMaterial == null) return;

		// Нормализованное время (от 0 до 1)
		float normalizedTime = currentTime / cycleDuration;

		// Получаем цвет из градиента
		Color currentColor = gradient.Evaluate(normalizedTime);

		// Устанавливаем цвет в материал
		targetMaterial.SetColor(colorPropertyName, currentColor);
	}

	void OnDestroy() {
		// Очистка созданных материалов, если мы не используем shared material
		if (!useSharedMaterial && Application.isPlaying) {
			if (targetMaterial != null) {
				Destroy(targetMaterial);
			}
		}
	}
}
