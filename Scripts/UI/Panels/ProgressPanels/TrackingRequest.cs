using System;
using UnityEngine;

/// <summary>
/// Тип панели трекинга — определяет какой префаб использовать
/// </summary>
public enum TrackingPanelType
{
	Quest,
	NextWave,
	Merchant,
	Tutorial
}

/// <summary>
/// Запрос на создание панели трекинга
/// </summary>
public class TrackingRequest
{
	public object Key { get; private set; }
	public TrackingPanelType PanelType { get; private set; }
	public Transform Target { get; private set; }
	public Action OnClick { get; private set; }

	public TrackingRequest(object key, TrackingPanelType panelType, Transform target, Action onClick = null) {
		Key = key;
		PanelType = panelType;
		Target = target;
		OnClick = onClick;
	}
}

/// <summary>
/// Запрос на создание панели трекинга с таймером
/// </summary>
public class TrackingTimerRequest : TrackingRequest
{
	public float Duration { get; private set; }
	public Action OnTimerExpired { get; private set; }

	public TrackingTimerRequest(object key, TrackingPanelType panelType, Transform target, float duration, Action onClick = null, Action onTimerExpired = null)
		: base(key, panelType, target, onClick) {
		Duration = duration;
		OnTimerExpired = onTimerExpired;
	}
}

/// <summary>
/// Запрос на удаление панели трекинга
/// </summary>
public class UntrackingRequest
{
	public object Key { get; private set; }

	public UntrackingRequest(object key) {
		Key = key;
	}
}