using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Supports protector role: scan for incoming projectiles, apply brief shields to allies, enforce cooldown.
/// </summary>
internal sealed class AiProtectorSystem : IUpdateSystem
{
    private readonly List<(Entity entity, Vector2 position)> _allies = new();
    private readonly List<(Entity projectile, Vector2 position, Projectile data)> _projectiles = new();

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;

        CacheAllies(world);
        CacheProjectiles(world);
        TickShields(world, deltaSeconds);

        world.ForEach<AiRoleConfig, AiBehaviorStateMachine, Position>(
            (Entity entity, ref AiRoleConfig roleConfig, ref AiBehaviorStateMachine state, ref Position position) =>
            {
                if (roleConfig.Role != EnemyRole.Protector)
                {
                    return;
                }

                if (!world.TryGetComponent(entity, out Health health) || health.IsDead)
                {
                    state.State = AiBehaviorState.Idle;
                    state.HasTarget = false;
                    world.SetComponent(entity, state);
                    return;
                }

                state.CooldownTimer = MathF.Max(0f, state.CooldownTimer - deltaSeconds);
                state.PerceptionTimer = MathF.Max(0f, state.PerceptionTimer - deltaSeconds);

                switch (state.State)
                {
                    case AiBehaviorState.Idle:
                    case AiBehaviorState.Seeking:
                        HandleSeeking(world, entity, ref state, ref position, in roleConfig);
                        break;
                    case AiBehaviorState.Shielding:
                        HandleShielding(world, entity, ref state, in roleConfig, deltaSeconds);
                        break;
                    case AiBehaviorState.Cooldown:
                        HandleCooldown(ref state);
                        break;
                }

                world.SetComponent(entity, state);
            });
    }

    private void HandleSeeking(
        EcsWorld world,
        Entity entity,
        ref AiBehaviorStateMachine state,
        ref Position position,
        in AiRoleConfig roleConfig)
    {
        // Only scan when perception timer elapses to throttle checks
        if (state.PerceptionTimer > 0f)
        {
            return;
        }

        var hasNearbyAlly = TryGetAllyInRange(position.Value, roleConfig.ShieldRange, entity, out var allyEntity);
        var projectileThreat = hasNearbyAlly && TryFindProjectileThreat(allyEntity, roleConfig.ShieldDetectionRange, out _);

        state.PerceptionTimer = MathF.Max(0.2f, roleConfig.ShieldDetectionRange * 0.0015f); // throttle ~0.2s

        if (!hasNearbyAlly || !projectileThreat || state.CooldownTimer > 0f)
        {
            state.State = AiBehaviorState.Seeking;
            return;
        }

        // Start shielding
        state.State = AiBehaviorState.Shielding;
        state.StateTimer = roleConfig.ShieldDuration;
        world.SetComponent(entity, new Velocity(Vector2.Zero));
        ApplyShieldsToAllies(world, position.Value, roleConfig.ShieldRange, roleConfig.ShieldDuration, entity, roleConfig.ShieldBlocksProjectiles);

        // Visual cue
        var telegraphData = roleConfig.Telegraph ?? new TelegraphData(
            duration: roleConfig.ShieldDuration,
            color: new Color(80, 140, 255, 180),
            radius: roleConfig.ShieldRange,
            offset: Vector2.Zero);
        TelegraphSystem.SpawnTelegraph(world, position.Value, telegraphData);
    }

    private static void HandleShielding(
        EcsWorld world,
        Entity entity,
        ref AiBehaviorStateMachine state,
        in AiRoleConfig roleConfig,
        float deltaSeconds)
    {
        world.SetComponent(entity, new Velocity(Vector2.Zero));

        state.StateTimer -= deltaSeconds;
        if (state.StateTimer > 0f)
        {
            return;
        }

        state.State = AiBehaviorState.Cooldown;
        state.CooldownTimer = MathF.Max(state.CooldownTimer, roleConfig.CooldownDuration);
        state.StateTimer = 0f;
        state.PerceptionTimer = 0f;
    }

    private static void HandleCooldown(ref AiBehaviorStateMachine state)
    {
        if (state.CooldownTimer > 0f)
        {
            state.State = AiBehaviorState.Cooldown;
            return;
        }

        state.State = AiBehaviorState.Seeking;
        state.PerceptionTimer = 0f;
    }

    private void ApplyShieldsToAllies(
        EcsWorld world,
        Vector2 origin,
        float shieldRange,
        float duration,
        Entity source,
        bool blocksProjectiles)
    {
        foreach (var ally in _allies)
        {
            if (ally.entity.Id == source.Id)
            {
                continue; // supporting allies, not self
            }

            var distanceSq = Vector2.DistanceSquared(origin, ally.position);
            if (distanceSq > shieldRange * shieldRange)
            {
                continue;
            }

            var blocksRemaining = blocksProjectiles ? 1 : 0;
            if (world.TryGetComponent(ally.entity, out ShieldActive shield))
            {
                shield.IsActive = true;
                shield.BlocksRemaining = Math.Max(shield.BlocksRemaining, blocksRemaining);
                shield.RemainingDuration = Math.Max(shield.RemainingDuration, duration);
                shield.Source = source;
                world.SetComponent(ally.entity, shield);
            }
            else
            {
                world.SetComponent(ally.entity, new ShieldActive(
                    isActive: true,
                    blocksRemaining: blocksRemaining,
                    durationSeconds: duration,
                    source: source));
            }
        }
    }

    private bool TryGetAllyInRange(Vector2 origin, float range, Entity self, out Entity allyEntity)
    {
        allyEntity = Entity.None;
        var bestDistance = float.MaxValue;
        var rangeSq = range * range;

        foreach (var ally in _allies)
        {
            if (ally.entity.Id == self.Id)
            {
                continue;
            }

            var distance = Vector2.DistanceSquared(origin, ally.position);
            if (distance > rangeSq || distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            allyEntity = ally.entity;
        }

        return allyEntity.IsValid;
    }

    private bool TryFindProjectileThreat(Entity ally, float detectionRange, out Entity projectileEntity)
    {
        projectileEntity = Entity.None;
        if (_projectiles.Count == 0)
        {
            return false;
        }

        var detectionSq = detectionRange * detectionRange;
        var allyPos = GetAllyPosition(ally);
        foreach (var projectile in _projectiles)
        {
            var distance = Vector2.DistanceSquared(projectile.position, allyPos);
            if (distance <= detectionSq)
            {
                projectileEntity = projectile.projectile;
                return true;
            }
        }

        return false;
    }

    private Vector2 GetAllyPosition(Entity ally)
    {
        foreach (var entry in _allies)
        {
            if (entry.entity.Id == ally.Id)
            {
                return entry.position;
            }
        }

        return Vector2.Zero;
    }

    private static void TickShields(EcsWorld world, float deltaSeconds)
    {
        world.ForEach<ShieldActive>(
            (Entity entity, ref ShieldActive shield) =>
            {
                shield.RemainingDuration -= deltaSeconds;
                if (!shield.IsActive || shield.RemainingDuration <= 0f || shield.BlocksRemaining <= 0)
                {
                    world.RemoveComponent<ShieldActive>(entity);
                    return;
                }

                world.SetComponent(entity, shield);
            });
    }

    private void CacheAllies(EcsWorld world)
    {
        _allies.Clear();
        world.ForEach<Position, Faction>(
            (Entity entity, ref Position position, ref Faction faction) =>
            {
                if (faction != Faction.Enemy)
                {
                    return;
                }

                if (world.TryGetComponent(entity, out Health health) && health.IsDead)
                {
                    return;
                }

                _allies.Add((entity, position.Value));
            });
    }

    private void CacheProjectiles(EcsWorld world)
    {
        _projectiles.Clear();
        world.ForEach<Projectile, Position>(
            (Entity entity, ref Projectile projectile, ref Position position) =>
            {
                if (projectile.SourceFaction == Faction.Enemy)
                {
                    return; // Ignore friendly projectiles
                }

                _projectiles.Add((entity, position.Value, projectile));
            });
    }
}

