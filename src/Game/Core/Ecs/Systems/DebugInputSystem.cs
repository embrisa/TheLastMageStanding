using TheLastMageStanding.Game.Core.Ecs.Systems.Collision;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles debug input commands like collision visualization toggle.
/// </summary>
internal sealed class DebugInputSystem : IUpdateSystem
{
    private readonly CollisionDebugRenderSystem _collisionDebugRender;

    public DebugInputSystem(CollisionDebugRenderSystem collisionDebugRender)
    {
        _collisionDebugRender = collisionDebugRender;
    }

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (context.Input.DebugTogglePressed)
        {
            _collisionDebugRender.Enabled = !_collisionDebugRender.Enabled;
            System.Console.WriteLine($"[Debug] Collision visualization: {(_collisionDebugRender.Enabled ? "ON" : "OFF")}");
        }
    }
}
