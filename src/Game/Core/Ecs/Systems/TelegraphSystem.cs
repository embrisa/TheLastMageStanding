using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Manages telegraph lifecycle and visual warnings.
/// </summary>
internal sealed class TelegraphSystem : IUpdateSystem
{
    public static bool ShowTelegraphs { get; set; } = true;

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!ShowTelegraphs) return;

        var deltaSeconds = context.DeltaSeconds;

        // Update active telegraphs
        world.ForEach<ActiveTelegraph>((Entity entity, ref ActiveTelegraph telegraph) =>
        {
            telegraph.RemainingTime -= deltaSeconds;

            if (telegraph.RemainingTime <= 0f)
            {
                world.RemoveComponent<ActiveTelegraph>(entity);
            }
        });
    }

    /// <summary>
    /// Spawns a telegraph warning at the given position.
    /// </summary>
    public static void SpawnTelegraph(EcsWorld world, Vector2 position, TelegraphData data)
    {
        if (!ShowTelegraphs) return;

        var entity = world.CreateEntity();
        world.SetComponent(entity, new Position(position + data.Offset));
        world.SetComponent(entity, new ActiveTelegraph(data.Duration, data));
    }
}
