using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Tooltips {
	/// <summary>
	/// Всплывающие подсказки. Одна подсказка на экран, собственный overlay-канвас поверх всего UI;
	/// ленивый бутстрап из Resources (как FloatingTextManager) — работает в любой сцене без расстановки.
	/// Контент — префаб-наследник TooltipContent: менеджер держит по инстансу на префаб и перед показом
	/// отдаёт его в колбек конфигурации. Время — unscaled: окна ставят timeScale = 0.
	/// </summary>
	public class TooltipManager : MonoBehaviour {
		private const string PrefabResourcePath = "Tooltips/TooltipSystem";

		[SerializeField] Canvas canvas;
		[SerializeField] TooltipView view;
		[Tooltip("Стандартные контенты: Show<T> без префаба ищет здесь по типу.")]
		[SerializeField] List<TooltipContent> contentPrefabs = new List<TooltipContent>();

		[Header("Появление")]
		[Tooltip("Задержка перед показом. Переезд уже видимой подсказки на соседний элемент — без задержки.")]
		[SerializeField] float showDelay = 0.3f;
		[SerializeField] float fadeInDuration = 0.08f;
		[SerializeField] float fadeOutDuration = 0.08f;

		private readonly Dictionary<TooltipContent, TooltipContent> instances = new Dictionary<TooltipContent, TooltipContent>();
		private TooltipContent current;
		private object owner;
		private TooltipPlacement placement;
		private bool hasTarget;
		private Camera targetCamera;
		private float delayLeft;
		private float alpha;

		private static TooltipManager _instance;
		private static bool _quitting;

		private static TooltipManager Instance {
			get {
				if (_instance == null && Application.isPlaying && !_quitting)
					Bootstrap();
				return _instance;
			}
		}

		// ---------------------------------------------------------------------
		// API
		// ---------------------------------------------------------------------

		// owner — кто запросил показ: Hide(owner) прячет подсказку, только если она всё ещё его,
		// а уничтоженный owner (UnityEngine.Object) прячет её сам
		public static void Show<T>(object owner, T contentPrefab, Action<T> configure, TooltipPlacement placement) where T : TooltipContent {
			var manager = Instance;
			if (manager != null)
				manager.ShowInternal(owner, contentPrefab, configure, placement);
		}

		// Стандартный контент — префаб типа T из списка менеджера
		public static void Show<T>(object owner, Action<T> configure, TooltipPlacement placement) where T : TooltipContent {
			var manager = Instance;
			if (manager != null)
				manager.ShowInternal(owner, manager.FindStandardPrefab<T>(), configure, placement);
		}

		public static void ShowText(object owner, string text, TooltipPlacement placement) {
			Show<TextTooltipContent>(owner, content => content.Set(text), placement);
		}

		public static void ShowTitled(object owner, string title, string text, TooltipPlacement placement) {
			Show<TitledTextTooltipContent>(owner, content => content.Set(title, text), placement);
		}

		public static void Hide(object owner) {
			// Без бутстрапа: прятать нечего, а зовут и из OnDisable при выгрузке сцены
			if (_instance != null && ReferenceEquals(_instance.owner, owner))
				_instance.owner = null;
		}

		// ---------------------------------------------------------------------
		// Бутстрап
		// ---------------------------------------------------------------------

		private static void Bootstrap() {
			var prefab = Resources.Load<GameObject>(PrefabResourcePath);
			if (prefab == null) {
				Debug.LogError($"TooltipManager: префаб не найден в Resources по пути '{PrefabResourcePath}' (собирается через Tools/UI/Build Tooltip System)");
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
				view.SetActive(false);
				view.SetAlpha(0f);
			} else if (_instance != this) {
				Destroy(gameObject);
			}
		}

		private void OnDestroy() {
			if (_instance == this)
				_instance = null;
		}

		private void OnApplicationQuit() {
			_quitting = true;
		}

		// ---------------------------------------------------------------------
		// Показ
		// ---------------------------------------------------------------------

		private void ShowInternal<T>(object owner, T contentPrefab, Action<T> configure, TooltipPlacement placement) where T : TooltipContent {
			if (owner == null || contentPrefab == null)
				return;

			var content = (T)GetInstance(contentPrefab);
			if (current != null && current != content)
				current.gameObject.SetActive(false);
			current = content;

			// Пузырь включается до контента: TMP внутри должен проснуться до Rebuild
			view.SetActive(true);
			content.gameObject.SetActive(true);
			configure?.Invoke(content);
			content.Rebuild();
			view.Fit(content);

			this.owner = owner;
			this.placement = placement;
			hasTarget = placement.target != null;
			targetCamera = hasTarget ? ResolveCamera(placement.target) : null;
			// Подсказка ещё на экране (переезд с соседнего элемента) — показываем сразу
			if (alpha <= 0f)
				delayLeft = showDelay;
			UpdatePlacement();
		}

		private T FindStandardPrefab<T>() where T : TooltipContent {
			foreach (var prefab in contentPrefabs) {
				if (prefab is T typed)
					return typed;
			}
			Debug.LogError($"TooltipManager: в списке стандартных контентов нет префаба типа {typeof(T).Name}");
			return null;
		}

		private TooltipContent GetInstance(TooltipContent prefab) {
			if (instances.TryGetValue(prefab, out var instance) && instance != null)
				return instance;
			instance = Instantiate(prefab);
			instance.gameObject.SetActive(false);
			view.Attach(instance);
			instances[prefab] = instance;
			return instance;
		}

		private void LateUpdate() {
			if (owner != null && !IsOwnerAlive())
				owner = null;

			float dt = Time.unscaledDeltaTime;
			if (owner != null) {
				if (delayLeft > 0f)
					delayLeft -= dt;
				else
					alpha = fadeInDuration > 0f ? Mathf.MoveTowards(alpha, 1f, dt / fadeInDuration) : 1f;
				UpdatePlacement();
			} else if (view.IsActive) {
				alpha = fadeOutDuration > 0f ? Mathf.MoveTowards(alpha, 0f, dt / fadeOutDuration) : 0f;
				if (alpha <= 0f) {
					view.SetActive(false);
					if (current != null)
						current.gameObject.SetActive(false);
				}
			}
			view.SetAlpha(alpha);
		}

		private bool IsOwnerAlive() {
			if (owner is UnityEngine.Object unityOwner && unityOwner == null)
				return false;
			// Цель уничтожили или выключили, а PointerExit не пришёл
			return !hasTarget || (placement.target != null && placement.target.gameObject.activeInHierarchy);
		}

		private void UpdatePlacement() {
			float scale = canvas.scaleFactor;
			Vector2 canvasSize = ((RectTransform)canvas.transform).rect.size;
			Vector2 anchorPos;
			Vector2 flippedAnchorPos;
			if (hasTarget) {
				anchorPos = GetScreenPoint(placement.anchor) / scale;
				flippedAnchorPos = GetScreenPoint(Mirror(placement.anchor, placement.direction)) / scale;
			} else {
				anchorPos = flippedAnchorPos = (Vector2)Input.mousePosition / scale;
			}
			view.Place(anchorPos, flippedAnchorPos, canvasSize, placement.direction);
		}

		private Vector2 GetScreenPoint(Vector2 anchor) {
			var target = placement.target;
			Rect rect = target.rect;
			Vector3 world = target.TransformPoint(new Vector3(rect.xMin + rect.width * anchor.x, rect.yMin + rect.height * anchor.y, 0f));
			return RectTransformUtility.WorldToScreenPoint(targetCamera, world);
		}

		// Якорь для перевёрнутого пузыря — зеркальная точка по оси показа
		private static Vector2 Mirror(Vector2 anchor, TooltipDirection direction) {
			return TooltipPlacement.IsVertical(direction) ? new Vector2(anchor.x, 1f - anchor.y) : new Vector2(1f - anchor.x, anchor.y);
		}

		// Overlay-канвас цели: мировые координаты = экранные (камера null); иначе — камера канваса
		private static Camera ResolveCamera(RectTransform target) {
			var targetCanvas = target.GetComponentInParent<Canvas>();
			if (targetCanvas == null)
				return Camera.main;
			var root = targetCanvas.rootCanvas;
			if (root.renderMode == RenderMode.ScreenSpaceOverlay)
				return null;
			return root.worldCamera != null ? root.worldCamera : Camera.main;
		}
	}
}
