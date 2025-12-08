using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Applies timed buffs to nearby allies for the buffer role.
/// </summary>
internal sealed class AiBufferSystem : IUpdateSystem
{
    private readonly List<(Entity entity, Vector2 position)> _allies = new();

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;
        CacheAllies(world);

        world.ForEach<AiRoleConfig, AiBehaviorStateMachine, Position>(
            (Entity entity, ref AiRoleConfig roleConfig, ref AiBehaviorStateMachine state, ref Position position) =>
            {
                if (roleConfig.Role != EnemyRole.Buffer)
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
                    case AiBehaviorState.Buffing:
                        HandleBuffing(world, entity, ref state, in roleConfig, deltaSeconds);
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
        if (state.CooldownTimer > 0f)
        {
            state.State = AiBehaviorState.Cooldown;
            return;
        }

        if (state.PerceptionTimer > 0f)
        {
            return;
        }

        state.PerceptionTimer = MathF.Max(0.5f, roleConfig.BuffRange * 0.01f);

        var hasAllyToBuff = AnyAllyInRange(position.Value, roleConfig.BuffRange);
        if (!hasAllyToBuff)
        {
            state.State = AiBehaviorState.Seeking;
            return;
        }

        // Apply buffs immediately on entering buffing state
        ApplyBuffs(world, position.Value, roleConfig, entity);
        state.State = AiBehaviorState.Buffing;
        state.StateTimer = roleConfig.BuffAnimationLock;
        world.SetComponent(entity, new Velocity(Vector2.Zero));

        // Visual pulse
        var telegraph = roleConfig.Telegraph ?? new TelegraphData(
            duration: roleConfig.BuffAnimationLock,
            color: new Color(120, 220, 120, 180),
            radius: roleConfig.BuffRange,
            offset: Vector2.Zero);
        TelegraphSystem.SpawnTelegraph(world, position.Value, telegraph);
        world.EventBus.Publish(new VfxSpawnEvent("buffer_pulse", position.Value, VfxType.WindupFlash, telegraph.Color));
    }

    private static void HandleBuffing(
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

    private void ApplyBuffs(EcsWorld world, Vector2 origin, AiRoleConfig roleConfig, Entity source)
    {
        foreach (var ally in _allies)
        {
            var distanceSq = Vector2.DistanceSquared(origin, ally.position);
            if (distanceSq > roleConfig.BuffRange * roleConfig.BuffRange)
            {
                continue;
            }

            var buff = new TimedBuff
            {
                Type = roleConfig.BuffType,
                Duration = roleConfig.BuffDuration,
                RemainingDuration = roleConfig.BuffDuration,
                Modifiers = roleConfig.BuffModifiers,
                Source = source
            };

            ApplyOrRefreshBuff(world, ally.entity, buff);
        }
    }

    private static void ApplyOrRefreshBuff(EcsWorld world, Entity target, TimedBuff buff)
    {
        ActiveBuffs active;
        if (!world.TryGetComponent(target, out active))
        {
            active = new ActiveBuffs { Buffs = new List<TimedBuff>() };
        }
        else if (active.Buffs == null)
        {
            active.Buffs = new List<TimedBuff>();
        }

        var refreshed = false;
        for (var i = 0; i < active.Buffs.Count; i++)
        {
            if (active.Buffs[i].Type != buff.Type)
            {
                continue;
            }

            var existing = active.Buffs[i];
            existing.RemainingDuration = MathF.Max(existing.RemainingDuration, buff.RemainingDuration);
            existing.Duration = MathF.Max(existing.Duration, buff.Duration);
            existing.Modifiers = buff.Modifiers;
            existing.Source = buff.Source;
            active.Buffs[i] = existing;
            refreshed = true;
            break;
        }

        if (!refreshed)
        {
            active.Buffs.Add(buff);
        }

        world.SetComponent(target, active);

        if (world.TryGetComponent(target, out ComputedStats stats))
        {
            ComputedStats.MarkDirty(ref stats);
            world.SetComponent(target, stats);
        }
    }

    private bool AnyAllyInRange(Vector2 origin, float range)
    {
        var rangeSq = range * range;
        foreach (var ally in _allies)
        {
            var distance = Vector2.DistanceSquared(origin, ally.position);
            if (distance <= rangeSq)
            {
                return true;
            }
        }

        return false;
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
}

