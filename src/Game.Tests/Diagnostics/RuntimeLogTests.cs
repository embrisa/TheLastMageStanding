using System;
using System.Collections.Generic;
using System.IO;
using TheLastMageStanding.Game.Core.Diagnostics;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Diagnostics;

public sealed class RuntimeLogTests
{
    [Fact]
    public void ConsoleRuntimeLogger_FiltersByLevelAndCategory()
    {
        var writer = new StringWriter();
        var logger = new ConsoleRuntimeLogger(
            new RuntimeLogSettings
            {
                MinimumLevel = RuntimeLogLevel.Warning,
                EnabledCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "EventBus" }
            },
            writer,
            () => new DateTimeOffset(2026, 03, 22, 12, 00, 00, TimeSpan.Zero));

        logger.Log(RuntimeLogLevel.Info, "EventBus", "ignored");
        logger.Log(RuntimeLogLevel.Warning, "Scene", "ignored");
        logger.Log(RuntimeLogLevel.Error, "EventBus", "captured");

        var output = writer.ToString();
        Assert.DoesNotContain("ignored", output, StringComparison.Ordinal);
        Assert.Contains("[Error] [EventBus] captured", output, StringComparison.Ordinal);
    }
}
