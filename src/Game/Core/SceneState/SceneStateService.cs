namespace TheLastMageStanding.Game.Core.SceneState;

/// <summary>
/// Tracks the current scene type (Hub vs Stage) to enforce hub-only configuration rules.
/// </summary>
internal sealed class SceneStateService
{
    private SceneType _currentScene = SceneType.MainMenu; // Default to main menu
    private string? _currentStageId;

    /// <summary>
    /// Gets the current scene type.
    /// </summary>
    public SceneType CurrentScene => _currentScene;

    /// <summary>
    /// Gets the current stage id if in a stage scene; otherwise null.
    /// </summary>
    public string? CurrentStageId => _currentStageId;

    /// <summary>
    /// Returns true if a stage id is currently active.
    /// </summary>
    public bool HasActiveStage => !string.IsNullOrWhiteSpace(_currentStageId);

    /// <summary>
    /// Returns true if currently in the Hub scene.
    /// </summary>
    public bool IsInHub() => _currentScene == SceneType.Hub;

    /// <summary>
    /// Returns true if currently in a Stage (combat) scene.
    /// </summary>
    public bool IsInStage() => _currentScene == SceneType.Stage;

    /// <summary>
    /// Returns true if currently in the main menu.
    /// </summary>
    public bool IsInMainMenu() => _currentScene == SceneType.MainMenu;

    /// <summary>
    /// Sets the current scene type and optional stage id.
    /// </summary>
    public void SetScene(SceneType scene, string? stageId = null)
    {
        _currentScene = scene;
        _currentStageId = scene == SceneType.Stage ? stageId : null;
    }
}
