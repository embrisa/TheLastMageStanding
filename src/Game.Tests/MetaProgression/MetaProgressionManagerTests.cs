using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.MetaProgression;
using Xunit;

namespace TheLastMageStanding.Game.Tests.MetaProgression;

public sealed class MetaProgressionManagerTests
{
    [Fact]
    public void Dispose_UnsubscribesRunLifecycleHandlers()
    {
        var fileSystem = new InMemoryFileSystem();
        var persistenceRoot = new PersistenceRoot(fileSystem, "/test/save");
        var slot = persistenceRoot.ForSlot("slot-1");
        var eventBus = new EventBus();
        var manager = new MetaProgressionManager(eventBus, slot);

        eventBus.Publish(new RunStartedEvent());
        eventBus.ProcessEvents();

        Assert.NotNull(manager.CurrentRun);

        manager.Dispose();

        eventBus.Publish(new RunStartedEvent());
        eventBus.ProcessEvents();

        Assert.Null(manager.CurrentRun);
    }
}
