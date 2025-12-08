namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Component tracking player's available and spent perk points.
/// </summary>
internal struct PerkPoints
{
    public int AvailablePoints { get; set; }
    public int TotalPointsEarned { get; set; }

    public PerkPoints(int availablePoints, int totalPointsEarned)
    {
        AvailablePoints = availablePoints;
        TotalPointsEarned = totalPointsEarned;
    }
}

/// <summary>
/// Component storing the player's allocated perks.
/// Key = perk ID, Value = current rank.
/// </summary>
internal struct PlayerPerks
{
    public Dictionary<string, int> AllocatedRanks { get; set; }

    public PlayerPerks()
    {
        AllocatedRanks = new Dictionary<string, int>();
    }

    /// <summary>
    /// Get the current rank for a perk (0 if not allocated).
    /// </summary>
    public int GetRank(string perkId)
    {
        return AllocatedRanks.TryGetValue(perkId, out var rank) ? rank : 0;
    }

    /// <summary>
    /// Set the rank for a perk.
    /// </summary>
    public void SetRank(string perkId, int rank)
    {
        if (rank > 0)
        {
            AllocatedRanks[perkId] = rank;
        }
        else
        {
            AllocatedRanks.Remove(perkId);
        }
    }
}

/// <summary>
/// Component tracking gameplay modifiers from perks.
/// </summary>
internal struct PerkGameplayModifiers
{
    public int ProjectilePierceBonus { get; set; }
    public int ProjectileChainBonus { get; set; }
    public float DashCooldownReduction { get; set; }

    public PerkGameplayModifiers()
    {
        ProjectilePierceBonus = 0;
        ProjectileChainBonus = 0;
        DashCooldownReduction = 0f;
    }
}
