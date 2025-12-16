namespace TheLastMageStanding.Game.Core.MetaProgression;

/// <summary>
/// Serializable representation of the player's equipped skill loadout.
/// Stored inside <see cref="PlayerProfile"/> and persisted to JSON.
/// </summary>
public sealed class EquippedSkillsProfile
{
    public int Version { get; set; } = 1;

    public string Primary { get; set; } = "Firebolt";

    public string? Hotkey1 { get; set; }
    public string? Hotkey2 { get; set; }
    public string? Hotkey3 { get; set; }
    public string? Hotkey4 { get; set; }

    public static EquippedSkillsProfile CreateDefault() => new()
    {
        Version = 1,
        Primary = "Firebolt",
        Hotkey1 = null,
        Hotkey2 = null,
        Hotkey3 = null,
        Hotkey4 = null
    };
}

