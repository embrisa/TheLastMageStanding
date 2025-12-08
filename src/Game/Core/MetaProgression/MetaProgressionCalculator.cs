namespace TheLastMageStanding.Game.Core.MetaProgression;

/// <summary>
/// Calculates meta XP rewards and level progression.
/// </summary>
public static class MetaProgressionCalculator
{
    // XP Formula Constants
    private const float BaseXpWaveMultiplier = 100f;
    private const float WaveExponent = 1.5f;
    private const float KillXpValue = 5f;
    private const float GoldXpMultiplier = 2f;
    private const float DamageXpDivisor = 1000f;
    private const float TimeMultiplierMax = 0.5f;
    private const float TimeMultiplierThreshold = 60f; // minutes

    // Level Formula Constants
    private const float BaseLevelXp = 1000f;
    private const float LevelExponent = 1.8f;

    /// <summary>
    /// Calculates meta XP earned from a completed run.
    /// </summary>
    /// <remarks>
    /// Formula:
    /// base_xp = wave_reached^1.5 * 100
    /// kill_bonus = total_kills * 5
    /// gold_bonus = gold_collected * 2
    /// damage_bonus = damage_dealt / 1000
    /// time_multiplier = max(0, 1 - (run_duration_minutes / 60))
    /// meta_xp = (base_xp + kill_bonus + gold_bonus + damage_bonus) * (1 + time_multiplier * 0.5)
    /// </remarks>
    public static int CalculateMetaXP(RunSession run)
    {
        // Base XP from wave progression (exponential)
        var baseXp = Math.Pow(run.WaveReached, WaveExponent) * BaseXpWaveMultiplier;

        // Bonus XP from kills
        var killBonus = run.TotalKills * KillXpValue;

        // Bonus XP from gold collection
        var goldBonus = run.GoldCollected * GoldXpMultiplier;

        // Bonus XP from damage dealt
        var damageBonus = run.TotalDamageDealt / DamageXpDivisor;

        // Time multiplier (rewards efficient runs, no penalty for long runs)
        var durationMinutes = run.Duration.TotalMinutes;
        var timeMultiplier = Math.Max(0, 1 - (durationMinutes / TimeMultiplierThreshold));

        // Calculate total with time multiplier bonus
        var totalXp = (baseXp + killBonus + goldBonus + damageBonus) * (1 + timeMultiplier * TimeMultiplierMax);

        // Ensure at least 1 XP is awarded
        return Math.Max(1, (int)Math.Floor(totalXp));
    }

    /// <summary>
    /// Gets the meta level for a given total XP amount.
    /// </summary>
    /// <remarks>
    /// Formula: xp_for_level_n = 1000 * (n^1.8)
    /// </remarks>
    public static int GetLevelFromXP(int totalXp)
    {
        if (totalXp <= 0)
            return 1;

        // Binary search for level
        int level = 1;
        while (GetXPForLevel(level + 1) <= totalXp)
        {
            level++;
        }

        return level;
    }

    /// <summary>
    /// Gets the total XP required to reach a specific level.
    /// </summary>
    /// <remarks>
    /// Formula: xp_for_level_n = 1000 * (n^1.8)
    /// </remarks>
    public static int GetXPForLevel(int level)
    {
        if (level <= 1)
            return 0;

        return (int)Math.Floor(BaseLevelXp * Math.Pow(level, LevelExponent));
    }

    /// <summary>
    /// Gets the XP required to reach the next level from current XP.
    /// </summary>
    public static int GetXPToNextLevel(int currentXp)
    {
        var currentLevel = GetLevelFromXP(currentXp);
        var nextLevelXp = GetXPForLevel(currentLevel + 1);
        return Math.Max(0, nextLevelXp - currentXp);
    }

    /// <summary>
    /// Gets the XP range for the current level (start and end).
    /// </summary>
    public static (int levelStart, int levelEnd) GetLevelXPRange(int level)
    {
        var levelStart = GetXPForLevel(level);
        var levelEnd = GetXPForLevel(level + 1);
        return (levelStart, levelEnd);
    }

    /// <summary>
    /// Gets progress through current level as a percentage (0.0 to 1.0).
    /// </summary>
    public static float GetLevelProgress(int currentXp)
    {
        var currentLevel = GetLevelFromXP(currentXp);
        var (levelStart, levelEnd) = GetLevelXPRange(currentLevel);
        
        if (levelEnd <= levelStart)
            return 0f;

        var progress = (float)(currentXp - levelStart) / (levelEnd - levelStart);
        return Math.Clamp(progress, 0f, 1f);
    }

    /// <summary>
    /// Simulates gold rewards from a run (placeholder for future tuning).
    /// </summary>
    public static int CalculateGoldReward(RunSession run)
    {
        // Base gold from wave completion
        var baseGold = run.WaveReached * 10;

        // Bonus gold from kills
        var killGold = run.TotalKills * 2;

        // Bonus for reaching certain wave milestones
        var milestoneBonus = 0;
        if (run.WaveReached >= 10) milestoneBonus += 50;
        if (run.WaveReached >= 20) milestoneBonus += 100;
        if (run.WaveReached >= 30) milestoneBonus += 200;

        return baseGold + killGold + milestoneBonus;
    }
}
