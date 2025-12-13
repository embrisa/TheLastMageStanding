using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.SceneState;
using Xunit;

namespace TheLastMageStanding.Game.Tests.SceneState;

public class SceneManagerTests
{
    [Fact]
    public void StageTransition_PersistsStageId_And_AllowsStageRestart()
    {
        var sceneStateService = new SceneStateService();
        var eventBus = new EventBus();
        var sceneManager = new SceneManager(sceneStateService, eventBus);

        var enterEvents = new List<SceneEnterEvent>();
        eventBus.Subscribe<SceneEnterEvent>(evt => enterEvents.Add(evt));

        sceneManager.TransitionToHub();
        Assert.True(sceneManager.ProcessPendingTransition());
        eventBus.ProcessEvents();

        sceneManager.TransitionToStage("act1_stage1");
        Assert.True(sceneManager.ProcessPendingTransition());
        eventBus.ProcessEvents();

        // Stage -> Stage restart (should not be ignored)
        sceneManager.TransitionToStage("act1_stage1");
        Assert.True(sceneManager.ProcessPendingTransition());
        eventBus.ProcessEvents();

        Assert.Equal(SceneType.Stage, sceneManager.CurrentScene);
        Assert.Equal("act1_stage1", sceneManager.CurrentStageId);
        Assert.Equal("act1_stage1", sceneStateService.CurrentStageId);
        Assert.Equal(3, enterEvents.Count);
        Assert.Equal(SceneType.Stage, enterEvents[^1].Scene);
        Assert.Equal("act1_stage1", enterEvents[^1].StageId);
    }
}





