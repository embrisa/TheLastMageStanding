using System;
using System.Collections.Generic;

namespace TheLastMageStanding.Game.Core.Events;

internal sealed class EventSubscriptionScope : IDisposable
{
    private readonly List<EventSubscription> _subscriptions = [];
    private bool _disposed;

    public EventSubscription Subscribe<T>(IEventBus eventBus, Action<T> handler) where T : struct
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var subscription = eventBus.Subscribe(handler);
        _subscriptions.Add(subscription);
        return subscription;
    }

    public EventSubscription Track(EventSubscription subscription)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _subscriptions.Add(subscription);
        return subscription;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        for (var i = _subscriptions.Count - 1; i >= 0; i--)
        {
            _subscriptions[i].Dispose();
        }

        _subscriptions.Clear();
        _disposed = true;
    }
}
