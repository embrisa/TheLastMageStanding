using TheLastMageStanding.Game.Core.Ecs;

namespace TheLastMageStanding.Game.Core.Events;

/// <summary>
/// Published when the player collects an XP orb.
/// </summary>
internal readonly struct XpCollectedEvent
{
    public Entity Player { get; }
    public int XpAmount { get; }

    public XpCollectedEvent(Entity player, int xpAmount)
    {
        Player = player;
        XpAmount = xpAmount;
    }
}

/// <summary>
/// Published when the player gains a level.
/// </summary>
internal readonly struct PlayerLeveledUpEvent
{
    public Entity Player { get; }
    public int NewLevel { get; }

    public PlayerLeveledUpEvent(Entity player, int newLevel)
    {
        Player = player;
        NewLevel = newLevel;
    }
}
