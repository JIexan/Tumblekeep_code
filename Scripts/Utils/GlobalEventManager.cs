using System.Collections.Generic;
using System;

public static class GlobalEventManager {
	private static Dictionary<GlobalEvents, Action> listeners = new Dictionary<GlobalEvents, Action>();

	static GlobalEventManager()
    {
		foreach (var eventType in Enum.GetValues(typeof(GlobalEvents)))
        { 
            listeners.Add((GlobalEvents)eventType, null);
		}
	}

	public static void Subsribe(GlobalEvents targetEvent, Action handler)
    {
		if (!listeners.ContainsKey(targetEvent)) {
			listeners.Add(targetEvent, handler);
		} else {
			listeners[targetEvent] += handler;
		}
	}

	public static void Unsubsribe(GlobalEvents targetEvent, Action handler)
    {
		listeners[targetEvent] -= handler;
	}

	public static void SendEvent(GlobalEvents targetEvent)
    {
		listeners[targetEvent]?.Invoke();
	}
}