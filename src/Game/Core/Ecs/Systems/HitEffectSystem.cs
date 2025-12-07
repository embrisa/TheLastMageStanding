using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Applies and decays hit reactions before movement is integrated.
/// </summary>
internal sealed class HitEffectSystem : IUpdateSystem
{
    private EcsWorld _world = null!;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
    }

    private void OnEntityDamaged(EntityDamagedEvent evt)
    {
        var flashDuration = 0.12f;
        ApplyFlash(_world, evt.Target, flashDuration);
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

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;

        world.ForEach<HitFlash>(
            (Entity entity, ref HitFlash flash) =>
            {
                flash.RemainingSeconds -= deltaSeconds;
                if (flash.RemainingSeconds <= 0f)
                {
                    world.RemoveComponent<HitFlash>(entity);
                }
                else
                {
                    world.SetComponent(entity, flash);
                }
            });

        world.ForEach<HitSlow, Velocity>(
            (Entity entity, ref HitSlow slow, ref Velocity velocity) =>
            {
                var clamped = MathHelper.Clamp(slow.Multiplier, 0.1f, 1f);
                velocity.Value *= clamped;
                world.SetComponent(entity, velocity);

                slow.RemainingSeconds -= deltaSeconds;
                if (slow.RemainingSeconds <= 0f)
                {
                    world.RemoveComponent<HitSlow>(entity);
                }
                else
                {
                    world.SetComponent(entity, slow);
                }
            });

        world.ForEach<HitKnockback, Velocity>(
            (Entity entity, ref HitKnockback knockback, ref Velocity velocity) =>
            {
                velocity.Value += knockback.Velocity;
                world.SetComponent(entity, velocity);

                knockback.Velocity = Vector2.Lerp(knockback.Velocity, Vector2.Zero, deltaSeconds * 10f);
                knockback.RemainingSeconds -= deltaSeconds;

                if (knockback.RemainingSeconds <= 0f || knockback.Velocity.LengthSquared() < 0.25f)
                {
                    world.RemoveComponent<HitKnockback>(entity);
                }
                else
                {
                    world.SetComponent(entity, knockback);
                }
            });
    }
}

