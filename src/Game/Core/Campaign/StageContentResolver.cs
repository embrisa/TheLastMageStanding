using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Diagnostics;

namespace TheLastMageStanding.Game.Core.Campaign;

/// <summary>
/// Resolves stage content (maps, etc.) based on a stage id.
/// </summary>
internal sealed class StageContentResolver
{
    private const string LogCategory = "Campaign.StageContent";
    private readonly StageRegistry _stageRegistry;

    public StageContentResolver(StageRegistry stageRegistry)
    {
        _stageRegistry = stageRegistry;
    }

    /// <summary>
    /// Returns the map asset path for the provided stage id.
    /// </summary>
    public string ResolveMapAssetForStage(string? stageId)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            var message = "Stage id is required to resolve stage content.";
            RuntimeLog.Error(LogCategory, message);
            throw new ArgumentException(message, nameof(stageId));
        }

        var stage = _stageRegistry.GetStage(stageId);
        if (stage == null)
        {
            var message = $"No stage definition is registered for stage id '{stageId}'.";
            RuntimeLog.Error(LogCategory, message);
            throw new KeyNotFoundException(message);
        }

        if (string.IsNullOrWhiteSpace(stage.MapAssetPath))
        {
            var message = $"Stage '{stageId}' does not define a map asset path.";
            RuntimeLog.Error(LogCategory, message);
            throw new InvalidOperationException(message);
        }

        return stage.MapAssetPath;
    }
}
