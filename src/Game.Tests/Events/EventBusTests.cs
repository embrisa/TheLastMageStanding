using System;
using System.Collections.Generic;
using System.IO;
using TheLastMageStanding.Game.Core.Diagnostics;
using TheLastMageStanding.Game.Core.Events;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Events;

public sealed class EventBusTests
{
    [Fact]
    public void ProcessEvents_ProcessesSelfRepublishedEventsAcrossMultiplePasses()
    {
        var eventBus = new EventBus(maxPasses: 5);
        var handled = new List<int>();

        eventBus.Subscribe<int>(value =>
        {
            handled.Add(value);
            if (value < 3)
            {
                eventBus.Publish(value + 1);
            }
        });

        eventBus.Publish(1);
        eventBus.ProcessEvents();

        Assert.Equal(new[] { 1, 2, 3 }, handled);
    }

    [Fact]
    public void ProcessEvents_ThrowsAndLogsWhenMaxPassesAreExceeded()
    {
        var eventBus = new EventBus(maxPasses: 2);
        var writer = new StringWriter();
        RuntimeLog.Configure(new ConsoleRuntimeLogger(
            new RuntimeLogSettings
            {
                MinimumLevel = RuntimeLogLevel.Error,
                EnabledCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "EventBus" }
            },
            writer,
            () => new DateTimeOffset(2026, 03, 22, 12, 00, 00, TimeSpan.Zero)));

        try
        {
            eventBus.Subscribe<int>(value => eventBus.Publish(value + 1));
            eventBus.Publish(0);

            var exception = Assert.Throws<EventBusOverflowException>(() => eventBus.ProcessEvents());

            Assert.Contains("configured max of 2 passes", exception.Message, StringComparison.Ordinal);
            Assert.Contains("[Error] [EventBus]", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            RuntimeLog.ResetToDefaults();
        }
    }
}
