namespace TheLastMageStanding.Game.Core.Ecs.Config;

/// <summary>
/// Configuration for player progression (XP curve, level-up bonuses).
/// </summary>
internal sealed class ProgressionConfig
{
    public int BaseXpRequirement { get; }
    public float XpGrowthFactor { get; }
    public int XpPerEnemy { get; }
    public float DamageBonusPerLevel { get; }
    public float MoveSpeedBonusPerLevel { get; }
    public float HealthBonusPerLevel { get; }
    public float OrbLifetimeSeconds { get; }
    public float OrbCollectionRadius { get; }
    public float OrbMagnetRadius { get; }
    public float OrbMagnetStrength { get; }

    public ProgressionConfig(
        int baseXpRequirement = 10,
        float xpGrowthFactor = 1.5f,
        int xpPerEnemy = 1,
        float damageBonusPerLevel = 2f,
        float moveSpeedBonusPerLevel = 5f,
        float healthBonusPerLevel = 10f,
        float orbLifetimeSeconds = 10f,
        float orbCollectionRadius = 40f,
        float orbMagnetRadius = 120f,
        float orbMagnetStrength = 3f)
    {
        BaseXpRequirement = baseXpRequirement;
        XpGrowthFactor = xpGrowthFactor;
        XpPerEnemy = xpPerEnemy;
        DamageBonusPerLevel = damageBonusPerLevel;
        MoveSpeedBonusPerLevel = moveSpeedBonusPerLevel;
        HealthBonusPerLevel = healthBonusPerLevel;
        OrbLifetimeSeconds = orbLifetimeSeconds;
        OrbCollectionRadius = orbCollectionRadius;
        OrbMagnetRadius = orbMagnetRadius;
        OrbMagnetStrength = orbMagnetStrength;
    }

    public static ProgressionConfig Default => new();

    /// <summary>
    /// Calculate XP required for a given level.
    /// Formula: BaseXP * (GrowthFactor ^ (Level - 1))
    /// </summary>
    public int CalculateXpForLevel(int level)
    {
        if (level <= 1)
            return BaseXpRequirement;

        return (int)(BaseXpRequirement * Math.Pow(XpGrowthFactor, level - 1));
    }
}
