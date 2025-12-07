namespace TheLastMageStanding.Game.Core.Events;

internal readonly struct WaveCompletedEvent
{
    public int WaveIndex { get; }

    public WaveCompletedEvent(int waveIndex)
    {
        WaveIndex = waveIndex;
    }
}
