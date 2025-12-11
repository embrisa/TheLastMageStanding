using System;
using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.UI.Myra;

/// <summary>
/// Small helper to manage EventBus subscriptions for UI screens and clean them up on dispose.
/// </summary>
internal sealed class UiEventBridge : IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly List<(Type Type, Delegate Handler)> _subscriptions = new();
    private bool _disposed;

    public UiEventBridge(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void Subscribe<T>(Action<T> handler) where T : struct
    {
        if (_disposed)
        {
            return;
        }

        _eventBus.Subscribe(handler);
        _subscriptions.Add((typeof(T), handler));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var (type, handler) in _subscriptions)
        {
            var unsubscribe = typeof(IEventBus).GetMethod(nameof(IEventBus.Unsubscribe));
            if (unsubscribe == null)
            {
                continue;
            }

            var typed = unsubscribe.MakeGenericMethod(type);
            typed.Invoke(_eventBus, new object?[] { handler });
        }

        _subscriptions.Clear();
        _disposed = true;
    }
}

