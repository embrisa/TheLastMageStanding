using System;
using System.Collections.Generic;
using System.Linq;
using TheLastMageStanding.Game.Core.Diagnostics;

namespace TheLastMageStanding.Game.Core.Events;

public sealed class EventBus : IEventBus
{
    private interface IEventQueue
    {
        int PendingCount { get; }
        Type EventType { get; }
        int Process(EventBus bus);
        void Clear();
    }

    private sealed class EventQueue<T> : IEventQueue where T : struct
    {
        private readonly Queue<T> _queue = new();

        public int PendingCount => _queue.Count;

        public Type EventType => typeof(T);

        public void Enqueue(T eventData)
        {
            _queue.Enqueue(eventData);
        }

        public int Process(EventBus bus)
        {
            // Each pass only processes the events that were already waiting when the pass started.
            // Handlers can publish more events, but those are deferred to a later pass in the same frame.
            var processedCount = 0;
            int count = _queue.Count;
            for (int i = 0; i < count; i++)
            {
                if (_queue.TryDequeue(out var eventData))
                {
                    bus.Dispatch(eventData);
                    processedCount++;
                }
            }

            return processedCount;
        }

        public void Clear() => _queue.Clear();
    }

    private readonly Dictionary<Type, IEventQueue> _queues = new();
    private readonly List<IEventQueue> _activeQueues = new();
    private readonly Dictionary<Type, List<object?>> _subscribers = new();
    private bool _dirtySubscribers;
    private readonly int _maxPasses;
    private const string LogCategory = "EventBus";
    private const int DefaultMaxPasses = 10;

    /// <summary>
    /// Creates a deferred event bus.
    /// Each call to <see cref="ProcessEvents"/> drains queued events in passes until the queues are empty.
    /// Events published by handlers are never re-entered into the same queue pass; they are deferred to a later pass.
    /// If handlers keep producing more work than can be drained within <paramref name="maxPasses"/>, processing fails loudly.
    /// </summary>
    public EventBus(int maxPasses = DefaultMaxPasses)
    {
        if (maxPasses <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPasses), maxPasses, "Event bus max passes must be positive.");
        }

        _maxPasses = maxPasses;
    }

    public void Publish<T>(T eventData) where T : struct
    {
        var type = typeof(T);
        if (!_queues.TryGetValue(type, out var queue))
        {
            queue = new EventQueue<T>();
            _queues[type] = queue;
            _activeQueues.Add(queue);
        }
        ((EventQueue<T>)queue).Enqueue(eventData);
    }

    public EventSubscription Subscribe<T>(Action<T> handler) where T : struct
    {
        var type = typeof(T);
        if (!_subscribers.TryGetValue(type, out var list))
        {
            list = new List<object?>();
            _subscribers[type] = list;
        }
        
        // Check for duplicates
        bool exists = false;
        for(int i=0; i<list.Count; i++) 
        {
            if (list[i] != null && list[i]!.Equals(handler)) 
            {
                exists = true; 
                break;
            }
        }
        
        if (!exists)
        {
            list.Add(handler);
        }

        return new EventSubscription(type, () => Unsubscribe(handler));
    }

    public void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out var list))
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i]!.Equals(handler))
                {
                    list[i] = null;
                    _dirtySubscribers = true;
                    break;
                }
            }
        }
    }

    public void ProcessEvents()
    {
        var passesExecuted = 0;
        var totalDispatched = 0;

        while (passesExecuted < _maxPasses)
        {
            var dispatchedThisPass = 0;
            for (int i = 0; i < _activeQueues.Count; i++)
            {
                dispatchedThisPass += _activeQueues[i].Process(this);
            }

            if (dispatchedThisPass == 0)
            {
                break;
            }

            totalDispatched += dispatchedThisPass;
            passesExecuted++;
        }

        if (HasPendingEvents())
        {
            var pendingEvents = FormatEventTypeDiagnostics(
                GetDiagnosticsSnapshot().EventTypes.Where(entry => entry.PendingCount > 0));

            var message =
                $"Event processing exceeded the configured max of {_maxPasses} passes " +
                $"after dispatching {totalDispatched} events. Pending queues: {pendingEvents}.";
            RuntimeLog.Error(LogCategory, message);
            throw new EventBusOverflowException(message);
        }

        if (passesExecuted > 1)
        {
            RuntimeLog.Debug(LogCategory, $"Processed {totalDispatched} events across {passesExecuted} passes.");
        }

        if (_dirtySubscribers)
        {
            CleanupSubscribers();
        }
    }

    public EventBusDiagnostics GetDiagnosticsSnapshot()
    {
        if (_dirtySubscribers)
        {
            CleanupSubscribers();
        }

        var eventTypes = new Dictionary<Type, EventBusEventTypeDiagnostics>();

        foreach (var queue in _activeQueues)
        {
            eventTypes[queue.EventType] = new EventBusEventTypeDiagnostics(
                queue.EventType,
                queue.PendingCount,
                SubscriberCountFor(queue.EventType));
        }

        foreach (var (eventType, list) in _subscribers)
        {
            if (!eventTypes.TryGetValue(eventType, out var entry))
            {
                eventTypes[eventType] = new EventBusEventTypeDiagnostics(eventType, 0, list.Count);
                continue;
            }

            eventTypes[eventType] = entry with { SubscriberCount = list.Count };
        }

        var orderedEventTypes = eventTypes.Values
            .OrderByDescending(entry => entry.PendingCount)
            .ThenByDescending(entry => entry.SubscriberCount)
            .ThenBy(entry => entry.EventType.Name, StringComparer.Ordinal)
            .ToArray();

        var pendingEventCount = 0;
        var subscriberCount = 0;
        for (var i = 0; i < orderedEventTypes.Length; i++)
        {
            pendingEventCount += orderedEventTypes[i].PendingCount;
            subscriberCount += orderedEventTypes[i].SubscriberCount;
        }

        return new EventBusDiagnostics(_maxPasses, pendingEventCount, subscriberCount, orderedEventTypes);
    }

    internal void Dispatch<T>(T eventData) where T : struct
    {
        if (_subscribers.TryGetValue(typeof(T), out var list))
        {
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                var handlerObj = list[i];
                if (handlerObj != null)
                {
                    ((Action<T>)handlerObj)(eventData);
                }
            }
        }
    }

    private void CleanupSubscribers()
    {
        foreach (var list in _subscribers.Values)
        {
            list.RemoveAll(x => x == null);
        }
        _dirtySubscribers = false;
    }

    private bool HasPendingEvents()
    {
        for (int i = 0; i < _activeQueues.Count; i++)
        {
            if (_activeQueues[i].PendingCount > 0)
            {
                return true;
            }
        }

        return false;
    }

    private int SubscriberCountFor(Type eventType)
    {
        return _subscribers.TryGetValue(eventType, out var list) ? list.Count : 0;
    }

    private static string FormatEventTypeDiagnostics(IEnumerable<EventBusEventTypeDiagnostics> entries)
    {
        return string.Join(
            ", ",
            entries.Select(entry => $"{entry.EventType.Name}(pending={entry.PendingCount}, subscribers={entry.SubscriberCount})"));
    }
}
