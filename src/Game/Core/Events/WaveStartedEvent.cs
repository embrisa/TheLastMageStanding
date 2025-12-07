namespace TheLastMageStanding.Game.Core.Events;

internal readonly struct WaveStartedEvent
{
    public int WaveIndex { get; }

    public WaveStartedEvent(int waveIndex)
    {
        WaveIndex = waveIndex;
    }
}
