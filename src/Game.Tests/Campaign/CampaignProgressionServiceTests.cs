using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Tests.MetaProgression;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Campaign;

public class CampaignProgressionServiceTests
{
    [Fact]
    public void IsStageUnlocked_FirstStageUnlockedAtMetaLevel1()
    {
        var fileSystem = new InMemoryFileSystem();
        var profileService = new PlayerProfileService(fileSystem, "/test/save");
        var registry = new StageRegistry();
        var progression = new CampaignProgressionService(registry, profileService);

        var profile = PlayerProfile.CreateDefault();
        profile.MetaLevel = 1;

        var stage = registry.GetStage("act1_stage1");
        Assert.NotNull(stage);

        Assert.True(CampaignProgressionService.IsStageUnlocked(stage!, profile));
    }

    [Fact]
    public void IsStageUnlocked_RequiresPreviousStageCompletion()
    {
        var fileSystem = new InMemoryFileSystem();
        var profileService = new PlayerProfileService(fileSystem, "/test/save");
        var registry = new StageRegistry();
        var progression = new CampaignProgressionService(registry, profileService);

        var profile = PlayerProfile.CreateDefault();
        profile.MetaLevel = 10;

        var stage = registry.GetStage("act2_stage1");
        Assert.NotNull(stage);

        Assert.False(CampaignProgressionService.IsStageUnlocked(stage!, profile));
        Assert.Contains("Complete", progression.GetLockReason(stage!, profile));

        profile.CompletedStages.Add("act1_stage3");
        Assert.True(CampaignProgressionService.IsStageUnlocked(stage!, profile));
    }

    [Fact]
    public void IsStageUnlocked_RequiresMetaLevel()
    {
        var fileSystem = new InMemoryFileSystem();
        var profileService = new PlayerProfileService(fileSystem, "/test/save");
        var registry = new StageRegistry();
        var progression = new CampaignProgressionService(registry, profileService);

        var profile = PlayerProfile.CreateDefault();
        profile.MetaLevel = 1;
        profile.CompletedStages.Add("act1_stage1");

        var stage = registry.GetStage("act1_stage2");
        Assert.NotNull(stage);

        Assert.False(CampaignProgressionService.IsStageUnlocked(stage!, profile));
        Assert.Contains("Meta Level", progression.GetLockReason(stage!, profile));

        profile.MetaLevel = stage!.RequiredMetaLevel;
        Assert.True(CampaignProgressionService.IsStageUnlocked(stage!, profile));
    }
}
