namespace TheLastMageStanding.Game.Core.Campaign;

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
    
    public StageDefinition() { }
    
    public StageDefinition(
        string stageId,
        string displayName,
        int actNumber,
        int stageNumber,
        string description,
        int requiredMetaLevel,
        string? requiredPreviousStageId,
        string mapAssetPath)
    {
        StageId = stageId;
        DisplayName = displayName;
        ActNumber = actNumber;
        StageNumber = stageNumber;
        Description = description;
        RequiredMetaLevel = requiredMetaLevel;
        RequiredPreviousStageId = requiredPreviousStageId;
        MapAssetPath = mapAssetPath;
    }
}

/// <summary>
/// Registry of all campaign stages.
/// </summary>
public sealed class StageRegistry
{
    private readonly Dictionary<string, StageDefinition> _stages = new();
    
    public StageRegistry()
    {
        RegisterDefaultStages();
    }
    
    private void RegisterDefaultStages()
    {
        // Act 1, Stage 1
        Register(new StageDefinition(
            stageId: "act1_stage1",
            displayName: "The Awakening",
            actNumber: 1,
            stageNumber: 1,
            description: "Your journey begins in the cursed forest.",
            requiredMetaLevel: 1,
            requiredPreviousStageId: null,
            mapAssetPath: "Tiles/Maps/FirstMap"
        ));
        
        // Act 1, Stage 2 (placeholder)
        Register(new StageDefinition(
            stageId: "act1_stage2",
            displayName: "Dark Woods",
            actNumber: 1,
            stageNumber: 2,
            description: "Venture deeper into the corrupted wilderness.",
            requiredMetaLevel: 3,
            requiredPreviousStageId: "act1_stage1",
            mapAssetPath: "Tiles/Maps/FirstMap" // TODO: Replace with actual stage 2 map
        ));
        
        // Act 1, Stage 3 (placeholder)
        Register(new StageDefinition(
            stageId: "act1_stage3",
            displayName: "Forest Guardian",
            actNumber: 1,
            stageNumber: 3,
            description: "Face the corrupted guardian of the forest.",
            requiredMetaLevel: 5,
            requiredPreviousStageId: "act1_stage2",
            mapAssetPath: "Tiles/Maps/FirstMap" // TODO: Replace with boss arena map
        ));
    }
    
    public void Register(StageDefinition stage)
    {
        _stages[stage.StageId] = stage;
    }
    
    public StageDefinition? GetStage(string stageId)
    {
        return _stages.TryGetValue(stageId, out var stage) ? stage : null;
    }
    
    public IReadOnlyList<StageDefinition> GetAllStages()
    {
        return _stages.Values.OrderBy(s => s.ActNumber).ThenBy(s => s.StageNumber).ToList();
    }
    
    public IReadOnlyList<StageDefinition> GetStagesForAct(int actNumber)
    {
        return _stages.Values
            .Where(s => s.ActNumber == actNumber)
            .OrderBy(s => s.StageNumber)
            .ToList();
    }
}
