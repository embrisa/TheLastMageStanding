using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Campaign;

public sealed record StageRewardDefinition(
    int CompletionGold,
    int CompletionMetaXpBonus,
    bool GuaranteedLoot);

public sealed record BiomeDefinition(
    string BiomeId,
    string DisplayName,
    Color PrimaryColor,
    Color SecondaryColor,
    IReadOnlyList<string> EnemyArchetypeIds);

public sealed record BossPhaseDefinition(
    string Name,
    float StartsBelowHealthFraction,
    float MoveSpeedMultiplier,
    float AttackCooldownMultiplier,
    float ProjectileDamageMultiplier,
    int SummonOnEnterCount,
    string? SummonArchetypeId);

public sealed record BossDefinition(
    string BossId,
    string DisplayName,
    string Description,
    string BossArchetypeId,
    IReadOnlyList<BossPhaseDefinition> Phases);

public sealed record ActDefinition(
    int ActNumber,
    string DisplayName,
    BiomeDefinition Biome,
    IReadOnlyList<StageDefinition> Stages,
    BossDefinition? Boss);

/// <summary>
/// Represents a stage/level in the campaign.
/// </summary>
public sealed class StageDefinition
{
    public string StageId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int ActNumber { get; init; }
    public int StageNumber { get; init; }
    public string Description { get; init; } = string.Empty;
    public int RequiredMetaLevel { get; init; }
    public string? RequiredPreviousStageId { get; init; }
    public string MapAssetPath { get; init; } = string.Empty;

    public string BiomeId { get; init; } = string.Empty;
    public int MaxWaves { get; init; } = 10;
    public bool IsBossStage { get; init; }
    public string? BossId { get; init; }
    public StageRewardDefinition Rewards { get; init; } = new(CompletionGold: 0, CompletionMetaXpBonus: 0, GuaranteedLoot: false);

    public StageDefinition() { }

    public StageDefinition(
        string stageId,
        string displayName,
        int actNumber,
        int stageNumber,
        string description,
        int requiredMetaLevel,
        string? requiredPreviousStageId,
        string mapAssetPath,
        string biomeId,
        int maxWaves,
        bool isBossStage,
        string? bossId,
        StageRewardDefinition rewards)
    {
        StageId = stageId;
        DisplayName = displayName;
        ActNumber = actNumber;
        StageNumber = stageNumber;
        Description = description;
        RequiredMetaLevel = requiredMetaLevel;
        RequiredPreviousStageId = requiredPreviousStageId;
        MapAssetPath = mapAssetPath;
        BiomeId = biomeId;
        MaxWaves = maxWaves;
        IsBossStage = isBossStage;
        BossId = bossId;
        Rewards = rewards;
    }
}

