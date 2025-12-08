namespace TheLastMageStanding.Game.Core.MetaProgression;

/// <summary>
/// Persistent player profile containing meta progression, equipment, and run statistics.
/// This profile persists across runs and game sessions.
/// </summary>
public sealed class PlayerProfile
{
    /// <summary>
    /// Schema version for migration support.
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// Player's meta level (persistent across runs).
    /// </summary>
    public int MetaLevel { get; set; } = 1;

    /// <summary>
    /// Total meta XP accumulated.
    /// </summary>
    public int TotalMetaXp { get; set; }

    /// <summary>
    /// Total gold available for spending in meta hub.
    /// </summary>
    public int TotalGold { get; set; }

    /// <summary>
    /// All equipment items owned by the player.
    /// </summary>
    public List<EquipmentItem> EquipmentInventory { get; set; } = new();

    /// <summary>
    /// Currently equipped weapon (ID reference to inventory).
    /// </summary>
    public string? EquippedWeaponId { get; set; }

    /// <summary>
    /// Currently equipped armor (ID reference to inventory).
    /// </summary>
    public string? EquippedArmorId { get; set; }

    /// <summary>
    /// Currently equipped accessories (ID references to inventory).
    /// </summary>
    public List<string> EquippedAccessoryIds { get; set; } = new();

    /// <summary>
    /// Unlocked talent node IDs from the talent tree.
    /// </summary>
    public List<string> UnlockedTalentNodes { get; set; } = new();

    /// <summary>
    /// Unlocked skill IDs available for selection.
    /// </summary>
    public List<string> UnlockedSkillIds { get; set; } = new();

    /// <summary>
    /// Total number of runs completed.
    /// </summary>
    public int TotalRuns { get; set; }

    /// <summary>
    /// Best wave reached across all runs.
    /// </summary>
    public int BestWave { get; set; }

    /// <summary>
    /// Total enemies killed across all runs.
    /// </summary>
    public int TotalKills { get; set; }

    /// <summary>
    /// Total damage dealt across all runs.
    /// </summary>
    public float TotalDamageDealt { get; set; }

    /// <summary>
    /// Total playtime across all runs.
    /// </summary>
    public TimeSpan TotalPlaytime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// When this profile was first created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time this profile was used.
    /// </summary>
    public DateTime LastPlayedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a new default player profile.
    /// </summary>
    public static PlayerProfile CreateDefault()
    {
        return new PlayerProfile
        {
            SchemaVersion = 1,
            MetaLevel = 1,
            TotalMetaXp = 0,
            TotalGold = 100, // Starting gold
            EquipmentInventory = new(),
            UnlockedTalentNodes = new(),
            UnlockedSkillIds = new(),
            CreatedAt = DateTime.UtcNow,
            LastPlayedAt = DateTime.UtcNow
        };
    }
}
