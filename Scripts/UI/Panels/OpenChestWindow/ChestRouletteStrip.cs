using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Горизонтальная лента рулетки сундука: за RectMask2D едет Content с рядом ячеек,
// победитель заранее стоит в ячейке winnerIndex и по кривой торможения останавливается
// в центре под указателем. Награда роллится до анимации — лента чисто косметика.
// Окно сундука ставит timeScale = 0, поэтому все твины на unscaled-времени.
public class ChestRouletteStrip : MonoBehaviour {
	[SerializeField] RectTransform content;
	[SerializeField] ChestRouletteCell cellTemplate;
	[Tooltip("Рамка-указатель над центром ленты; пульсирует при остановке")]
	[SerializeField] Image pointer;

	[Header("Лента")]
	[Tooltip("Расстояние между центрами соседних ячеек")]
	[SerializeField] float cellStep = 150f;
	[SerializeField] int cellCount = 36;
	[Tooltip("Индекс ячейки-победителя = сколько ячеек проедет лента до остановки")]
	[SerializeField] int winnerIndex = 30;

	[Header("Движение")]
	[SerializeField] float spinDuration = 4.5f;
	[Tooltip("Профиль движения: время 0..1 → пройденный путь 0..1. Быстрый разгон, долгое плавное торможение")]
	[SerializeField] AnimationCurve spinCurve = new AnimationCurve(
		new Keyframe(0f, 0f, 0f, 0f),
		new Keyframe(0.18f, 0.35f, 2.2f, 2.2f),
		new Keyframe(1f, 1f, 0f, 0f));
	[Tooltip("Насколько увеличивается ячейка, проходящая через центр")]
	[SerializeField] float centerScale = 1.15f;

	[Header("Остановка")]
	[SerializeField] float dimmedAlpha = 0.45f;
	[SerializeField] float revealDuration = 0.35f;
	[SerializeField] float winnerPunchScale = 0.25f;

	private readonly List<ChestRouletteCell> cells = new List<ChestRouletteCell>();
	private Tween spinTween;
	private Sequence revealSequence;

	public ChestRouletteCell WinnerCell { get; private set; }
	public bool IsSpinning => spinTween != null && spinTween.IsActive();

	// randomEntry — генератор случайных записей для «фоновых» ячеек (null => лента не строится),
	// winner — что должно остановиться в центре (null => пустая награда, сразу onFinished).
	public void Spin(Func<ChestRouletteEntry> randomEntry, ChestRouletteEntry winner, Action onFinished) {
		Kill();
		if (winner == null || randomEntry == null) {
			HideCells();
			onFinished?.Invoke();
			return;
		}

		Build(randomEntry, winner);
		content.anchoredPosition = Vector2.zero;
		UpdateCellScales();
		if (pointer != null)
			pointer.transform.localScale = Vector3.one;

		float target = -WinnerIndexClamped * cellStep;
		spinTween = content.DOAnchorPosX(target, spinDuration)
			.SetEase(spinCurve)
			.SetUpdate(true)
			.OnUpdate(UpdateCellScales)
			.OnComplete(() => {
				spinTween = null;
				content.anchoredPosition = new Vector2(target, 0f);
				UpdateCellScales();
				Reveal();
				onFinished?.Invoke();
			});
	}

	public void Kill() {
		spinTween?.Kill();
		spinTween = null;
		revealSequence?.Kill();
		revealSequence = null;
		WinnerCell = null;
	}

	private int WinnerIndexClamped => Mathf.Clamp(winnerIndex, 0, Mathf.Max(0, cellCount - 1));

	private void Build(Func<ChestRouletteEntry> randomEntry, ChestRouletteEntry winner) {
		EnsureCells(cellCount);
		int winnerAt = WinnerIndexClamped;
		ChestRouletteEntry previous = null;
		for (int i = 0; i < cellCount; i++) {
			var entry = i == winnerAt ? winner : PickDistinct(randomEntry, previous, winner, i, winnerAt);
			var cell = cells[i];
			cell.gameObject.SetActive(true);
			cell.Set(entry);
			cell.SetAlpha(1f);
			cell.Rect.localScale = Vector3.one;
			cell.Rect.anchoredPosition = new Vector2(i * cellStep, 0f);
			previous = entry;
		}
		for (int i = cellCount; i < cells.Count; i++)
			cells[i].gameObject.SetActive(false);
		WinnerCell = cells[winnerAt];
	}

	// Не даём одному предмету встать в две соседние ячейки (лента выглядит «зависшей»)
	// и не ставим копию победителя вплотную к нему — иначе непонятно, что именно выпало.
	private static ChestRouletteEntry PickDistinct(Func<ChestRouletteEntry> randomEntry, ChestRouletteEntry previous,
		ChestRouletteEntry winner, int index, int winnerAt) {
		ChestRouletteEntry entry = null;
		for (int attempt = 0; attempt < 4; attempt++) {
			entry = randomEntry();
			if (entry == null)
				break;
			bool sameAsPrevious = previous != null && SameSource(entry, previous);
			bool nearWinner = Mathf.Abs(index - winnerAt) == 1 && SameSource(entry, winner);
			if (!sameAsPrevious && !nearWinner)
				break;
		}
		return entry ?? winner;
	}

	private static bool SameSource(ChestRouletteEntry a, ChestRouletteEntry b) {
		return a.Source != null && a.Source == b.Source;
	}

	private void EnsureCells(int count) {
		if (cellTemplate == null)
			return;
		cellTemplate.gameObject.SetActive(false);
		while (cells.Count < count) {
			var cell = Instantiate(cellTemplate, content);
			cell.name = "Cell" + cells.Count;
			cells.Add(cell);
		}
	}

	private void HideCells() {
		foreach (var cell in cells)
			cell.gameObject.SetActive(false);
	}

	// Ячейка, проходящая через центр, чуть крупнее соседей — читается как «текущий кандидат»
	private void UpdateCellScales() {
		float offset = content.anchoredPosition.x;
		foreach (var cell in cells) {
			if (!cell.gameObject.activeSelf)
				continue;
			float distance = Mathf.Abs(cell.Rect.anchoredPosition.x + offset) / cellStep;
			float scale = Mathf.Lerp(centerScale, 1f, Mathf.Clamp01(distance));
			cell.Rect.localScale = new Vector3(scale, scale, 1f);
		}
	}

	// Остановились: соседи гаснут, победитель «подпрыгивает», указатель пульсирует
	private void Reveal() {
		revealSequence?.Kill();
		revealSequence = DOTween.Sequence().SetUpdate(true);
		foreach (var cell in cells) {
			if (!cell.gameObject.activeSelf || cell == WinnerCell)
				continue;
			var c = cell;
			revealSequence.Join(DOTween.To(() => 1f, a => c.SetAlpha(a), dimmedAlpha, revealDuration));
		}
		if (WinnerCell != null) {
			WinnerCell.Rect.SetAsLastSibling();
			revealSequence.Join(WinnerCell.Rect.DOPunchScale(Vector3.one * winnerPunchScale, revealDuration, 6, 0.6f));
		}
		if (pointer != null)
			revealSequence.Join(pointer.transform.DOPunchScale(Vector3.one * 0.12f, revealDuration, 4, 0.5f));
	}

	private void OnDisable() {
		Kill();
	}
}
