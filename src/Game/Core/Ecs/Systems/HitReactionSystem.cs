using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Converts damage events into short-lived combat reactions (flash, slow, knockback).
/// </summary>
internal sealed class HitReactionSystem : IUpdateSystem
{
    private readonly Random _random = new();

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        world.ForEach<DamageEvent, Position>(
            (Entity entity, ref DamageEvent damage, ref Position position) =>
            {
                var direction = position.Value - damage.SourcePosition;
                if (direction.LengthSquared() < 0.0001f)
                {
                    direction = new Vector2(0f, -1f);
                }
                else
                {
                    direction = Vector2.Normalize(direction);
                }

                var targetFaction = world.TryGetComponent(entity, out Faction faction) ? faction : Faction.Neutral;
                var isPlayer = targetFaction == Faction.Player;

                var flashDuration = 0.12f;
                var slowMultiplier = isPlayer ? 0.85f : 0.6f;
                var slowDuration = isPlayer ? 0.18f : 0.28f;
                var knockbackDuration = 0.12f;
                var knockbackStrength = isPlayer ? 0f : 200f;
                var knockbackVelocity = knockbackStrength > 0f ? LimitMagnitude(direction * knockbackStrength, 240f) : Vector2.Zero;

                ApplyFlash(world, entity, flashDuration);
                ApplySlow(world, entity, slowMultiplier, slowDuration);
                if (knockbackStrength > 0f)
                {
                    ApplyKnockback(world, entity, knockbackVelocity, knockbackDuration);
                }

                if (isPlayer)
                {
                    // Player take-damage clip is 15 frames at 10 FPS (~1.5s). Give it a small buffer so it
                    // visibly completes before we revert to movement/idle.
                    const float playerHitDurationSeconds = 1.5f;
                    ApplyPlayerHitAnimation(world, entity, durationSeconds: playerHitDurationSeconds);
                }

                SpawnDamageNumber(world, position.Value, damage, targetFaction);

                world.RemoveComponent<DamageEvent>(entity);
            });
    }

    private static void ApplyFlash(EcsWorld world, Entity entity, float duration)
    {
        if (world.TryGetComponent(entity, out HitFlash flash))
        {
            flash.RemainingSeconds = MathF.Max(flash.RemainingSeconds, duration);
            world.SetComponent(entity, flash);
        }
        else
        {
            world.SetComponent(entity, new HitFlash(duration));
        }
    }

    private static void ApplySlow(EcsWorld world, Entity entity, float multiplier, float duration)
    {
        multiplier = MathHelper.Clamp(multiplier, 0.2f, 1f);
        if (world.TryGetComponent(entity, out HitSlow slow))
        {
            slow.Multiplier = MathF.Min(slow.Multiplier, multiplier);
            slow.RemainingSeconds = MathF.Max(slow.RemainingSeconds, duration);
            world.SetComponent(entity, slow);
        }
        else
        {
            world.SetComponent(entity, new HitSlow(multiplier, duration));
        }
    }

    private static void ApplyKnockback(EcsWorld world, Entity entity, Vector2 velocity, float duration)
    {
        if (world.TryGetComponent(entity, out HitKnockback knockback))
        {
            if (knockback.Velocity.LengthSquared() < velocity.LengthSquared())
            {
                knockback.Velocity = velocity;
            }

            knockback.RemainingSeconds = MathF.Max(knockback.RemainingSeconds, duration);
            world.SetComponent(entity, knockback);
        }
        else
        {
            world.SetComponent(entity, new HitKnockback(velocity, duration));
        }
    }

    private static Vector2 LimitMagnitude(Vector2 vector, float maxLength)
    {
        var lengthSquared = vector.LengthSquared();
        if (lengthSquared <= maxLength * maxLength)
        {
            return vector;
        }

        var length = MathF.Sqrt(lengthSquared);
        return vector / length * maxLength;
    }

    private void SpawnDamageNumber(EcsWorld world, Vector2 position, DamageEvent damage, Faction targetFaction)
    {
        if (damage.Amount <= 0f || targetFaction == Faction.Player)
        {
            return;
        }

        var numberEntity = world.CreateEntity();
        var lifetimeSeconds = 0.9f;
        var floatSpeed = 22f;
        var horizontalJitter = (_random.NextSingle() - 0.5f) * 14f;
        var scale = targetFaction == Faction.Player ? 0.55f : 0.6f;
        var color = damage.SourceFaction == Faction.Player ? Color.Gold : Color.Crimson;
        var spawnOffset = new Vector2(horizontalJitter * 0.5f, -18f);

        world.SetComponent(numberEntity, new Position(position + spawnOffset));
        world.SetComponent(numberEntity, new DamageNumber(damage.Amount, lifetimeSeconds, floatSpeed, horizontalJitter, scale, color));
        world.SetComponent(numberEntity, new Lifetime(lifetimeSeconds));
    }

    private static void ApplyPlayerHitAnimation(EcsWorld world, Entity entity, float durationSeconds)
    {
        // Restart the hit window so the animation always plays from the start.
        world.SetComponent(entity, new PlayerHitState(durationSeconds, durationSeconds));

        if (world.TryGetComponent(entity, out PlayerAnimationState anim))
        {
            if (anim.ActiveClip != PlayerAnimationClip.Hit)
            {
                anim.ActiveClip = PlayerAnimationClip.Hit;
                anim.FrameIndex = 0;
                anim.Timer = 0f;
                world.SetComponent(entity, anim);
            }
        }
    }
}

