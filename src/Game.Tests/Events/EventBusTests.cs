using System;
using System.Collections.Generic;
using System.IO;
using TheLastMageStanding.Game.Core.Diagnostics;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.SceneState;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Events;

public sealed class EventBusTests
{
    private static readonly int[] ExpectedSelfRepublishedEvents = [1, 2, 3];

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

        Assert.Equal(ExpectedSelfRepublishedEvents, handled);
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

    [Fact]
    public void SubscriptionDispose_UnsubscribesHandler()
    {
        var eventBus = new EventBus();
        var handled = 0;
        var subscription = eventBus.Subscribe<int>(_ => handled++);

        eventBus.Publish(1);
        eventBus.ProcessEvents();
        subscription.Dispose();
        eventBus.Publish(2);
        eventBus.ProcessEvents();

        Assert.Equal(1, handled);
    }

    [Fact]
    public void GetDiagnosticsSnapshot_ReturnsPendingQueuesAndSubscribers()
    {
        var eventBus = new EventBus(maxPasses: 3);
        eventBus.Subscribe<int>(_ => { });
        eventBus.Subscribe<SceneEnterEvent>(_ => { });
        eventBus.Publish(42);

        var diagnostics = eventBus.GetDiagnosticsSnapshot();

        Assert.Equal(3, diagnostics.MaxPasses);
        Assert.Equal(1, diagnostics.PendingEventCount);
        Assert.Equal(2, diagnostics.SubscriberCount);
        Assert.Contains(
            diagnostics.EventTypes,
            entry => entry.EventType == typeof(int) && entry.PendingCount == 1 && entry.SubscriberCount == 1);
        Assert.Contains(
            diagnostics.EventTypes,
            entry => entry.EventType == typeof(SceneEnterEvent) && entry.PendingCount == 0 && entry.SubscriberCount == 1);
    }
}
