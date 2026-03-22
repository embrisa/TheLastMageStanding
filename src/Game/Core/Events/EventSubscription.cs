using System;

namespace TheLastMageStanding.Game.Core.Events;

public sealed class EventSubscription : IDisposable
{
    private Action? _disposeAction;

    internal EventSubscription(Type eventType, Action disposeAction)
    {
        EventType = eventType;
        _disposeAction = disposeAction;
    }

    public Type EventType { get; }

    public bool IsDisposed => _disposeAction is null;

    public void Dispose()
    {
        var disposeAction = _disposeAction;
        if (disposeAction is null)
        {
            return;
        }

        _disposeAction = null;
        disposeAction();
    }
}
