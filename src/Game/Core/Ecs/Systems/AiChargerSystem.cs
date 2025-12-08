using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles charger role behavior: seek, telegraph, commit attack, cooldown.
/// </summary>
internal sealed class AiChargerSystem : IUpdateSystem
{
    private readonly List<(Entity entity, Faction faction, Vector2 position)> _targets = new();

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;
        CacheTargets(world);

        world.ForEach<AiRoleConfig, AiBehaviorStateMachine, Position>(
            (Entity entity, ref AiRoleConfig roleConfig, ref AiBehaviorStateMachine state, ref Position position) =>
            {
                if (roleConfig.Role != EnemyRole.Charger)
                {
                    return;
                }

                if (!world.TryGetComponent(entity, out MoveSpeed moveSpeed))
                {
                    return;
                }

                if (!world.TryGetComponent(entity, out Health health) || health.IsDead)
                {
                    world.SetComponent(entity, new Velocity(Vector2.Zero));
                    state.State = AiBehaviorState.Idle;
                    state.HasTarget = false;
                    world.SetComponent(entity, state);
                    return;
                }

                // Cooldown ticks regardless of state
                state.CooldownTimer = MathF.Max(0f, state.CooldownTimer - deltaSeconds);

                var hasTarget = TryGetNearestTarget(position.Value, Faction.Player, out var targetEntity, out var targetPosition);
                state.HasTarget = hasTarget;
                state.TargetEntity = targetEntity;

                switch (state.State)
                {
                    case AiBehaviorState.Idle:
                    case AiBehaviorState.Seeking:
                        HandleSeeking(world, entity, ref state, ref position, in moveSpeed, in roleConfig, in targetPosition, hasTarget);
                        break;
                    case AiBehaviorState.Committing:
                        HandleCommit(world, entity, ref state, ref position, in roleConfig, in targetPosition, deltaSeconds, hasTarget);
                        break;
                    case AiBehaviorState.Cooldown:
                        HandleCooldown(world, entity, ref state, ref position, in roleConfig, deltaSeconds);
                        break;
                    default:
                        world.SetComponent(entity, new Velocity(Vector2.Zero));
                        state.State = AiBehaviorState.Seeking;
                        break;
                }

                world.SetComponent(entity, state);
            });
    }

    private static void HandleSeeking(
        EcsWorld world,
        Entity entity,
        ref AiBehaviorStateMachine state,
        ref Position position,
        in MoveSpeed moveSpeed,
        in AiRoleConfig roleConfig,
        in Vector2 targetPosition,
        bool hasTarget)
    {
        if (!hasTarget)
        {
            world.SetComponent(entity, new Velocity(Vector2.Zero));
            state.State = AiBehaviorState.Idle;
            return;
        }

        var direction = targetPosition - position.Value;
        var distance = direction.Length();
        if (distance <= 0.001f)
        {
            world.SetComponent(entity, new Velocity(Vector2.Zero));
            return;
        }

        direction /= distance;
        state.AimDirection = direction;

        var inCommitRange = distance >= roleConfig.CommitRangeMin && distance <= roleConfig.CommitRangeMax;
        if (inCommitRange && state.CooldownTimer <= 0f)
        {
            state.State = AiBehaviorState.Committing;
            state.StateTimer = MathF.Max(0.01f, roleConfig.WindupDuration);
            world.SetComponent(entity, new Velocity(Vector2.Zero));
            SpawnTelegraph(world, position.Value, direction, roleConfig);
            BoostMassForCommit(world, entity, ref state);
            return;
        }

        var velocity = direction * moveSpeed.Value;
        world.SetComponent(entity, new Velocity(velocity));
        state.State = AiBehaviorState.Seeking;
    }

    private static void HandleCommit(
        EcsWorld world,
        Entity entity,
        ref AiBehaviorStateMachine state,
        ref Position position,
        in AiRoleConfig roleConfig,
        in Vector2 targetPosition,
        float deltaSeconds,
        bool hasTarget)
    {
        world.SetComponent(entity, new Velocity(Vector2.Zero));

        // Cancel if target lost or far away during windup
        if (!hasTarget || Vector2.Distance(position.Value, targetPosition) > (roleConfig.CommitRangeMax + 200f))
        {
            state.State = AiBehaviorState.Seeking;
            state.StateTimer = 0f;
            RestoreMass(world, entity, ref state);
            return;
        }

        state.StateTimer -= deltaSeconds;
        if (state.StateTimer > 0f)
        {
            return;
        }

        PerformCommitAttack(world, entity, in roleConfig, in state, position.Value, targetPosition);
        state.State = AiBehaviorState.Cooldown;
        state.CooldownTimer = MathF.Max(state.CooldownTimer, roleConfig.CooldownDuration);
        state.StateTimer = 0f;
        RestoreMass(world, entity, ref state);
    }

    private static void HandleCooldown(
        EcsWorld world,
        Entity entity,
        ref AiBehaviorStateMachine state,
        ref Position position,
        in AiRoleConfig roleConfig,
        float deltaSeconds)
    {
        world.SetComponent(entity, new Velocity(Vector2.Zero));
        state.StateTimer += deltaSeconds;

        if (state.CooldownTimer > 0f)
        {
            return;
        }

        state.State = AiBehaviorState.Seeking;
        state.StateTimer = 0f;
        RestoreMass(world, entity, ref state);
    }

    private static void PerformCommitAttack(
        EcsWorld world,
        Entity entity,
        in AiRoleConfig roleConfig,
        in AiBehaviorStateMachine state,
        Vector2 position,
        Vector2 targetPosition)
    {
        var direction = state.AimDirection;
        if (direction.LengthSquared() < 0.0001f)
        {
            direction = Vector2.Normalize(targetPosition - position);
            if (direction.LengthSquared() < 0.0001f)
            {
                direction = Vector2.UnitX;
            }
        }

        var attackOrigin = position + direction * 30f;
        var hitboxRadius = roleConfig.Telegraph?.Radius ?? 45f;

        var damageMultiplier = 1.5f;
        var damage = world.TryGetComponent(entity, out AttackStats attackStats)
            ? attackStats.Damage * damageMultiplier
            : 12f;

        var ownerFaction = world.TryGetComponent(entity, out Faction faction) ? faction : Faction.Enemy;

        var hitboxEntity = world.CreateEntity();
        world.SetComponent(hitboxEntity, new Position(attackOrigin));
        world.SetComponent(hitboxEntity, new AttackHitbox(entity, damage, ownerFaction, lifetimeSeconds: 0.15f));
        world.SetComponent(
            hitboxEntity,
            Collider.CreateCircle(
                hitboxRadius,
                layer: CollisionLayer.Enemy,
                mask: CollisionLayer.Player,
                isTrigger: true));

        // Knockback target
        var knockbackForce = roleConfig.KnockbackForce > 0f ? roleConfig.KnockbackForce : 400f;
        if (state.TargetEntity.IsValid)
        {
            var toTarget = targetPosition - position;
            var directionToTarget = toTarget.LengthSquared() > 0.0001f ? Vector2.Normalize(toTarget) : direction;
            var knockbackVelocity = directionToTarget * knockbackForce;
            KnockbackSystem.ApplyKnockback(world, state.TargetEntity, knockbackVelocity, 0.2f);
        }

        // Small self knockback for commitment feel
        KnockbackSystem.ApplyKnockback(world, entity, -direction * 50f, 0.15f);
    }

    private static void SpawnTelegraph(EcsWorld world, Vector2 position, Vector2 direction, in AiRoleConfig roleConfig)
    {
        var telegraphData = roleConfig.Telegraph ?? new TelegraphData(
            duration: roleConfig.WindupDuration,
            color: new Color(255, 50, 50, 180),
            radius: 45f,
            offset: Vector2.Zero);

        var offset = direction * 30f + telegraphData.Offset;
        var dataWithOffset = new TelegraphData(
            telegraphData.Duration,
            telegraphData.Color,
            telegraphData.Radius,
            offset,
            telegraphData.Shape);

        TelegraphSystem.SpawnTelegraph(world, position, dataWithOffset);
    }

    private static void BoostMassForCommit(EcsWorld world, Entity entity, ref AiBehaviorStateMachine state)
    {
        if (state.PreviousMass > 0f)
        {
            return;
        }

        if (world.TryGetComponent(entity, out Mass mass))
        {
            state.PreviousMass = mass.Value;
            mass.Value = MathF.Max(mass.Value, 10f);
            world.SetComponent(entity, mass);
        }
    }

    private static void RestoreMass(EcsWorld world, Entity entity, ref AiBehaviorStateMachine state)
    {
        if (state.PreviousMass <= 0f)
        {
            return;
        }

        if (world.TryGetComponent(entity, out Mass mass))
        {
            mass.Value = state.PreviousMass;
            world.SetComponent(entity, mass);
        }

        state.PreviousMass = 0f;
    }

    private void CacheTargets(EcsWorld world)
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
    }

    private bool TryGetNearestTarget(Vector2 origin, Faction targetFaction, out Entity targetEntity, out Vector2 targetPosition)
    {
        var bestDistance = float.MaxValue;
        targetEntity = Entity.None;
        targetPosition = Vector2.Zero;

        foreach (var target in _targets)
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
            targetEntity = target.entity;
            targetPosition = target.position;
        }

        return targetEntity.IsValid;
    }
}

