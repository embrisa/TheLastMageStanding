using System;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.SceneState;

/// <summary>
/// Manages scene transitions and current scene state.
/// Coordinates loading/unloading of scenes and publishes transition events.
/// </summary>
internal sealed class SceneManager
{
    private readonly SceneStateService _sceneStateService;
    private readonly IEventBus _eventBus;
    
    private SceneType _currentScene;
    private SceneType? _pendingTransition;
    private string? _pendingStageId;
    private string? _currentStageId;

    public SceneType CurrentScene => _currentScene;
    public string? CurrentStageId => _currentStageId;
    public bool HasPendingTransition => _pendingTransition.HasValue;

    public SceneManager(SceneStateService sceneStateService, IEventBus eventBus)
    {
        _sceneStateService = sceneStateService;
        _eventBus = eventBus;
        _currentScene = SceneType.MainMenu; // Start in main menu
        _sceneStateService.SetScene(_currentScene, _currentStageId);
    }

    /// <summary>
    /// Transitions to the hub scene.
    /// </summary>
    public void TransitionToHub()
    {
        if (_currentScene == SceneType.Hub)
        {
            Console.WriteLine("[SceneManager] Already in Hub scene, ignoring transition request.");
            return;
        }

        _pendingTransition = SceneType.Hub;
        _pendingStageId = null;
        Console.WriteLine("[SceneManager] Queued transition to Hub");
    }

    /// <summary>
    /// Transitions to a stage scene with the given stage ID.
    /// </summary>
    public void TransitionToStage(string stageId)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            Console.WriteLine("[SceneManager] Ignoring transition to Stage with empty stageId.");
            return;
        }

        _pendingTransition = SceneType.Stage;
        _pendingStageId = stageId;
        Console.WriteLine($"[SceneManager] Queued transition to Stage: {stageId}");
    }

    /// <summary>
    /// Transitions back to the main menu.
    /// </summary>
    public void TransitionToMainMenu()
    {
        if (_currentScene == SceneType.MainMenu)
        {
            Console.WriteLine("[SceneManager] Already in MainMenu, ignoring transition request.");
            return;
        }

        _pendingTransition = SceneType.MainMenu;
        _pendingStageId = null;
        Console.WriteLine("[SceneManager] Queued transition to MainMenu");
    }

    /// <summary>
    /// Processes pending scene transitions. Should be called once per frame.
    /// Returns true if a transition was processed.
    /// </summary>
    public bool ProcessPendingTransition()
    {
        if (!_pendingTransition.HasValue)
        {
            return false;
        }

        var targetScene = _pendingTransition.Value;
        var stageId = _pendingStageId;
        
        _pendingTransition = null;
        _pendingStageId = null;

        ExecuteTransition(targetScene, stageId);
        return true;
    }

    private void ExecuteTransition(SceneType targetScene, string? stageId)
    {
        var resolvedStageId = targetScene == SceneType.Stage
            ? stageId ?? _currentStageId
            : null;

        Console.WriteLine($"[SceneManager] Executing transition: {_currentScene} -> {targetScene} (stageId: {resolvedStageId ?? "none"})");

        // Publish scene exit event
        _eventBus.Publish(new SceneExitEvent(_currentScene));

        // Update current scene
        _currentScene = targetScene;
        _currentStageId = resolvedStageId;
        _sceneStateService.SetScene(targetScene, resolvedStageId);

        // Publish scene enter event
        if (targetScene == SceneType.Hub)
        {
            _eventBus.Publish(new SceneEnterEvent(SceneType.Hub, null));
        }
        else if (targetScene == SceneType.Stage)
        {
            _eventBus.Publish(new SceneEnterEvent(SceneType.Stage, resolvedStageId));
        }
        else
        {
            _eventBus.Publish(new SceneEnterEvent(targetScene, null));
        }

        Console.WriteLine($"[SceneManager] Scene transition complete: {targetScene} (stageId: {resolvedStageId ?? "none"})");
    }
}
