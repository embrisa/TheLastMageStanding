using System;

namespace TheLastMageStanding.Game.Core.Events;

public interface IEventBus
{
    /// <summary>
    /// Publishes an event to the bus. The event will be queued and processed
    /// when ProcessEvents is called (usually at the end of the frame).
    /// </summary>
    void Publish<T>(T eventData) where T : struct;

    /// <summary>
    /// Subscribes to an event type.
    /// </summary>
    void Subscribe<T>(Action<T> handler) where T : struct;

    /// <summary>
    /// Unsubscribes from an event type.
    /// </summary>
    void Unsubscribe<T>(Action<T> handler) where T : struct;
}
