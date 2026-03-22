namespace TheLastMageStanding.Game.Core.Events;

internal enum SessionStateCommand
{
    Pause,
    Resume,
    Restart
}

internal readonly record struct SessionStateRequestEvent
{
    public SessionStateCommand Command { get; init; }
}

/// <summary>
/// Emitted when a run is restarted to allow systems to reset internal state.
/// </summary>
public readonly record struct SessionRestartedEvent;






