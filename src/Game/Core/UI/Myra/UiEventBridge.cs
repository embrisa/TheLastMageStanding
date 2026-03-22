using System;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.UI.Myra;

/// <summary>
/// Small helper to manage EventBus subscriptions for UI screens and clean them up on dispose.
/// </summary>
internal sealed class UiEventBridge : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly EventSubscriptionScope _subscriptions = new();
    private bool _disposed;

    public UiEventBridge(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public EventSubscription Subscribe<T>(Action<T> handler) where T : struct
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _subscriptions.Subscribe(_eventBus, handler);
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
