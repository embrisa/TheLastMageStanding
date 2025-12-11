using TheLastMageStanding.Game.Core.Campaign;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Campaign;

public class StageContentResolverTests
{
    [Fact]
    public void ResolveMapAssetForStage_ReturnsStageMap()
    {
        var registry = new StageRegistry();
        var resolver = new StageContentResolver(registry, "fallback");

        var result = resolver.ResolveMapAssetForStage("act1_stage1");

        Assert.Equal("Tiles/Maps/FirstMap", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("missing-stage")]
    public void ResolveMapAssetForStage_FallsBackWhenMissing(string? stageId)
    {
        var registry = new StageRegistry();
        var resolver = new StageContentResolver(registry, "fallback");

        var result = resolver.ResolveMapAssetForStage(stageId);

        Assert.Equal("fallback", result);
    }
}


