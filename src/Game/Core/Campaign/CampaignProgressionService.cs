using TheLastMageStanding.Game.Core.MetaProgression;

namespace TheLastMageStanding.Game.Core.Campaign;

/// <summary>
/// Reads/writes campaign progression from the persistent player profile.
/// Unlock rules are derived from stage requirements plus completion state.
/// </summary>
public sealed class CampaignProgressionService
{
    private readonly StageRegistry _stageRegistry;
    private readonly PlayerProfileService _profileService;

    public CampaignProgressionService(StageRegistry stageRegistry, PlayerProfileService profileService)
    {
        _stageRegistry = stageRegistry;
        _profileService = profileService;
    }

    public PlayerProfile LoadProfile() => _profileService.LoadProfile();

    public bool IsStageCompleted(string stageId, PlayerProfile profile) =>
        profile.CompletedStages.Contains(stageId);

    public bool IsStageUnlocked(StageDefinition stage, PlayerProfile profile)
    {
        if (profile.MetaLevel < stage.RequiredMetaLevel)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(stage.RequiredPreviousStageId) &&
            !profile.CompletedStages.Contains(stage.RequiredPreviousStageId))
        {
            return false;
        }

        return true;
    }

    public bool IsStageUnlocked(string stageId, PlayerProfile profile)
    {
        var stage = _stageRegistry.GetStage(stageId);
        return stage != null && IsStageUnlocked(stage, profile);
    }

    public void MarkStageCompleted(string stageId)
    {
        var profile = _profileService.LoadProfile();
        if (profile.CompletedStages.Contains(stageId))
        {
            return;
        }

        profile.CompletedStages.Add(stageId);
        _profileService.SaveProfile(profile);
    }

    public StageDefinition? GetStage(string stageId) => _stageRegistry.GetStage(stageId);

    public BossDefinition? GetBossForStage(StageDefinition stage)
    {
        if (!stage.IsBossStage || string.IsNullOrEmpty(stage.BossId))
        {
            return null;
        }

        var act = _stageRegistry.GetAct(stage.ActNumber);
        if (act?.Boss?.BossId == stage.BossId)
        {
            return act.Boss;
        }

        return _stageRegistry.GetAllActs().Select(a => a.Boss).FirstOrDefault(b => b?.BossId == stage.BossId);
    }

    public string GetLockReason(StageDefinition stage, PlayerProfile profile)
    {
        if (profile.MetaLevel < stage.RequiredMetaLevel)
        {
            return $"Requires Meta Level {stage.RequiredMetaLevel}";
        }

        if (!string.IsNullOrEmpty(stage.RequiredPreviousStageId) &&
            !profile.CompletedStages.Contains(stage.RequiredPreviousStageId))
        {
            var req = _stageRegistry.GetStage(stage.RequiredPreviousStageId);
            if (req != null)
            {
                return $"Complete Act {req.ActNumber} - Stage {req.StageNumber}: {req.DisplayName}";
            }

            return "Complete previous stage";
        }

        return "Locked";
    }
}

