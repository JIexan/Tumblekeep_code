using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD-счётчик кристаллов (объект Crystalls в UICanvas) + FX дропа с юнитов.
///
/// Показывает кристаллы, заработанные в ТЕКУЩЕЙ катке (MatchRewards.MatchCrystals: убийства + сундуки),
/// профиль не участвует — счётчик локальный для катки.
/// Кристалл, выпавший из юнита, рисуется UI-спрайтом: вылетает из точки юнита по дуге (как всплывающий
/// текст), зависает, затем летит по кривой Безье к иконке счётчика — и только по прилёту счётчик
/// тикает. Пока спрайт в полёте, его сумма вычитается из показываемого значения (inFlight).
/// Кристаллы из сундука (OpenChestWindow.Claim → MatchRewards.AddChestCrystals, затем в профиль)
/// тикают сразу по событию OnItemCountChanged.
/// Все твины на unscaled time — окна сундука/паузы держат timeScale = 0.
/// </summary>
public class CrystalsHudCounter : MonoBehaviour {
	public static CrystalsHudCounter Instance { get; private set; }

	[Header("Ссылки")]
	[SerializeField] private TMP_Text counter;
	[SerializeField] private RectTransform icon;
	[Tooltip("Спрайт летящего кристалла. Пусто — берётся спрайт иконки счётчика")]
	[SerializeField] private Sprite flySprite;
	[SerializeField] private Vector2 flySize = new Vector2(44f, 44f);

	[Header("Вылет из юнита (мировые единицы — спрайт привязан к точке мира, камера может двигаться)")]
	[SerializeField] private float popDuration = 0.5f;
	[SerializeField] private float popScaleDuration = 0.2f;
	[SerializeField] private float popFromScale = 0.3f;
	[Tooltip("Высота над точкой дропа (ногами юнита), откуда вылетает кристалл")]
	[SerializeField] private float popStartHeight = 1.5f;
	[Tooltip("Разлёт в стороны вдоль экрана, мин/макс")]
	[SerializeField] private Vector2 popHorizontalRange = new Vector2(0.5f, 1.2f);
	[Tooltip("Сила подброса (высота дуги), мин/макс")]
	[SerializeField] private Vector2 popHeightRange = new Vector2(0.8f, 1.4f);
	[Tooltip("На какой высоте над точкой дропа кристалл «ложится» на землю")]
	[SerializeField] private float landHeight = 0.15f;
	[SerializeField] private float maxTiltDegrees = 25f;
	[SerializeField] private float spawnJitter = 0.15f;
	[Tooltip("Пауза между вылетом и полётом к счётчику")]
	[SerializeField] private float holdDuration = 0.25f;
	[Tooltip("Доп. задержка каждого следующего кристалла одного дропа — летят цепочкой")]
	[SerializeField] private float holdStagger = 0.07f;
	[SerializeField] private int maxSpritesPerDrop = 5;

	[Header("Полёт к счётчику")]
	[SerializeField] private float flyDuration = 0.7f;
	[SerializeField] private Ease flyEase = Ease.InSine;
	[Tooltip("Боковой изгиб дуги, доля от дистанции до счётчика")]
	[SerializeField] private float curvature = 0.35f;
	[Tooltip("Подъём контрольной точки дуги, доля от дистанции")]
	[SerializeField] private float curveUpBias = 0.15f;
	[SerializeField] private float arriveScale = 0.6f;
	[SerializeField] private float spinDegrees = 180f;

	[Header("Счётчик")]
	[SerializeField] private float countDuration = 0.3f;
	[SerializeField] private float counterPunch = 0.2f;
	[SerializeField] private float iconPunch = 0.35f;
	[SerializeField] private float punchDuration = 0.3f;

	private class FlyingCrystal {
		public RectTransform rt;
		public Image image;
		public Sequence seq;
		public int amount;
	}

	private readonly Stack<FlyingCrystal> _pool = new Stack<FlyingCrystal>();
	private readonly List<FlyingCrystal> _active = new List<FlyingCrystal>();
	private RectTransform _flyLayer;
	private Canvas _rootCanvas;
	private GameController _gameController;

	private int _shownValue;
	private int _inFlightAmount;
	private Tween _countTween;

	private void Awake() {
		Instance = this;
	}

	private void OnEnable() {
		InGameEventManager.Subscribe(InGameEvents.OnItemCountChanged, OnItemCountChanged);
	}

	private void OnDisable() {
		InGameEventManager.Unsubscribe(InGameEvents.OnItemCountChanged, OnItemCountChanged);
	}

	private void Start() {
		// Стартовое значение — без анимации
		_shownValue = ComputeTotal();
		if (counter != null)
			counter.text = _shownValue.ToString();
	}

	private void OnDestroy() {
		_countTween?.Kill();
		foreach (var fc in _active)
			fc.seq?.Kill();
		_active.Clear();
		if (Instance == this)
			Instance = null;
	}

