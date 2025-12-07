using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles AI for ranged enemies: spacing from target, windup, and projectile firing.
/// Overrides normal seek behavior to maintain optimal firing distance.
/// </summary>
internal sealed class RangedAttackSystem : IUpdateSystem
{
    private readonly List<(Entity entity, Faction faction, Vector2 position)> _targets = new();

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;

        // Gather all potential targets
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

        // Update ranged attackers
        world.ForEach<RangedAttacker, AiSeekTarget, Position>(
            (Entity entity, ref RangedAttacker ranged, ref AiSeekTarget ai, ref Position position) =>
            {
                // Get required components manually
                if (!world.TryGetComponent(entity, out MoveSpeed moveSpeed) ||
                    !world.TryGetComponent(entity, out AttackStats attack))
                {
                    return;
                }

                // Find nearest target
                if (!TryGetNearestTarget(position.Value, ai.TargetFaction, _targets, out var targetPosition))
                {
                    // No target - stop and reset
                    world.SetComponent(entity, new Velocity(Vector2.Zero));
                    ranged.IsWindingUp = false;
                    ranged.WindupTimer = 0f;
                    world.SetComponent(entity, ranged);
                    return;
                }

                var toTarget = targetPosition - position.Value;
                var distanceToTarget = toTarget.Length();

                // Check if we can attack (cooldown ready)
                var canAttack = attack.CooldownTimer <= 0f;

                // Determine desired distance based on whether we're ready to attack
                var minRange = ranged.OptimalRange * 0.8f;
                var maxRange = ranged.OptimalRange * 1.2f;

                // State machine: Move -> Windup -> Fire
                if (ranged.IsWindingUp)
                {
                    // Stop moving during windup
                    world.SetComponent(entity, new Velocity(Vector2.Zero));

                    // Update windup timer
                    ranged.WindupTimer += deltaSeconds;

                    // Check if windup complete
                    if (ranged.WindupTimer >= ranged.WindupSeconds)
                    {
                        // Fire projectile!
                        FireProjectile(world, entity, position.Value, targetPosition, ranged);

                        // Reset windup and start attack cooldown
                        ranged.IsWindingUp = false;
                        ranged.WindupTimer = 0f;
                        attack.CooldownTimer = attack.CooldownSeconds;
                        world.SetComponent(entity, attack);
                    }

                    world.SetComponent(entity, ranged);
                }
                else if (distanceToTarget >= minRange && distanceToTarget <= maxRange && canAttack)
                {
                    // In optimal range and ready to attack - start windup
                    world.SetComponent(entity, new Velocity(Vector2.Zero));
                    ranged.IsWindingUp = true;
                    ranged.WindupTimer = 0f;
                    world.SetComponent(entity, ranged);
                }
                else
                {
                    // Not in optimal range or on cooldown - move toward/away from target
                    Vector2 velocity;

                    if (distanceToTarget < minRange)
                    {
                        // Too close - back away
                        var direction = -Vector2.Normalize(toTarget);
                        velocity = direction * moveSpeed.Value * 0.7f; // Move slower when backing up
                    }
                    else
                    {
                        // Too far - move closer
                        var direction = Vector2.Normalize(toTarget);
                        velocity = direction * moveSpeed.Value;
                    }

                    world.SetComponent(entity, new Velocity(velocity));
                }
            });
    }

    private static void FireProjectile(EcsWorld world, Entity source, Vector2 sourcePosition, Vector2 targetPosition, RangedAttacker ranged)
    {
        // Calculate direction to target
        var direction = targetPosition - sourcePosition;
        if (direction.LengthSquared() < 0.0001f)
        {
            direction = new Vector2(1f, 0f); // Default direction if target is at exact same position
        }
        else
        {
            direction = Vector2.Normalize(direction);
        }

        // Get source faction
        var sourceFaction = world.TryGetComponent(source, out Faction faction) ? faction : Faction.Neutral;

        // Create projectile entity
        var projectileEntity = world.CreateEntity();

        // Position (slightly offset from source to avoid self-collision)
        var spawnOffset = direction * 10f;
        world.SetComponent(projectileEntity, new Position(sourcePosition + spawnOffset));

        // Velocity
        var velocity = direction * ranged.ProjectileSpeed;
        world.SetComponent(projectileEntity, new Velocity(velocity));

        // Projectile component
        world.SetComponent(projectileEntity, new Projectile(source, ranged.ProjectileDamage, sourceFaction, lifetimeSeconds: 5f));

        // Visual
        var projectileColor = sourceFaction == Faction.Enemy
            ? new Color(255, 100, 100) // Reddish for enemy projectiles
            : new Color(100, 100, 255); // Blueish for player projectiles (future)
        world.SetComponent(projectileEntity, new ProjectileVisual(projectileColor, radius: 4f));

        // Collider - trigger that hits opposing faction
        var projectileLayer = CollisionLayer.Projectile;
        var targetLayer = sourceFaction == Faction.Player ? CollisionLayer.Enemy : CollisionLayer.Player;
        world.SetComponent(
            projectileEntity,
            Collider.CreateCircle(
                radius: 4f,
                layer: projectileLayer,
                mask: targetLayer | CollisionLayer.WorldStatic, // Hit targets and walls
                isTrigger: true));
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
