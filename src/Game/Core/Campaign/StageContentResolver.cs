namespace TheLastMageStanding.Game.Core.Campaign;

/// <summary>
/// Resolves stage content (maps, etc.) based on a stage id with a safe fallback.
/// </summary>
internal sealed class StageContentResolver
{
    private readonly StageRegistry _stageRegistry;
    private readonly string _hubMapAsset;

    public StageContentResolver(StageRegistry stageRegistry, string hubMapAsset)
    {
        _stageRegistry = stageRegistry;
        _hubMapAsset = hubMapAsset;
    }

    /// <summary>
    /// Returns the map asset path for the provided stage id, or the hub map if missing.
    /// </summary>
    public string ResolveMapAssetForStage(string? stageId)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            return _hubMapAsset;
        }

        var stage = _stageRegistry.GetStage(stageId);
        if (stage == null || string.IsNullOrWhiteSpace(stage.MapAssetPath))
        {
            return _hubMapAsset;
        }

        return stage.MapAssetPath;
    }
}


