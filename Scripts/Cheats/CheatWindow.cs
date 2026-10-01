#if CHEATS
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Cheats {
	public class CheatWindow : MonoBehaviour {
		// Структура для хранения информации о чите
		[System.Serializable]
		public struct CheatItem {
			public string title;
			public Action action;

			public CheatItem(string title, Action action) {
				this.title = title;
				this.action = action;
			}
		}

		// Ссылки на UI компоненты
		private Canvas canvas;
		private RectTransform windowRect;
		private GameObject windowGameObject;
		private GridLayoutGroup gridLayout;
		private ScrollRect scrollRect;
		private GameObject contentPanel;

		// Список читов
		private List<CheatItem> cheats = new List<CheatItem>();

		// Стили и настройки
		private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
		private Color buttonNormalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
		private Color buttonHoverColor = new Color(0.3f, 0.3f, 0.3f, 1f);
		private Color buttonPressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);

		// События
		public event Action OnWindowShown;
		public event Action OnWindowHidden;

		/// <summary>
		/// Инициализация окна читов
		/// </summary>
		/// <param name="parentCanvas">Родительский Canvas (если null, создаст свой)</param>
		/// <param name="cheatItems">Список читов</param>
		/// <param name="windowTitle">Заголовок окна</param>
		public void Initialize(Canvas parentCanvas = null, List<CheatItem> cheatItems = null,
							  string windowTitle = "Cheat Console") {
			cheats = cheatItems ?? new List<CheatItem>();

			// Создаем или получаем Canvas
			SetupCanvas(parentCanvas);

			// Создаем окно
			CreateWindow(windowTitle);

			// Создаем кнопки читов
			CreateCheatButtons();

			// Настраиваем обработчик перетаскивания
			SetupDragHandler();

			Debug.Log($"CheatWindowUGUI initialized with {cheats.Count} cheats");
		}

		/// <summary>
		/// Настройка Canvas
		/// </summary>
		private void SetupCanvas(Canvas parentCanvas) {
			if (parentCanvas != null) {
				canvas = parentCanvas;
				// Добавляем EventSystem если его нет
				if (FindObjectOfType<EventSystem>() == null) {
					CreateEventSystem();
				}
			} else {
				// Создаем свой Canvas
				GameObject canvasObj = new GameObject("CheatWindowCanvas");
				canvas = canvasObj.AddComponent<Canvas>();
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;
				canvas.sortingOrder = 1000; // Высокий порядок отрисовки

				// Добавляем CanvasScaler
				CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
				scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = new Vector2(1920, 1080);
				scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
				scaler.matchWidthOrHeight = 0.5f;

				// Добавляем GraphicRaycaster
				canvasObj.AddComponent<GraphicRaycaster>();

				// Создаем EventSystem
				CreateEventSystem();
			}
		}

		/// <summary>
		/// Создание EventSystem
		/// </summary>
		private void CreateEventSystem() {
			GameObject eventSystemObj = new GameObject("EventSystem");
			eventSystemObj.AddComponent<EventSystem>();
			eventSystemObj.AddComponent<StandaloneInputModule>();
		}

		/// <summary>
		/// Создание основного окна
		/// </summary>
		private void CreateWindow(string title) {
			// Создаем объект окна
			windowGameObject = new GameObject("CheatWindow");
			windowRect = windowGameObject.AddComponent<RectTransform>();
			windowRect.SetParent(canvas.transform);

			// A nested canvas keeps the console visible when the parent group's alpha is zero.
			Canvas windowCanvas = windowGameObject.AddComponent<Canvas>();
			windowCanvas.overrideSorting = true;
			windowCanvas.sortingOrder = 1000;
			windowGameObject.AddComponent<GraphicRaycaster>();
			CanvasGroup windowGroup = windowGameObject.AddComponent<CanvasGroup>();
			windowGroup.ignoreParentGroups = true;

			// Устанавливаем размер и позицию окна
			windowRect.sizeDelta = new Vector2(600, 700);
			windowRect.anchoredPosition = Vector2.zero;
			windowRect.anchorMin = new Vector2(0.5f, 0.5f);
			windowRect.anchorMax = new Vector2(0.5f, 0.5f);
			windowRect.pivot = new Vector2(0.5f, 0.5f);
			windowRect.localScale = Vector3.one;

			// Добавляем Image для фона
			Image bgImage = windowGameObject.AddComponent<Image>();
			bgImage.color = backgroundColor;
			bgImage.sprite = null;
			bgImage.type = Image.Type.Sliced;

			// Создаем заголовок окна
			CreateHeader(title);

			// Создаем кнопку закрытия
			CreateCloseButton();

			// Создаем панель контента с прокруткой
			CreateScrollableContent();
		}

		/// <summary>
		/// Создание заголовка окна
		/// </summary>
		private void CreateHeader(string title) {
			GameObject headerObj = new GameObject("Header");
			RectTransform headerRect = headerObj.AddComponent<RectTransform>();
			headerRect.SetParent(windowRect);

			// Настраиваем RectTransform
			headerRect.anchorMin = new Vector2(0, 1);
			headerRect.anchorMax = new Vector2(1, 1);
			headerRect.pivot = new Vector2(0.5f, 1);
			headerRect.sizeDelta = new Vector2(-40, 60);
			headerRect.anchoredPosition = new Vector2(0, -10);
			headerRect.localScale = Vector3.one;

			// Добавляем фон заголовка
			Image headerImage = headerObj.AddComponent<Image>();
			headerImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

			// Добавляем текст заголовка
			GameObject textObj = new GameObject("TitleText");
			RectTransform textRect = textObj.AddComponent<RectTransform>();
			textRect.SetParent(headerRect);

			textRect.anchorMin = Vector2.zero;
			textRect.anchorMax = Vector2.one;
			textRect.sizeDelta = Vector2.zero;
			textRect.anchoredPosition = Vector2.zero;

			Text titleText = textObj.AddComponent<Text>();
			titleText.text = title;
			titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			titleText.fontSize = 24;
			titleText.color = Color.white;
			titleText.alignment = TextAnchor.MiddleCenter;
			titleText.fontStyle = FontStyle.Bold;
		}

		/// <summary>
		/// Создание кнопки закрытия
		/// </summary>
		private void CreateCloseButton() {
			GameObject closeButtonObj = new GameObject("CloseButton");
			RectTransform closeRect = closeButtonObj.AddComponent<RectTransform>();
			closeRect.SetParent(windowRect);

			// Настраиваем позицию
			closeRect.anchorMin = new Vector2(1, 1);
			closeRect.anchorMax = new Vector2(1, 1);
			closeRect.pivot = new Vector2(1, 1);
			closeRect.sizeDelta = new Vector2(40, 40);
			closeRect.anchoredPosition = new Vector2(-10, -10);
			closeRect.localScale = Vector3.one;

			// Добавляем Image
			Image closeImage = closeButtonObj.AddComponent<Image>();
			closeImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

			// Добавляем Button
			Button closeButton = closeButtonObj.AddComponent<Button>();
			closeButtonObj.AddComponent<ButtonClickSound>();
			closeButton.onClick.AddListener(Hide);

			// Настраиваем цвета кнопки
			ColorBlock colors = closeButton.colors;
			colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 1f);
			colors.highlightedColor = new Color(0.9f, 0.3f, 0.3f, 1f);
			colors.pressedColor = new Color(1f, 0.4f, 0.4f, 1f);
			colors.selectedColor = new Color(0.8f, 0.2f, 0.2f, 1f);
			closeButton.colors = colors;

			// Добавляем текст "X"
			GameObject textObj = new GameObject("CloseText");
			RectTransform textRect = textObj.AddComponent<RectTransform>();
			textRect.SetParent(closeRect);

			textRect.anchorMin = Vector2.zero;
			textRect.anchorMax = Vector2.one;
			textRect.sizeDelta = Vector2.zero;
			textRect.anchoredPosition = Vector2.zero;

			Text closeText = textObj.AddComponent<Text>();
			closeText.text = "X";
			closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			closeText.fontSize = 20;
			closeText.color = Color.white;
			closeText.alignment = TextAnchor.MiddleCenter;
			closeText.fontStyle = FontStyle.Bold;
		}

		/// <summary>
		/// Создание прокручиваемого контента
		/// </summary>
		private void CreateScrollableContent() {
			// Создаем ScrollRect
			GameObject scrollObj = new GameObject("ScrollView");
			RectTransform scrollRectTransform = scrollObj.AddComponent<RectTransform>();
			scrollRectTransform.SetParent(windowRect);

			// Настраиваем ScrollRect
			scrollRectTransform.anchorMin = new Vector2(0, 0);
			scrollRectTransform.anchorMax = new Vector2(1, 1);
			scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
			scrollRectTransform.sizeDelta = new Vector2(-40, -80);
			scrollRectTransform.anchoredPosition = new Vector2(0, -35);
			scrollRectTransform.localScale = Vector3.one;

			// Добавляем Image для маски
			Image scrollImage = scrollObj.AddComponent<Image>();
			scrollImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

			// Добавляем ScrollRect
			scrollRect = scrollObj.AddComponent<ScrollRect>();
			scrollRect.horizontal = false;
			scrollRect.vertical = true;
			scrollRect.movementType = ScrollRect.MovementType.Clamped;
			scrollRect.decelerationRate = 0.135f;
			scrollRect.scrollSensitivity = 25f;

			// Создаем Viewport
			GameObject viewportObj = new GameObject("Viewport");
			RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
			viewportRect.SetParent(scrollRectTransform);

			viewportRect.anchorMin = Vector2.zero;
			viewportRect.anchorMax = Vector2.one;
			viewportRect.sizeDelta = Vector2.zero;
			viewportRect.pivot = new Vector2(0.5f, 1f);
			viewportRect.anchoredPosition = Vector2.zero;
			viewportRect.localScale = Vector3.one;

			viewportObj.AddComponent<RectMask2D>();

			// Создаем Content panel
			contentPanel = new GameObject("Content");
			RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
			contentRect.SetParent(viewportRect);

			contentRect.anchorMin = new Vector2(0.5f, 1);
			contentRect.anchorMax = new Vector2(0.5f, 1);
			contentRect.pivot = new Vector2(0.5f, 1);
			contentRect.sizeDelta = new Vector2(500, 0);
			contentRect.anchoredPosition = Vector2.zero;
			contentRect.localScale = Vector3.one;

			// Добавляем GridLayoutGroup
			gridLayout = contentPanel.AddComponent<GridLayoutGroup>();
			gridLayout.cellSize = new Vector2(240, 80);
			gridLayout.spacing = new Vector2(10, 10);
			gridLayout.padding = new RectOffset(10, 10, 10, 10);
			gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			gridLayout.constraintCount = 2;
			gridLayout.childAlignment = TextAnchor.UpperCenter;

			// Настраиваем ScrollRect
			scrollRect.content = contentRect;
			scrollRect.viewport = viewportRect;
		}

		/// <summary>
		/// Создание кнопок читов
		/// </summary>
		private void CreateCheatButtons() {
			foreach (var cheat in cheats) {
				CreateCheatButton(cheat);
			}

			// Обновляем размер content panel
			UpdateContentSize();
		}

		/// <summary>
		/// Создание одной кнопки читов
		/// </summary>
		private void CreateCheatButton(CheatItem cheat) {
			// Создаем объект кнопки
			GameObject buttonObj = new GameObject($"CheatButton_{cheat.title}");
			RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
			buttonRect.SetParent(contentPanel.transform);
			buttonRect.localScale = Vector3.one;

			// Добавляем Image
			Image buttonImage = buttonObj.AddComponent<Image>();
			buttonImage.color = buttonNormalColor;

			// Добавляем Button
			Button button = buttonObj.AddComponent<Button>();
			buttonObj.AddComponent<ButtonClickSound>();

			// Настраиваем цвета кнопки
			ColorBlock colors = button.colors;
			colors.normalColor = buttonNormalColor;
			colors.highlightedColor = buttonHoverColor;
			colors.pressedColor = buttonPressedColor;
			colors.selectedColor = buttonHoverColor;
			colors.fadeDuration = 0.1f;
			button.colors = colors;

			// Добавляем обработчик нажатия
			button.onClick.AddListener(() => {
				cheat.action?.Invoke();
				Debug.Log($"Cheat activated: {cheat.title}");

				// Визуальная обратная связь
				StartCoroutine(ButtonPressFeedback(buttonImage));
			});

			// Создаем вертикальную группу для текста
			GameObject textGroup = new GameObject("TextGroup");
			RectTransform textGroupRect = textGroup.AddComponent<RectTransform>();
			textGroupRect.SetParent(buttonRect);

			textGroupRect.anchorMin = Vector2.zero;
			textGroupRect.anchorMax = Vector2.one;
			textGroupRect.sizeDelta = new Vector2(-20, -20);
			textGroupRect.anchoredPosition = Vector2.zero;
			textGroup.transform.localScale = Vector3.one;

			// Добавляем VerticalLayoutGroup
			VerticalLayoutGroup verticalLayout = textGroup.AddComponent<VerticalLayoutGroup>();
			verticalLayout.padding = new RectOffset(5, 5, 5, 5);
			verticalLayout.spacing = 2;
			verticalLayout.childAlignment = TextAnchor.MiddleCenter;
			verticalLayout.childControlHeight = true;
			verticalLayout.childControlWidth = true;
			verticalLayout.childForceExpandHeight = false;
			verticalLayout.childForceExpandWidth = false;

			// Добавляем ContentSizeFitter
			ContentSizeFitter sizeFitter = textGroup.AddComponent<ContentSizeFitter>();
			sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

			// Создаем заголовок
			GameObject titleObj = new GameObject("Title");
			RectTransform titleRect = titleObj.AddComponent<RectTransform>();
			titleRect.SetParent(textGroupRect);
			titleRect.localScale = Vector3.one;

			Text titleText = titleObj.AddComponent<Text>();
			titleText.text = cheat.title;
			titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			titleText.fontSize = 16;
			titleText.color = Color.white;
			titleText.alignment = TextAnchor.MiddleCenter;
			titleText.fontStyle = FontStyle.Bold;

		}

		/// <summary>
		/// Обновление размера контента
		/// </summary>
		private void UpdateContentSize() {
			if (gridLayout == null) return;

			int childCount = contentPanel.transform.childCount;
			if (childCount == 0) return;

			int rows = Mathf.CeilToInt((float)childCount / gridLayout.constraintCount);
			float totalHeight = rows * gridLayout.cellSize.y +
							   (rows - 1) * gridLayout.spacing.y +
							   gridLayout.padding.top + gridLayout.padding.bottom;

			// Ширина — по числу колонок, иначе сетка вылезает за контент и обрезается маской
			float totalWidth = gridLayout.constraintCount * gridLayout.cellSize.x +
							   (gridLayout.constraintCount - 1) * gridLayout.spacing.x +
							   gridLayout.padding.left + gridLayout.padding.right;

			contentPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(totalWidth, totalHeight);
		}

		/// <summary>
		/// Настройка перетаскивания окна
		/// </summary>
		private void SetupDragHandler() {
			// Находим заголовок
			Transform header = windowRect.Find("Header");
			if (header != null) {
				DragHandler dragHandler = header.gameObject.AddComponent<DragHandler>();
				dragHandler.Initialize(windowRect);
			}
		}

		/// <summary>
		/// Визуальная обратная связь при нажатии
		/// </summary>
		private System.Collections.IEnumerator ButtonPressFeedback(Image buttonImage) {
			Color originalColor = buttonImage.color;
			buttonImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);

			yield return new WaitForSeconds(0.1f);

			buttonImage.color = originalColor;
		}

		/// <summary>
		/// Показать окно
		/// </summary>
		public void Show() {
			if (windowGameObject != null) {
				windowGameObject.SetActive(true);
				windowGameObject.transform.SetAsLastSibling(); // Поверх других окон
				OnWindowShown?.Invoke();
			}
		}

		/// <summary>
		/// Скрыть окно
		/// </summary>
		public void Hide() {
			if (windowGameObject != null) {
				windowGameObject.SetActive(false);
				OnWindowHidden?.Invoke();
			}
		}

		/// <summary>
		/// Переключить видимость окна
		/// </summary>
		public void Toggle() {
			if (windowGameObject != null) {
				bool newState = !windowGameObject.activeSelf;
				windowGameObject.SetActive(newState);

				if (newState)
					OnWindowShown?.Invoke();
				else
					OnWindowHidden?.Invoke();
			}
		}

		/// <summary>
		/// Добавить чит динамически
		/// </summary>
		public void AddCheat(string title, Action action, string description = "") {
			CheatItem newCheat = new CheatItem {
				title = title,
				action = action,
			};

			cheats.Add(newCheat);
			CreateCheatButton(newCheat);
			UpdateContentSize();
		}

		/// <summary>
		/// Удалить чит по заголовку
		/// </summary>
		public void RemoveCheat(string title) {
			// Удаляем из списка
			cheats.RemoveAll(c => c.title == title);

			// Удаляем соответствующую кнопку
			Transform buttonToRemove = contentPanel.transform.Find($"CheatButton_{title}");
			if (buttonToRemove != null) {
				Destroy(buttonToRemove.gameObject);
				UpdateContentSize();
			}
		}

		/// <summary>
		/// Очистить все читы
		/// </summary>
		public void ClearCheats() {
			cheats.Clear();

			// Удаляем все кнопки
			foreach (Transform child in contentPanel.transform) {
				Destroy(child.gameObject);
			}

			UpdateContentSize();
		}

		/// <summary>
		/// Установить количество колонок
		/// </summary>
		public void SetGridColumns(int columns) {
			if (gridLayout != null) {
				gridLayout.constraintCount = Mathf.Max(1, columns);
				UpdateContentSize();
			}
		}

		/// <summary>
		/// Установить размер кнопок
		/// </summary>
		public void SetButtonSize(Vector2 size) {
			if (gridLayout != null) {
				gridLayout.cellSize = size;
				UpdateContentSize();
			}
		}

		/// <summary>
		/// Уничтожить окно
		/// </summary>
		public void DestroyWindow() {
			if (canvas != null && canvas.gameObject.name == "CheatWindowCanvas") {
				Destroy(canvas.gameObject);
			} else if (windowGameObject != null) {
				Destroy(windowGameObject);
			}

			Destroy(this);
		}

		void Update() {
			// Горячая клавиша для переключения окна (BackQuote - клавиша `)
			if (Input.GetKeyDown(KeyCode.BackQuote)) {
				Toggle();
			}
		}
	}
}
#endif