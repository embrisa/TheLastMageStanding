using TheLastMageStanding.Game.Core.Perks;

namespace TheLastMageStanding.Game.Core.Config;

/// <summary>
/// Configuration for the perk tree, containing all available perks and metadata.
/// </summary>
internal sealed class PerkTreeConfig
{
    public List<PerkDefinition> Perks { get; init; }
    public int PointsPerLevel { get; init; } = 1;
    public int RespecCost { get; init; } // 0 = free respec

    public PerkTreeConfig(List<PerkDefinition> perks, int pointsPerLevel = 1, int respecCost = 0)
    {
        Perks = perks;
        PointsPerLevel = pointsPerLevel;
        RespecCost = respecCost;
    }

    /// <summary>
    /// Find a perk definition by ID.
    /// </summary>
    public PerkDefinition? GetPerk(string perkId)
    {
        return Perks.FirstOrDefault(p => p.Id == perkId);
    }

    /// <summary>
    /// Default mage perk tree with fire/arcane/frost specializations.
    /// </summary>
    public static PerkTreeConfig Default => new(
        new List<PerkDefinition>
        {
            // Core / Foundation tier (Row 0)
            new PerkDefinition
            {
                Id = "core_power",
                Name = "Arcane Mastery",
                Description = "Increases Power by 0.2 per rank",
                MaxRank = 5,
                PointsPerRank = 1,
                Prerequisites = new(),
                GridPosition = (0, 1),
                EffectsByRank = new Dictionary<int, PerkEffects>
                {
                    [1] = new() { PowerAdditive = 0.2f },
                    [2] = new() { PowerAdditive = 0.4f },
                    [3] = new() { PowerAdditive = 0.6f },
                    [4] = new() { PowerAdditive = 0.8f },
                    [5] = new() { PowerAdditive = 1.0f }
                }
            },
            new PerkDefinition
            {
                Id = "core_speed",
                Name = "Swift Casting",
                Description = "Increases Attack Speed by 0.1 per rank",
                MaxRank = 3,
                PointsPerRank = 1,
                Prerequisites = new(),
                GridPosition = (0, 2),
                EffectsByRank = new Dictionary<int, PerkEffects>
                {
                    [1] = new() { AttackSpeedAdditive = 0.1f },
                    [2] = new() { AttackSpeedAdditive = 0.2f },
                    [3] = new() { AttackSpeedAdditive = 0.3f }
                }
            },
            new PerkDefinition
            {
                Id = "core_health",
                Name = "Vitality",
                Description = "Increases max health by 20 per rank",
                MaxRank = 3,
                PointsPerRank = 1,
                Prerequisites = new(),
                GridPosition = (0, 0),
                EffectsByRank = new Dictionary<int, PerkEffects>
                {
                    [1] = new() { HealthAdditive = 20f },
                    [2] = new() { HealthAdditive = 40f },
                    [3] = new() { HealthAdditive = 60f }
                }
            },

            // Intermediate tier (Row 1)
            new PerkDefinition
            {
                Id = "crit_mastery",
                Name = "Critical Focus",
                Description = "Increases Crit Chance by 5% and Crit Multiplier by 0.1",
                MaxRank = 3,
                PointsPerRank = 1,
                Prerequisites = new()
                {
                    new("core_power", 2)
                },
                GridPosition = (1, 1),
                EffectsByRank = new Dictionary<int, PerkEffects>
                {
                    [1] = new() { CritChanceAdditive = 0.05f, CritMultiplierAdditive = 0.1f },
                    [2] = new() { CritChanceAdditive = 0.10f, CritMultiplierAdditive = 0.2f },
                    [3] = new() { CritChanceAdditive = 0.15f, CritMultiplierAdditive = 0.3f }
                }
            },
            new PerkDefinition
            {
                Id = "armor_mastery",
                Name = "Arcane Armor",
                Description = "Increases Armor and Arcane Resist by 10 per rank",
                MaxRank = 3,
                PointsPerRank = 1,
                Prerequisites = new()
                {
                    new("core_health", 2)
                },
                GridPosition = (1, 0),
                EffectsByRank = new Dictionary<int, PerkEffects>
                {
                    [1] = new() { ArmorAdditive = 10f, ArcaneResistAdditive = 10f },
                    [2] = new() { ArmorAdditive = 20f, ArcaneResistAdditive = 20f },
                    [3] = new() { ArmorAdditive = 30f, ArcaneResistAdditive = 30f }
                }
            },
            new PerkDefinition
            {
                Id = "mobility",
                Name = "Fleet Footed",
                Description = "Increases Move Speed by 10 per rank",
                MaxRank = 3,
                PointsPerRank = 1,
                Prerequisites = new()
                {
                    new("core_speed", 1)
                },
                GridPosition = (1, 2),
                EffectsByRank = new Dictionary<int, PerkEffects>
                {
                    [1] = new() { MoveSpeedAdditive = 10f },
                    [2] = new() { MoveSpeedAdditive = 20f },
                    [3] = new() { MoveSpeedAdditive = 30f }
                }
            },

            // Advanced tier (Row 2) - Gameplay modifiers
            new PerkDefinition
            {
                Id = "projectile_pierce",
                Name = "Piercing Projectiles",
                Description = "Projectiles pierce through +1 enemy per rank",
                MaxRank = 2,
                PointsPerRank = 2,
                Prerequisites = new()
                {
                    new("crit_mastery", 2),
                    new("core_speed", 2)
                },
                GridPosition = (2, 1),
                EffectsByRank = new Dictionary<int, PerkEffects>
                {
                    [1] = new() { ProjectilePierceBonus = 1 },
                    [2] = new() { ProjectilePierceBonus = 2 }
                }
            },
            new PerkDefinition
            {
                Id = "cooldown_mastery",
                Name = "Temporal Flux",
                Description = "Reduces cooldowns by 10% per rank",
                MaxRank = 2,
                PointsPerRank = 2,
                Prerequisites = new()
                {
                    new("core_speed", 3)
                },
                GridPosition = (2, 2),
                EffectsByRank = new Dictionary<int, PerkEffects>
                {
                    [1] = new() { CooldownReductionAdditive = 0.1f },
                    [2] = new() { CooldownReductionAdditive = 0.2f }
                }
            },

            // Capstone tier (Row 3)
            new PerkDefinition
            {
                Id = "ultimate_power",
                Name = "Archmage's Might",
                Description = "Increases Power by 50%",
                MaxRank = 1,
                PointsPerRank = 3,
                Prerequisites = new()
                {
                    new("crit_mastery", 3),
                    new("projectile_pierce", 1)
                },
                GridPosition = (3, 1),
                EffectsByRank = new Dictionary<int, PerkEffects>
                {
                    [1] = new() { PowerMultiplicative = 1.5f }
                }
            }
        },
        pointsPerLevel: 1,
        respecCost: 0  // Free respec for now
    );
}
