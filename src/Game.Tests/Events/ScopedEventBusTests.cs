using TheLastMageStanding.Game.Core.Events;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Events;

public sealed class ScopedEventBusTests
{
    [Fact]
    public void Dispose_UnsubscribesTrackedHandlers()
    {
        var bus = new EventBus();
        var firstCount = 0;
        var secondCount = 0;

        using (var firstScope = new ScopedEventBus(bus))
        {
            firstScope.Subscribe<int>(_ => firstCount++);

            bus.Publish(1);
            bus.ProcessEvents();
        }

        bus.Publish(2);
        bus.ProcessEvents();

        using (var secondScope = new ScopedEventBus(bus))
        {
            secondScope.Subscribe<int>(_ => secondCount++);

            bus.Publish(3);
            bus.ProcessEvents();
        }

        Assert.Equal(1, firstCount);
        Assert.Equal(1, secondCount);
    }

    [Fact]
    public void RecreatingScope_DoesNotLeakHandlersFromDisposedScope()
    {
        var bus = new EventBus();
        var firstScopeCount = 0;
        var secondScopeCount = 0;

        using (var firstScope = new ScopedEventBus(bus))
        {
            firstScope.Subscribe<int>(_ => firstScopeCount++);
        }

        using (var secondScope = new ScopedEventBus(bus))
        {
            secondScope.Subscribe<int>(_ => secondScopeCount++);
            bus.Publish(1);
            bus.ProcessEvents();
        }

        Assert.Equal(0, firstScopeCount);
        Assert.Equal(1, secondScopeCount);
    }
}
