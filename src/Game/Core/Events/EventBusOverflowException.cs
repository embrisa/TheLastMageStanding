using System;

namespace TheLastMageStanding.Game.Core.Events;

public sealed class EventBusOverflowException : InvalidOperationException
{
    public EventBusOverflowException(string message) : base(message)
    {
    }
}
