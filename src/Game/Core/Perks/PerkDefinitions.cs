namespace TheLastMageStanding.Game.Core.Perks;

/// <summary>
/// Defines a single perk node in the talent tree.
/// </summary>
internal sealed class PerkDefinition
{
    /// <summary>
    /// Unique identifier for this perk (e.g., "fire_power_1").
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Display name for UI (e.g., "Burning Fury").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Short description of what the perk does.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Maximum ranks this perk can have (e.g., 3 means rank 0/1/2/3).
    /// </summary>
    public int MaxRank { get; init; } = 1;

    /// <summary>
    /// Perk points required per rank allocation.
    /// </summary>
    public int PointsPerRank { get; init; } = 1;

    /// <summary>
    /// List of prerequisite perk IDs and their required ranks.
    /// Empty list means no prerequisites.
    /// </summary>
    public List<PerkPrerequisite> Prerequisites { get; init; } = new();

    /// <summary>
    /// Effects applied at each rank.
    /// Key = rank number, Value = effects at that rank.
    /// </summary>
    public Dictionary<int, PerkEffects> EffectsByRank { get; init; } = new();

    /// <summary>
    /// UI positioning hint (e.g., for tree visualization).
    /// </summary>
    public (int Row, int Column) GridPosition { get; init; }
}

/// <summary>
/// Prerequisite requirement for a perk.
/// </summary>
internal sealed class PerkPrerequisite
{
    /// <summary>
    /// ID of the perk that must be allocated.
    /// </summary>
    public string PerkId { get; init; } = string.Empty;

    /// <summary>
    /// Minimum rank required in that perk.
    /// </summary>
    public int MinimumRank { get; init; } = 1;

    public PerkPrerequisite(string perkId, int minimumRank = 1)
    {
        PerkId = perkId;
        MinimumRank = minimumRank;
    }
}

/// <summary>
/// Effects granted by a perk at a specific rank.
/// </summary>
internal sealed class PerkEffects
{
    // Stat modifiers (additive)
    public float PowerAdditive { get; init; }
    public float AttackSpeedAdditive { get; init; }
    public float CritChanceAdditive { get; init; }
    public float CritMultiplierAdditive { get; init; }
    public float CooldownReductionAdditive { get; init; }
    public float ArmorAdditive { get; init; }
    public float ArcaneResistAdditive { get; init; }
    public float MoveSpeedAdditive { get; init; }
    public float HealthAdditive { get; init; }

    // Stat modifiers (multiplicative)
    public float PowerMultiplicative { get; init; } = 1f;
    public float AttackSpeedMultiplicative { get; init; } = 1f;
    public float MoveSpeedMultiplicative { get; init; } = 1f;

    // Gameplay modifiers
    public int ProjectilePierceBonus { get; init; }
    public int ProjectileChainBonus { get; init; }
    public float DashCooldownReduction { get; init; }

    /// <summary>
    /// Create a PerkEffects with no effects (identity).
    /// </summary>
    public static PerkEffects None => new();

    /// <summary>
    /// Combine multiple PerkEffects together (for calculating total effects).
    /// </summary>
    public static PerkEffects Combine(params PerkEffects[] effects)
    {
        var combined = new PerkEffects
        {
            PowerAdditive = effects.Sum(e => e.PowerAdditive),
            AttackSpeedAdditive = effects.Sum(e => e.AttackSpeedAdditive),
            CritChanceAdditive = effects.Sum(e => e.CritChanceAdditive),
            CritMultiplierAdditive = effects.Sum(e => e.CritMultiplierAdditive),
            CooldownReductionAdditive = effects.Sum(e => e.CooldownReductionAdditive),
            ArmorAdditive = effects.Sum(e => e.ArmorAdditive),
            ArcaneResistAdditive = effects.Sum(e => e.ArcaneResistAdditive),
            MoveSpeedAdditive = effects.Sum(e => e.MoveSpeedAdditive),
            HealthAdditive = effects.Sum(e => e.HealthAdditive),

            // Multiplicative effects multiply together
            PowerMultiplicative = effects.Aggregate(1f, (acc, e) => acc * e.PowerMultiplicative),
            AttackSpeedMultiplicative = effects.Aggregate(1f, (acc, e) => acc * e.AttackSpeedMultiplicative),
            MoveSpeedMultiplicative = effects.Aggregate(1f, (acc, e) => acc * e.MoveSpeedMultiplicative),

            // Gameplay modifiers sum
            ProjectilePierceBonus = effects.Sum(e => e.ProjectilePierceBonus),
            ProjectileChainBonus = effects.Sum(e => e.ProjectileChainBonus),
            DashCooldownReduction = effects.Sum(e => e.DashCooldownReduction)
        };

        return combined;
    }
}
