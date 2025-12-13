using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class CleanupSystem : IUpdateSystem
{
    private Entity? _sessionEntity;

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!IsPlaying(world))
        {
            return;
        }

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
                    // Skip player entities - they shouldn't be removed on death
                    if (!world.TryGetComponent<PlayerTag>(entity, out _))
                    {
                        var deathPosition = world.TryGetComponent(entity, out Position position) ? position.Value : Vector2.Zero;
                        IReadOnlyList<EliteModifierType>? activeModifiers = null;
                        if (world.TryGetComponent(entity, out EliteModifierData modifierData) && modifierData.ActiveModifiers != null)
                        {
                            activeModifiers = new List<EliteModifierType>(modifierData.ActiveModifiers);
                        }
                        if (world.TryGetComponent<Faction>(entity, out var faction) && faction == Faction.Enemy)
                        {
                            world.EventBus.Publish(new EnemyDiedEvent(entity, deathPosition, activeModifiers));
                        }
                        toRemove.Add(entity);
                    }
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

    private bool IsPlaying(EcsWorld world)
    {
        if (_sessionEntity is null || !world.IsAlive(_sessionEntity.Value))
        {
            _sessionEntity = null;
            world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
            {
                _sessionEntity = entity;
            });
        }

        if (!_sessionEntity.HasValue)
        {
            return true;
        }

        return world.TryGetComponent(_sessionEntity.Value, out GameSession session) &&
               session.State == GameState.Playing;
    }
}
