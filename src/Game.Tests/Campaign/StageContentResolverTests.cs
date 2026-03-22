using System;
using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Campaign;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Campaign;

public sealed class StageContentResolverTests
{
    [Fact]
    public void ResolveMapAssetForStage_ReturnsStageMap()
    {
        var registry = new StageRegistry();
        var resolver = new StageContentResolver(registry);

        var result = resolver.ResolveMapAssetForStage("act1_stage1");

        Assert.Equal("Tiles/Maps/FirstMap", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ResolveMapAssetForStage_ThrowsWhenStageIdIsMissing(string? stageId)
    {
        var registry = new StageRegistry();
        var resolver = new StageContentResolver(registry);

        var exception = Assert.Throws<ArgumentException>(() => resolver.ResolveMapAssetForStage(stageId));

        Assert.Equal("stageId", exception.ParamName);
        Assert.Contains("Stage id is required", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveMapAssetForStage_ThrowsWhenStageDefinitionDoesNotExist()
    {
        var registry = new StageRegistry();
        var resolver = new StageContentResolver(registry);

        var exception = Assert.Throws<KeyNotFoundException>(() => resolver.ResolveMapAssetForStage("missing-stage"));

        Assert.Contains("missing-stage", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveMapAssetForStage_ThrowsWhenStageDoesNotDefineAMapAsset()
    {
        var registry = new StageRegistry();
        registry.Register(new StageDefinition
        {
            StageId = "broken-stage",
            DisplayName = "Broken",
            ActNumber = 99,
            StageNumber = 1,
            Description = "Missing map asset for validation",
            RequiredMetaLevel = 1,
            MapAssetPath = string.Empty
        });

        var resolver = new StageContentResolver(registry);

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.ResolveMapAssetForStage("broken-stage"));

        Assert.Contains("broken-stage", exception.Message, StringComparison.Ordinal);
        Assert.Contains("does not define a map asset path", exception.Message, StringComparison.Ordinal);
    }
}
