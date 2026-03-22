using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs;

/// <summary>
/// Removes transient stage entities that should never survive a run reset.
/// </summary>
internal sealed class StageRunEntityCleanupService
{
    public void RemoveTransientStageEntities(EcsWorld world)
    {
        var entitiesToRemove = new List<Entity>();

        world.ForEach<Faction>((Entity entity, ref Faction faction) =>
        {
            if (faction == Faction.Enemy)
            {
                entitiesToRemove.Add(entity);
            }
        });

        world.ForEach<XpOrb>((Entity entity, ref XpOrb _) =>
        {
            entitiesToRemove.Add(entity);
        });

        foreach (var entity in entitiesToRemove)
        {
            if (world.IsAlive(entity))
            {
                world.DestroyEntity(entity);
            }
        }
    }
}
