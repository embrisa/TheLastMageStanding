using System;
using System.Collections.Generic;

namespace TheLastMageStanding.Game.Core.Events;

public sealed class EventBus : IEventBus
{
    private interface IEventQueue
    {
        bool Process(EventBus bus);
        void Clear();
    }

    private sealed class EventQueue<T> : IEventQueue where T : struct
    {
        private readonly Queue<T> _queue = new();

        public void Enqueue(T eventData)
        {
            _queue.Enqueue(eventData);
        }

        public bool Process(EventBus bus)
        {
            bool processedAny = false;
            // Process currently queued items. 
            // We capture count to avoid infinite loops within a single type's processing if it republishes itself.
            int count = _queue.Count;
            for (int i = 0; i < count; i++)
            {
                if (_queue.TryDequeue(out var eventData))
                {
                    bus.Dispatch(eventData);
                    processedAny = true;
                }
            }
            return processedAny;
        }

        public void Clear() => _queue.Clear();
    }

    private readonly Dictionary<Type, IEventQueue> _queues = new();
    private readonly List<IEventQueue> _activeQueues = new();
    private readonly Dictionary<Type, List<object?>> _subscribers = new();
    private bool _dirtySubscribers;
    private const int MaxPasses = 10;

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

    public void Subscribe<T>(Action<T> handler) where T : struct
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
        int passes = 0;
        bool processedAny;
        do
        {
            processedAny = false;
            for (int i = 0; i < _activeQueues.Count; i++)
            {
                if (_activeQueues[i].Process(this))
                {
                    processedAny = true;
                }
            }
            passes++;
        } while (processedAny && passes < MaxPasses);

        if (_dirtySubscribers)
        {
            CleanupSubscribers();
        }
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
}