	private void OnItemCountChanged(object obj) {
		if (obj is ItemType type && type == ItemType.Crystal)
			Refresh();
	}

	// Заработано за катку: накоп с убийств + кристаллы из сундуков (без множителя за таски)
	private int ComputeTotal() {
		var rewards = ResolveGameController()?.MatchRewards;
		return rewards != null ? rewards.MatchCrystals : 0;
	}

	private GameController ResolveGameController() {
		if (_gameController == null) {
			var ui = GetComponentInParent<UIController>();
			_gameController = ui != null ? ui.gameController : FindObjectOfType<GameController>();
		}
		return _gameController;
	}

	/// <summary>Пересчитать показываемое значение и дотикать до него.</summary>
	public void Refresh() {
		int target = ComputeTotal() - _inFlightAmount;
		if (target == _shownValue)
			return;

		_countTween?.Kill();
		int from = _shownValue;
		_countTween = DOVirtual.Int(from, target, countDuration, v => {
			_shownValue = v;
			if (counter != null)
				counter.text = v.ToString();
		}).SetEase(Ease.OutQuad).SetUpdate(true);

		if (counter != null && target > from)
			Punch(counter.rectTransform, counterPunch);
	}

	/// <summary>
	/// Дроп кристаллов с юнита. worldPosition — точка «ног» юнита: кристалл вылетает с высоты
	/// popStartHeight, ложится рядом на землю (фаза в мировых координатах, каждый кадр проецируется
	/// на экран — камера может двигаться), затем летит к счётчику уже по экрану.
	/// Значение MatchRewards уже увеличено — счётчик покажет прибавку только по прилёту.
	/// </summary>
	public void DropFromWorld(Vector3 worldPosition, int amount) {
		if (amount <= 0)
			return;

		Camera cam = Camera.main;
		if (!isActiveAndEnabled || cam == null || !TryGetFlyLayer(out RectTransform layer)) {
			Refresh();
			return;
		}

		layer.SetAsLastSibling(); // поверх всего, что есть на канвасе

		int sprites = Mathf.Clamp(Mathf.Min(amount, maxSpritesPerDrop), 1, amount);
		int perSprite = amount / sprites;
		int remainder = amount - perSprite * sprites;
		for (int i = 0; i < sprites; i++) {
			int share = perSprite + (i < remainder ? 1 : 0);
			_inFlightAmount += share;
			Launch(cam, worldPosition, share, i);
		}
	}

