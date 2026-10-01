using System.Collections.Generic;
using System;

public static class InGameEventManager {

	static Dictionary<InGameEvents, Action<object>> events = new Dictionary<InGameEvents, Action<object>>();

	public static void Subscribe(InGameEvents targetEvent, Action<object> handler) {
		if (events.ContainsKey(targetEvent)) {
			events[targetEvent] += handler;
		} else {
			events.Add(targetEvent, handler);
		}
	}

	public static void Unsubscribe(InGameEvents targetEvent, Action<object> handler) {
		if (events.ContainsKey(targetEvent)) {
			events[targetEvent] -= handler;
		} else {
			events.Remove(targetEvent);
		}
	}

	public static void SendEvent(InGameEvents targetEvent, object param) {
		if (events.ContainsKey(targetEvent)) {
			var handler = events[targetEvent];
			if (handler != null) {
				handler(param);
			}
		}
	}

	public static void SendEvent(InGameEvents targetEvent) {
		if (events.ContainsKey(targetEvent)) {
			var handler = events[targetEvent];
			if (handler != null) {
				handler(null);
			}
		}
	}
}
