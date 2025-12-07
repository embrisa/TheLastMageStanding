namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Marks an entity as an XP orb that can be collected by the player.
/// </summary>
internal struct XpOrb
{
    public XpOrb(int xpValue) => XpValue = xpValue;
    public int XpValue { get; set; }
}

/// <summary>
/// Tracks player experience and level progression.
/// </summary>
internal struct PlayerXp
{
    public PlayerXp(int currentXp, int level, int xpToNextLevel)
    {
        CurrentXp = currentXp;
        Level = level;
        XpToNextLevel = xpToNextLevel;
    }

    public int CurrentXp { get; set; }
    public int Level { get; set; }
    public int XpToNextLevel { get; set; }
}
