using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class CleanupSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Remove entities that expired or died; use a snapshot to avoid modifying during iteration.
        var toRemove = new List<Entity>();
        var playerAlive = false;
        var deltaSeconds = context.DeltaSeconds;

        world.ForEach<Lifetime>(
            (Entity entity, ref Lifetime lifetime) =>
            {
                lifetime.RemainingSeconds -= deltaSeconds;
                if (lifetime.RemainingSeconds <= 0f)
                {
                    toRemove.Add(entity);
                }
            });

        world.ForEach<Health>(
            (Entity entity, ref Health health) =>
            {
                if (health.IsDead)
                {
                    toRemove.Add(entity);
                }
            });

        world.ForEach<PlayerTag, Health>(
            (Entity _, ref PlayerTag _, ref Health health) =>
            {
                if (!health.IsDead)
                {
                    playerAlive = true;
                }
            });

        if (!playerAlive)
        {
            world.ForEach<Faction>(
                (Entity entity, ref Faction faction) =>
                {
                    if (faction == Faction.Enemy)
                    {
                        toRemove.Add(entity);
                    }
                });
        }

        if (toRemove.Count == 0)
        {
            return;
        }

        foreach (var entity in toRemove)
        {
            world.DestroyEntity(entity);
        }
    }
}

