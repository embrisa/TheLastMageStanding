namespace TheLastMageStanding.Game.Core.Events;

/// <summary>
/// Published when a new gameplay run begins.
/// </summary>
public readonly record struct RunStartedEvent;

/// <summary>
/// Published when a gameplay run ends (death, quit, etc).
/// </summary>
public readonly record struct RunEndedEvent;

/// <summary>
/// Published when gold is collected during a run.
/// </summary>
public readonly record struct GoldCollectedEvent
{
    public int Amount { get; init; }

    public GoldCollectedEvent(int amount)
    {
        Amount = amount;
    }
}

/// <summary>
/// Published when the player gains meta XP.
/// </summary>
public readonly record struct MetaXpGainedEvent
{
    public int Amount { get; init; }
    public int TotalXp { get; init; }
    public int NewLevel { get; init; }

    public MetaXpGainedEvent(int amount, int totalXp, int newLevel)
    {
        Amount = amount;
        TotalXp = totalXp;
        NewLevel = newLevel;
    }
}

/// <summary>
/// Published when the player gains a meta level.
/// </summary>
public readonly record struct MetaLevelUpEvent
{
    public int NewLevel { get; init; }

    public MetaLevelUpEvent(int newLevel)
    {
        NewLevel = newLevel;
    }
}

/// <summary>
/// Published when equipment is added to the player's inventory.
/// </summary>
public readonly record struct EquipmentCollectedEvent
{
    public string EquipmentId { get; init; }

    public EquipmentCollectedEvent(string equipmentId)
    {
        EquipmentId = equipmentId;
    }
}
