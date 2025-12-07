using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Converts damage events into health reduction and short-lived combat reactions (slow, knockback).
/// </summary>
internal sealed class HitReactionSystem : IUpdateSystem
{
    private EcsWorld _world = null!;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
    }

    private void OnEntityDamaged(EntityDamagedEvent evt)
    {
        var entity = evt.Target;

        if (_world.TryGetComponent(entity, out Health health))
        {
            var wasAlive = !health.IsDead;
            health.Current = MathF.Max(0f, health.Current - evt.Amount);
            _world.SetComponent(entity, health);

            if (wasAlive && health.IsDead)
            {
                var currentFaction = _world.TryGetComponent(entity, out Faction f) ? f : Faction.Neutral;
                if (currentFaction == Faction.Player)
                {
                    _world.EventBus.Publish(new PlayerDiedEvent(entity));
                }
            }
        }

        if (!_world.TryGetComponent(entity, out Position position))
        {
            return;
        }

        var direction = position.Value - evt.SourcePosition;
        if (direction.LengthSquared() < 0.0001f)
        {
            direction = new Vector2(0f, -1f);
        }
        else
        {
            direction = Vector2.Normalize(direction);
        }

        var targetFaction = _world.TryGetComponent(entity, out Faction faction) ? faction : Faction.Neutral;
        var isPlayer = targetFaction == Faction.Player;

        var slowMultiplier = isPlayer ? 0.85f : 0.6f;
        var slowDuration = isPlayer ? 0.18f : 0.28f;
        var knockbackDuration = 0.12f;
        var knockbackStrength = isPlayer ? 0f : 200f;
        var knockbackVelocity = knockbackStrength > 0f ? LimitMagnitude(direction * knockbackStrength, 240f) : Vector2.Zero;

        ApplySlow(_world, entity, slowMultiplier, slowDuration);
        if (knockbackStrength > 0f)
        {
            ApplyKnockback(_world, entity, knockbackVelocity, knockbackDuration);
        }

        if (isPlayer)
        {
            // Player take-damage clip is 15 frames at 10 FPS (~1.5s). Give it a small buffer so it
            // visibly completes before we revert to movement/idle.
            const float playerHitDurationSeconds = 1.5f;
            ApplyPlayerHitAnimation(_world, entity, durationSeconds: playerHitDurationSeconds);
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

    private static void ApplyPlayerHitAnimation(EcsWorld world, Entity entity, float durationSeconds)
    {
        // Restart the hit window so the animation always plays from the start.
        world.SetComponent(entity, new PlayerHitState(durationSeconds, durationSeconds));

        if (world.TryGetComponent(entity, out PlayerAnimationState anim))
        {
            // Update facing based on current input/velocity to ensure hit plays in correct direction.
            var movement = Vector2.Zero;
            if (world.TryGetComponent(entity, out InputIntent intent))
            {
                movement = intent.Movement;
            }
            else if (world.TryGetComponent(entity, out Velocity velocity))
            {
                movement = velocity.Value;
            }

            if (movement.LengthSquared() > 0.0001f)
            {
                anim.Facing = ToFacing(movement);
            }

            if (anim.ActiveClip != PlayerAnimationClip.Hit)
            {
                anim.ActiveClip = PlayerAnimationClip.Hit;
                anim.FrameIndex = 0;
                anim.Timer = 0f;
            }
            world.SetComponent(entity, anim);
        }
    }

    private static PlayerFacingDirection ToFacing(Vector2 movement)
    {
        const float dead = 0.0001f;
        if (movement.LengthSquared() <= dead)
        {
            return PlayerFacingDirection.South;
        }

        var direction = Vector2.Normalize(movement);
        var angle = MathF.Atan2(direction.Y, direction.X); // y > 0 is down in screen space
        if (angle < 0f)
        {
            angle += MathF.Tau;
        }

        const float octantSize = MathF.PI / 4f; // 45 degrees per facing slice
        var octant = (int)MathF.Floor((angle + (octantSize * 0.5f)) / octantSize) % 8;

        return octant switch
        {
            0 => PlayerFacingDirection.East,
            1 => PlayerFacingDirection.SouthEast,
            2 => PlayerFacingDirection.South,
            3 => PlayerFacingDirection.SouthWest,
            4 => PlayerFacingDirection.West,
            5 => PlayerFacingDirection.NorthWest,
            6 => PlayerFacingDirection.North,
            _ => PlayerFacingDirection.NorthEast,
        };
    }
}
