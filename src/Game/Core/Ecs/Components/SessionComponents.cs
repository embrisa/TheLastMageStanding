namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Game session state: Playing allows normal gameplay, GameOver halts spawning/combat/input.
/// </summary>
internal enum GameState
{
    Playing,
    GameOver
}

/// <summary>
/// Singleton component tracking the current game session state, wave progression, and wave timer.
/// </summary>
internal struct GameSession
{
    public GameState State { get; set; }
    public int CurrentWave { get; set; }
    public float WaveTimer { get; set; }
    public float WaveInterval { get; set; }
    public int EnemiesKilled { get; set; }
    public float TimeSurvived { get; set; }

    public GameSession(float waveInterval = 5.0f)
    {
        State = GameState.Playing;
        CurrentWave = 0;
        WaveTimer = 0f;
        WaveInterval = waveInterval;
        EnemiesKilled = 0;
        TimeSurvived = 0f;
    }
}

/// <summary>
/// Transient notification message displayed on the HUD with an auto-dismiss timer.
/// </summary>
internal struct WaveNotification
{
    public string Message { get; set; }
    public float RemainingSeconds { get; set; }

    public WaveNotification(string message, float duration = 2.5f)
    {
        Message = message;
        RemainingSeconds = duration;
    }
}
