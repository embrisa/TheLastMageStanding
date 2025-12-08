using TheLastMageStanding.Game.Core.Ecs;

namespace TheLastMageStanding.Game.Core.Events;

/// <summary>
/// Published when the player is granted perk points (typically on level-up).
/// </summary>
internal readonly struct PerkPointsGrantedEvent
{
    public Entity Player { get; }
    public int PointsGranted { get; }
    public int TotalAvailable { get; }

    public PerkPointsGrantedEvent(Entity player, int pointsGranted, int totalAvailable)
    {
        Player = player;
        PointsGranted = pointsGranted;
        TotalAvailable = totalAvailable;
    }
}

/// <summary>
/// Published when a perk is allocated or deallocated.
/// </summary>
internal readonly struct PerkAllocatedEvent
{
    public Entity Player { get; }
    public string PerkId { get; }
    public int NewRank { get; }

    public PerkAllocatedEvent(Entity player, string perkId, int newRank)
    {
        Player = player;
        PerkId = perkId;
        NewRank = newRank;
    }
}

/// <summary>
/// Published when all perks are respecced.
/// </summary>
internal readonly struct PerksRespecedEvent
{
    public Entity Player { get; }

    public PerksRespecedEvent(Entity player)
    {
        Player = player;
    }
}
