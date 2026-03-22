using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Tests.MetaProgression;
using Xunit;

namespace TheLastMageStanding.Game.Tests.SceneState;

public class ActiveSaveSlotControllerTests
{
    [Fact]
    public void EnsureActiveSlot_UsesMostRecentExistingSlot()
    {
        var fileSystem = new InMemoryFileSystem();
        var saveSlotService = new SaveSlotService(fileSystem, "/test/save");
        saveSlotService.CreateNextSlot();
        var expected = saveSlotService.CreateNextSlot();

        var controller = new ActiveSaveSlotController(saveSlotService);

        var activeSlotId = controller.EnsureActiveSlot();

        Assert.Equal(expected.SlotId, activeSlotId);
        Assert.Equal(expected.SlotId, controller.CurrentSlotId);
    }

    [Fact]
    public void EnsureActiveSlot_CreatesSlotWhenNoneExist()
    {
        var fileSystem = new InMemoryFileSystem();
        var saveSlotService = new SaveSlotService(fileSystem, "/test/save");
        var controller = new ActiveSaveSlotController(saveSlotService);

        var activeSlotId = controller.EnsureActiveSlot();

        Assert.Equal("slot1", activeSlotId);
        Assert.True(saveSlotService.SlotExists(activeSlotId));
    }

    [Fact]
    public void ActivateSlot_ReturnsWhetherWorldMustBeRebuilt()
    {
        var fileSystem = new InMemoryFileSystem();
        var saveSlotService = new SaveSlotService(fileSystem, "/test/save");
        var controller = new ActiveSaveSlotController(saveSlotService);

        Assert.True(controller.ActivateSlot("slot1"));
        Assert.False(controller.ActivateSlot("slot1"));
        Assert.True(controller.ActivateSlot("slot2"));
        Assert.Equal("slot2", controller.CurrentSlotId);
    }
}
