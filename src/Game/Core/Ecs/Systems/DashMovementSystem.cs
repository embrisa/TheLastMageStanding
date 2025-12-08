using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class DashMovementSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;

        // Update active dashes
        world.ForEach<DashState, DashConfig, Velocity>(
            (Entity entity, ref DashState dash, ref DashConfig config, ref Velocity velocity) =>
            {
                if (!dash.IsActive)
                    return;

                dash.Elapsed += deltaSeconds;

                var dashSpeed = config.Distance / MathF.Max(config.Duration, 0.0001f);
                velocity.Value = dash.Direction * dashSpeed;

                if (dash.IFrameActive && dash.Elapsed >= config.IFrameWindow)
                {
                    dash.IFrameActive = false;
                    if (world.TryGetComponent(entity, out Invulnerable _))
                    {
                        world.RemoveComponent<Invulnerable>(entity);
                    }
                }

                if (dash.Elapsed >= config.Duration)
                {
                    velocity.Value = Vector2.Zero;
                    world.SetComponent(entity, velocity);
                    world.RemoveComponent<DashState>(entity);
                    if (world.TryGetComponent(entity, out Invulnerable _))
                    {
                        world.RemoveComponent<Invulnerable>(entity);
                    }

                    if (world.TryGetComponent(entity, out Position pos))
                    {
                        world.EventBus.Publish(new VfxSpawnEvent("dash_end", pos.Value, VfxType.DashEnd));
                        world.EventBus.Publish(new SfxPlayEvent("DashEnd", SfxCategory.Ability, pos.Value, 0.7f));
                    }
                }
                else
                {
                    world.SetComponent(entity, dash);
                    world.SetComponent(entity, velocity);
                }
            });
    }
}

