using System;
using System.Collections.Generic;

namespace BecomingLegend.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> events = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!events.ContainsKey(type))
                events[type] = new List<Delegate>();
            events[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (events.TryGetValue(type, out var handlers))
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                    events.Remove(type);
            }
        }

        public static void Publish<T>(T eventData) where T : struct
        {
            if (events.TryGetValue(typeof(T), out var handlers))
            {
                foreach (var handler in handlers)
                    (handler as Action<T>)?.Invoke(eventData);
            }
        }

        public static void Clear()
        {
            events.Clear();
        }
    }
}