	private void Launch(Camera cam, Vector3 origin, int amount, int index) {
		FlyingCrystal fc = GetFromPool();
		fc.amount = amount;
		RectTransform rt = fc.rt;

		// Горизонталь разлёта — вдоль экрана (как у всплывающих текстов), фиксируем на момент дропа
		Vector3 right = cam.transform.right;
		right.y = 0f;
		right = right.sqrMagnitude > 0.001f ? right.normalized : Vector3.right;

		Vector3 start = origin + Vector3.up * popStartHeight
			+ (right * Random.Range(-1f, 1f) + Vector3.up * Random.Range(-1f, 1f)) * spawnJitter;
		float side = Random.value < 0.5f ? -1f : 1f;
		float h = side * Random.Range(popHorizontalRange.x, popHorizontalRange.y);
		float height = Random.Range(popHeightRange.x, popHeightRange.y);
		float dropY = (origin.y + landHeight) - start.y; // приземление — на уровень земли у ног юнита
		Vector3 landing = start + right * h + Vector3.up * dropY;

		PlaceAtWorld(fc, start);
		rt.localScale = Vector3.one * popFromScale;
		rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-maxTiltDegrees, maxTiltDegrees));
		fc.image.color = Color.white;
		rt.gameObject.SetActive(true);

		// Безье считается в момент старта полёта — оттуда, где спрайт реально лежит на экране
		Vector2 p0 = Vector2.zero, p1 = Vector2.zero, p2 = Vector2.zero;
		bool curveReady = false;
		float curveSide = -side; // изгиб в противоположную вылету сторону — читается как «разворот»

		Sequence seq = DOTween.Sequence().SetUpdate(true);
		seq.Join(rt.DOScale(1f, popScaleDuration).SetEase(Ease.OutBack));
		// Парабола в мире: y = 4·height·t·(1−t) поверх линейного снижения к точке приземления
		seq.Join(DOVirtual.Float(0f, 1f, popDuration, t => {
			PlaceAtWorld(fc, start + right * (h * t) + Vector3.up * (dropY * t + 4f * height * t * (1f - t)));
		}).SetEase(Ease.Linear));
		// Лежит на земле: продолжаем следить за мировой точкой, пока камера едет
		seq.Append(DOVirtual.Float(0f, 1f, holdDuration + index * holdStagger, _ => PlaceAtWorld(fc, landing))
			.SetEase(Ease.Linear));
		seq.Append(DOVirtual.Float(0f, 1f, flyDuration, t => {
			if (!curveReady) {
				curveReady = true;
				fc.image.enabled = true; // мог быть скрыт, если точка мира ушла за камеру
				p0 = rt.anchoredPosition;
				p2 = GetTargetLocal();
				Vector2 dir = p2 - p0;
				float dist = dir.magnitude;
				Vector2 perp = dist > 0.01f ? new Vector2(-dir.y, dir.x) / dist : Vector2.right;
				p1 = (p0 + p2) * 0.5f + perp * (curveSide * curvature * dist) + Vector2.up * (curveUpBias * dist);
			}
			float u = 1f - t;
			rt.anchoredPosition = u * u * p0 + 2f * u * t * p1 + t * t * p2;
		}).SetEase(flyEase));
		seq.Join(rt.DOScale(arriveScale, flyDuration).SetEase(Ease.InQuad));
		seq.Join(rt.DOLocalRotate(new Vector3(0f, 0f, curveSide * spinDegrees), flyDuration, RotateMode.LocalAxisAdd).SetEase(Ease.InSine));
		seq.OnComplete(() => OnArrived(fc));

		fc.seq = seq;
		_active.Add(fc);
	}

	private void OnArrived(FlyingCrystal fc) {
		fc.seq = null;
		_inFlightAmount = Mathf.Max(0, _inFlightAmount - fc.amount);
		ReturnToPool(fc);

		if (icon != null)
			Punch(icon, iconPunch);
		Refresh();
	}

	private void Punch(RectTransform target, float strength) {
		if (strength <= 0f || target == null)
			return;
		target.DOKill();
		target.localScale = Vector3.one;
		target.DOPunchScale(Vector3.one * strength, punchDuration, 6, 0.6f).SetUpdate(true);
	}

	// ---- слой полёта и конвертация координат ----

	private bool TryGetFlyLayer(out RectTransform layer) {
		if (_flyLayer != null) {
			layer = _flyLayer;
			return true;
		}

		Canvas canvas = GetComponentInParent<Canvas>();
		if (canvas == null) {
			layer = null;
			return false;
		}
		_rootCanvas = canvas.rootCanvas;

		var go = new GameObject("CrystalsFlyLayer", typeof(RectTransform), typeof(CanvasGroup));
		_flyLayer = go.GetComponent<RectTransform>();
		_flyLayer.SetParent(_rootCanvas.transform, false);
		_flyLayer.anchorMin = Vector2.zero;
		_flyLayer.anchorMax = Vector2.one;
		_flyLayer.offsetMin = Vector2.zero;
		_flyLayer.offsetMax = Vector2.zero;
		_flyLayer.localScale = Vector3.one;
		var cg = go.GetComponent<CanvasGroup>();
		cg.blocksRaycasts = false;
		cg.interactable = false;

		layer = _flyLayer;
		return true;
	}

	private Camera CanvasCamera => _rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
		? _rootCanvas.worldCamera
		: null;

	// Проекция мировой точки на слой полёта. За камерой — прячем спрайт (проекция там зеркалится)
	private void PlaceAtWorld(FlyingCrystal fc, Vector3 world) {
		Camera cam = Camera.main;
		if (cam == null)
			return;
		Vector3 screen = cam.WorldToScreenPoint(world);
		bool inFront = screen.z > 0f;
		fc.image.enabled = inFront;
		if (inFront)
			fc.rt.anchoredPosition = ScreenToLayer(screen);
	}

	private Vector2 ScreenToLayer(Vector2 screen) {
		RectTransformUtility.ScreenPointToLocalPointInRectangle(_flyLayer, screen, CanvasCamera, out Vector2 local);
		return local;
	}

	private Vector2 GetTargetLocal() {
		Transform target = icon != null ? icon : transform;
		Vector2 screen = RectTransformUtility.WorldToScreenPoint(CanvasCamera, target.position);
		return ScreenToLayer(screen);
	}

	// ---- пул спрайтов ----

	private FlyingCrystal GetFromPool() {
		if (_pool.Count > 0)
			return _pool.Pop();

		var go = new GameObject("Crystal", typeof(RectTransform), typeof(Image));
		var rt = go.GetComponent<RectTransform>();
		rt.SetParent(_flyLayer, false);
		rt.sizeDelta = flySize;
		var img = go.GetComponent<Image>();
		img.sprite = flySprite != null ? flySprite : (icon != null ? icon.GetComponent<Image>()?.sprite : null);
		img.preserveAspect = true;
		img.raycastTarget = false;
		return new FlyingCrystal { rt = rt, image = img };
	}

	private void ReturnToPool(FlyingCrystal fc) {
		_active.Remove(fc);
		fc.rt.gameObject.SetActive(false);
		_pool.Push(fc);
	}
}
