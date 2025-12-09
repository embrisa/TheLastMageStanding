namespace TheLastMageStanding.Game.Core.SceneState;

/// <summary>
/// Tracks the current scene type (Hub vs Stage) to enforce hub-only configuration rules.
/// </summary>
internal sealed class SceneStateService
{
    private SceneType _currentScene = SceneType.MainMenu; // Default to main menu

    /// <summary>
    /// Gets the current scene type.
    /// </summary>
    public SceneType CurrentScene => _currentScene;

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
    /// Sets the current scene type.
    /// </summary>
    public void SetScene(SceneType scene)
    {
        _currentScene = scene;
    }
}
