using System;
using System.Collections.Generic;

namespace TheLastMageStanding.Game.Core.Events;

/// <summary>
/// Tracks subscriptions made by a runtime scope so they can be removed together on disposal.
/// Publishes still flow through the shared underlying event bus.
/// </summary>
internal sealed class ScopedEventBus : IEventBus, IDisposable
{
    private interface ITrackedSubscription
    {
        bool Matches(Delegate handler);
        void Unsubscribe(EventBus eventBus);
    }

    private sealed class TrackedSubscription<T> : ITrackedSubscription where T : struct
    {
        private readonly Action<T> _handler;

        public TrackedSubscription(Action<T> handler)
        {
            _handler = handler;
        }

        public bool Matches(Delegate handler) => _handler.Equals(handler);

        public void Unsubscribe(EventBus eventBus) => eventBus.Unsubscribe(_handler);
    }

    private readonly EventBus _eventBus;
    private readonly List<ITrackedSubscription> _subscriptions = [];
    private bool _disposed;

    public ScopedEventBus(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Publish<T>(T eventData) where T : struct
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _eventBus.Publish(eventData);
    }

    public void Subscribe<T>(Action<T> handler) where T : struct
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _eventBus.Subscribe(handler);
        _subscriptions.Add(new TrackedSubscription<T>(handler));
    }

    public void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        if (_disposed)
        {
            return;
        }

        _eventBus.Unsubscribe(handler);
        RemoveTrackedSubscription(handler);
    }

    public void ProcessEvents()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _eventBus.ProcessEvents();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        for (var i = _subscriptions.Count - 1; i >= 0; i--)
        {
            _subscriptions[i].Unsubscribe(_eventBus);
        }

        _subscriptions.Clear();
        _disposed = true;
    }

    private void RemoveTrackedSubscription(Delegate handler)
    {
        for (var i = _subscriptions.Count - 1; i >= 0; i--)
        {
            if (_subscriptions[i].Matches(handler))
            {
                _subscriptions.RemoveAt(i);
                break;
            }
        }
    }
}
