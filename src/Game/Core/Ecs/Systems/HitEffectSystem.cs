using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Applies and decays hit reactions before movement is integrated.
/// </summary>
internal sealed class HitEffectSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
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

