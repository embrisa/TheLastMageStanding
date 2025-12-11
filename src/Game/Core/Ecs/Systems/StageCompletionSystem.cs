using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.SceneState;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles stage completion and transitions back to hub.
/// </summary>
internal sealed class StageCompletionSystem : IUpdateSystem
{
    private readonly SceneManager _sceneManager;
    private EcsWorld _world = null!;
    private bool _runEndedHandled;

    public StageCompletionSystem(SceneManager sceneManager)
    {
        _sceneManager = sceneManager;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<RunEndedEvent>(OnRunEnded);
        world.EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        world.EventBus.Subscribe<SceneEnterEvent>(OnSceneEnter);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // This system mainly reacts to events
    }

    private void OnRunEnded(RunEndedEvent evt)
    {
        if (_runEndedHandled)
        {
            return;
        }

        _runEndedHandled = true;
        Console.WriteLine("[StageCompletion] Run ended, transitioning to hub...");
        
        // TODO: Show rewards screen before transition
        // For now, just transition immediately
        _sceneManager.TransitionToHub();
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        // Player death triggers run end, which will call OnRunEnded
        Console.WriteLine("[StageCompletion] Player died");
    }

    private void OnSceneEnter(SceneEnterEvent evt)
    {
        // Reset flag so subsequent runs can transition back to hub again
        _runEndedHandled = false;
    }
}
