using TheLastMageStanding.Game.Core.SceneState;

namespace TheLastMageStanding.Game.Core.Events;

/// <summary>
/// Published when a scene is being exited.
/// </summary>
public readonly record struct SceneExitEvent
{
    public SceneType Scene { get; init; }

    public SceneExitEvent(SceneType scene)
    {
        Scene = scene;
    }
}

/// <summary>
/// Published when a new scene is entered.
/// </summary>
public readonly record struct SceneEnterEvent
{
    public SceneType Scene { get; init; }
    public string? StageId { get; init; }

    public SceneEnterEvent(SceneType scene, string? stageId)
    {
        Scene = scene;
        StageId = stageId;
    }
}
