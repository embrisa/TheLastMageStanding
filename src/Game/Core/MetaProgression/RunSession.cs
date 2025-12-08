namespace TheLastMageStanding.Game.Core.MetaProgression;

/// <summary>
/// Tracks statistics and progress during a single gameplay run.
/// Used to calculate meta XP and update player profile at run end.
/// </summary>
public sealed class RunSession
{
    /// <summary>
    /// When this run started.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When this run ended (null if still in progress).
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total duration of the run.
    /// </summary>
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;

    /// <summary>
    /// Highest wave number reached.
    /// </summary>
    public int WaveReached { get; set; }

    /// <summary>
    /// Total enemies killed.
    /// </summary>
    public int TotalKills { get; set; }

    /// <summary>
    /// Total damage dealt to enemies.
    /// </summary>
    public float TotalDamageDealt { get; set; }

    /// <summary>
    /// Total damage taken by player.
    /// </summary>
    public float TotalDamageTaken { get; set; }

    /// <summary>
    /// Total gold collected during this run.
    /// </summary>
    public int GoldCollected { get; set; }

    /// <summary>
    /// Meta XP earned from this run (calculated at end).
    /// </summary>
    public int MetaXpEarned { get; set; }

    /// <summary>
    /// Equipment items found during this run.
    /// </summary>
    public List<EquipmentItem> EquipmentFound { get; set; } = new();

    /// <summary>
    /// Skills used during the run (skill IDs).
    /// </summary>
    public List<string> SkillsUsed { get; set; } = new();

    /// <summary>
    /// Cause of death / reason run ended.
    /// </summary>
    public string CauseOfDeath { get; set; } = string.Empty;

    /// <summary>
    /// Player's in-run level at end of run.
    /// </summary>
    public int FinalLevel { get; set; }

    /// <summary>
    /// Unique identifier for this run session.
    /// </summary>
    public string RunId { get; set; } = Guid.NewGuid().ToString();
}
