using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class DashInputSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!IsSessionActive(world))
        {
            return;
        }

        var deltaSeconds = context.DeltaSeconds;
        var input = context.Input;

        world.ForEach<PlayerTag, DashConfig, DashCooldown>(
            (Entity entity, ref PlayerTag _, ref DashConfig config, ref DashCooldown cooldown) =>
            {
                var buffer = world.TryGetComponent(entity, out DashInputBuffer existingBuffer)
                    ? existingBuffer
                    : new DashInputBuffer();

                // Cooldown tick
                if (cooldown.RemainingSeconds > 0f)
                {
                    cooldown.RemainingSeconds = MathF.Max(0f, cooldown.RemainingSeconds - deltaSeconds);
                    world.SetComponent(entity, cooldown);
                }

                // Buffer decay
                if (buffer.HasBufferedInput)
                {
                    buffer.TimeRemaining -= deltaSeconds;
                    if (buffer.TimeRemaining <= 0f)
                    {
                        buffer.HasBufferedInput = false;
                        buffer.TimeRemaining = 0f;
                    }
                    world.SetComponent(entity, buffer);
                }

                var dashActive = world.TryGetComponent(entity, out DashState dashState) && dashState.IsActive;

                if (input.DashPressed)
                {
                    if (!TryPublishDash(world, entity, config, cooldown, dashActive))
                    {
                        buffer.Buffer(config.InputBufferWindow);
                        world.SetComponent(entity, buffer);
                    }
                }
                else if (buffer.HasBufferedInput && !dashActive && cooldown.IsReady)
                {
                    if (TryPublishDash(world, entity, config, cooldown, dashActive: false))
                    {
                        buffer.HasBufferedInput = false;
                        buffer.TimeRemaining = 0f;
                    }
                }
                world.SetComponent(entity, buffer);
            });
    }

    private static bool TryPublishDash(EcsWorld world, Entity entity, DashConfig config, DashCooldown cooldown, bool dashActive)
    {
        if (dashActive || !cooldown.IsReady)
        {
            return false;
        }

        var direction = ResolveDashDirection(world, entity);
        world.EventBus.Publish(new DashRequestEvent(entity, direction));
        return true;
    }

    private static Vector2 ResolveDashDirection(EcsWorld world, Entity entity)
    {
        if (world.TryGetComponent(entity, out InputIntent intent) &&
            intent.Movement.LengthSquared() > 0.0001f)
        {
            return Vector2.Normalize(intent.Movement);
        }

        if (world.TryGetComponent(entity, out Velocity velocity) &&
            velocity.Value.LengthSquared() > 0.0001f)
        {
            return Vector2.Normalize(velocity.Value);
        }

        if (world.TryGetComponent(entity, out PlayerAnimationState animState))
        {
            return FacingToVector(animState.Facing);
        }

        return Vector2.UnitX;
    }

    private static Vector2 FacingToVector(PlayerFacingDirection facing)
    {
        return facing switch
        {
            PlayerFacingDirection.South => new Vector2(0f, 1f),
            PlayerFacingDirection.SouthEast => Vector2.Normalize(new Vector2(1f, 1f)),
            PlayerFacingDirection.East => new Vector2(1f, 0f),
            PlayerFacingDirection.NorthEast => Vector2.Normalize(new Vector2(1f, -1f)),
            PlayerFacingDirection.North => new Vector2(0f, -1f),
            PlayerFacingDirection.NorthWest => Vector2.Normalize(new Vector2(-1f, -1f)),
            PlayerFacingDirection.West => new Vector2(-1f, 0f),
            PlayerFacingDirection.SouthWest => Vector2.Normalize(new Vector2(-1f, 1f)),
            _ => Vector2.UnitX
        };
    }

    private static bool IsSessionActive(EcsWorld world)
    {
        var active = false;
        world.ForEach<GameSession>((Entity _, ref GameSession session) =>
        {
            if (session.State == GameState.Playing)
            {
                active = true;
            }
        });
        return active;
    }
}

