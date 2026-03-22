using System;
namespace TheLastMageStanding.Game.Core.Events;

/// <summary>
/// Tracks subscriptions made by a runtime scope so they can be removed together on disposal.
/// Publishes still flow through the shared underlying event bus.
/// </summary>
internal sealed class ScopedEventBus : IEventBus, IDisposable
{
    private readonly EventBus _eventBus;
    private readonly EventSubscriptionScope _subscriptions = new();
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

    public EventSubscription Subscribe<T>(Action<T> handler) where T : struct
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _subscriptions.Track(_eventBus.Subscribe(handler));
    }

    public void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        if (_disposed)
        {
            return;
        }

        _eventBus.Unsubscribe(handler);
    }

    public void ProcessEvents()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _eventBus.ProcessEvents();
    }

    public EventBusDiagnostics GetDiagnosticsSnapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _eventBus.GetDiagnosticsSnapshot();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _subscriptions.Dispose();
        _disposed = true;
    }
}
