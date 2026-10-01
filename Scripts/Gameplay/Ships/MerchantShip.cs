using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MerchantShip : BaseShip, IPointerClickHandler
{
	[SerializeField] private Collider clickCollider;
	
	public event Action<MerchantShip> OnMerchantArrived;
	public event Action<MerchantShip> OnMerchantDeparted;
	public event Action<MerchantShip> OnMerchantClicked;

	private float waitDuration;
	private float waitTimer;
	private bool waiting;

	/// <summary>
	/// Сгенерированные предложения торговца. Создаются один раз при первом открытии окна.
	/// </summary>
	public List<MerchantOffer> Offers { get; private set; }

	public override void Setup(Transform targetPoint, int enterPath, int exitPath) {
		base.Setup(targetPoint, enterPath, exitPath);
		clickCollider.enabled = false;
		waiting = false;
		Offers = null;
	}

	/// <summary>
	/// Установить время ожидания торговца
	/// </summary>
	public void SetWaitDuration(float duration) {
		waitDuration = duration;
	}

	/// <summary>
	/// Установить предложения (вызывается один раз при первой генерации)
	/// </summary>
	public void SetOffers(List<MerchantOffer> offers) {
		Offers = offers;
	}

	/// <summary>
	/// Были ли уже сгенерированы предложения
	/// </summary>
	public bool HasOffers => Offers != null && Offers.Count > 0;

	protected override void OnShipArrived() {
		base.OnShipArrived();
		clickCollider.enabled = true;
		waiting = true;
		waitTimer = 0f;
		OnMerchantArrived?.Invoke(this);
	}

	protected override void OnShipDeparted() {
		base.OnShipDeparted();
		clickCollider.enabled = false;
		waiting = false;
		Offers = null;
		OnMerchantDeparted?.Invoke(this);
	}

	protected override void Update() {
		base.Update();

		if (waiting) {
			waitTimer += Time.deltaTime;
			if (waitTimer >= waitDuration) {
				waiting = false;
				FinishTrading();
			}
		}
	}

	/// <summary>
	/// Обработка клика через EventSystem (требует PhysicsRaycaster на камере)
	/// </summary>
	public void OnPointerClick(PointerEventData eventData) {
		HandleClick();
	}

	/// <summary>
	/// Альтернативная обработка клика через OnMouseDown (работает без EventSystem)
	/// </summary>
	private void OnMouseDown() {
		HandleClick();
	}

	private void HandleClick() {
		if (IsArrived) {
			OnMerchantClicked?.Invoke(this);
		}
	}

	/// <summary>
	/// Приостановить таймер ожидания (пока открыто окно торговли)
	/// </summary>
	public void PauseWaiting() {
		waiting = false;
	}

	/// <summary>
	/// Возобновить таймер ожидания (окно закрыто без покупки)
	/// </summary>
	public void ResumeWaiting() {
		if (IsArrived) {
			waiting = true;
		}
	}

	/// <summary>
	/// Оставшееся время ожидания
	/// </summary>
	public float TimeRemaining => Mathf.Max(0f, waitDuration - waitTimer);

	/// <summary>
	/// Вызывается после успешной покупки — корабль сразу уплывает
	/// </summary>
	public void FinishTrading() {
		waiting = false;
		clickCollider.enabled = false;
		Depart();
	}
}