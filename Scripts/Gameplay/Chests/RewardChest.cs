using System;
using System.Collections;
using UnityEngine;

// Сундук в мире: тир + Animator (Idle в цикле, триггер "Open" → состояние "Open" без выхода)
// + кликабельный коллайдер (слой Clickable, GameController.OnClick ищет компонент по коллайдеру,
// поэтому коллайдер и RewardChest живут на одном объекте). Префабы собирает ChestsAssetsBuilder.
public class RewardChest : MonoBehaviour
{
	private const string OpenTrigger = "Open";
	private const string OpenState = "Open";

	[SerializeField] ChestTier tier;
	[SerializeField] Animator animator;
	[SerializeField] BoxCollider boxCollider;
	[Tooltip("Страховка: колбэк открытия сработает не позже этого времени, даже если состояние Open не дождались")]
	[SerializeField] float openTimeout = 5f;

	public ChestTier Tier => tier;
	public Collider Collider => boxCollider;
	public bool IsOpening { get; private set; }
	private readonly GameAudioLoop idleAudio = new GameAudioLoop();
	private bool droppedInWorld;

	public void BeginWorldIdle() => droppedInWorld = true;

	private void Update() => idleAudio.Update(GameAudioManager.Instance?.GetId(GameSound.ChestIdle),
		transform.position, droppedInWorld && !IsOpening);

	private void OnDisable() => idleAudio.Stop();

	// Проигрывает открытие и зовёт onOpened, когда анимация доиграла. Повторный клик блокируется
	// выключением коллайдера; без Animator колбэк вызывается сразу.
	public void Open(Action onOpened) {
		if (IsOpening)
			return;
		IsOpening = true;
		idleAudio.Stop();
		if (boxCollider != null)
			boxCollider.enabled = false;

		if (animator == null) {
			onOpened?.Invoke();
			return;
		}
		animator.SetTrigger(OpenTrigger);
		StartCoroutine(WaitForOpenAnimation(onOpened));
	}

	private IEnumerator WaitForOpenAnimation(Action onOpened) {
		float elapsed = 0f;
		while (elapsed < openTimeout) {
			var info = animator.GetCurrentAnimatorStateInfo(0);
			// Сначала проверяем состояние: у зацикленного Idle normalizedTime тоже уходит за 1.
			if (info.IsName(OpenState) && !animator.IsInTransition(0) && info.normalizedTime >= 1f)
				break;
			// Игровое время: пауза игры ставит на паузу и ожидание, как и сам Animator.
			elapsed += Time.deltaTime;
			yield return null;
		}
		onOpened?.Invoke();
	}
}
