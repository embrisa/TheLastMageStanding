using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class AiSeekSystem : IUpdateSystem
{
    private readonly List<(Entity entity, Faction faction, Vector2 position)> _targets = new();

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        _targets.Clear();

        world.ForEach<Position, Faction>(
            (Entity entity, ref Position position, ref Faction faction) =>
            {
                if (world.TryGetComponent(entity, out Health health) && health.IsDead)
                {
                    return;
                }

                _targets.Add((entity, faction, position.Value));
            });

        if (_targets.Count == 0)
        {
            world.ForEach<AiSeekTarget>(
                (Entity entity, ref AiSeekTarget _) =>
                {
                    world.SetComponent(entity, new Velocity(Vector2.Zero));
                });

            return;
        }

        world.ForEach<AiSeekTarget, MoveSpeed, Position>(
            (Entity entity, ref AiSeekTarget ai, ref MoveSpeed moveSpeed, ref Position position) =>
            {
                if (!TryGetNearestTarget(position.Value, ai.TargetFaction, _targets, out var targetPosition))
                {
                    world.SetComponent(entity, new Velocity(Vector2.Zero));
                    return;
                }

                var direction = targetPosition - position.Value;
                var distanceSquared = direction.LengthSquared();
                if (distanceSquared <= 0.0001f)
                {
                    world.SetComponent(entity, new Velocity(Vector2.Zero));
                    return;
                }

                var velocity = Vector2.Normalize(direction) * moveSpeed.Value;
                world.SetComponent(entity, new Velocity(velocity));
            });
    }

    private static bool TryGetNearestTarget(
        Vector2 origin,
        Faction targetFaction,
        List<(Entity entity, Faction faction, Vector2 position)> targets,
        out Vector2 targetPosition)
    {
        var bestDistance = float.MaxValue;
        targetPosition = Vector2.Zero;

        foreach (var target in targets)
        {
            if (target.faction != targetFaction)
            {
                continue;
            }

            var distance = Vector2.DistanceSquared(origin, target.position);
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            targetPosition = target.position;
        }

        return bestDistance < float.MaxValue;
    }
}

