using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class DashExecutionSystem : IUpdateSystem
{
    private readonly HitStopSystem _hitStopSystem;
    private EcsWorld _world = null!;

    public DashExecutionSystem(HitStopSystem hitStopSystem)
    {
        _hitStopSystem = hitStopSystem;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<DashRequestEvent>(OnDashRequested);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Event-driven
    }

    private void OnDashRequested(DashRequestEvent evt)
    {
        if (!_world.IsAlive(evt.Actor))
            return;

        if (!_world.TryGetComponent(evt.Actor, out DashConfig config))
            return;

        if (_world.TryGetComponent(evt.Actor, out DashState dashState) && dashState.IsActive)
            return;

        var cooldown = _world.TryGetComponent(evt.Actor, out DashCooldown dashCooldown)
            ? dashCooldown
            : new DashCooldown(0f);

        if (!cooldown.IsReady)
            return;

        var direction = evt.Direction;
        if (direction.LengthSquared() < 0.0001f)
        {
            direction = Vector2.UnitX;
        }
        else
        {
            direction.Normalize();
        }

        var dashSpeed = config.Distance / MathF.Max(config.Duration, 0.0001f);
        dashState = new DashState
        {
            IsActive = true,
            Elapsed = 0f,
            Direction = direction,
            IFrameActive = true
        };
        _world.SetComponent(evt.Actor, dashState);

        if (_world.TryGetComponent(evt.Actor, out Velocity velocity))
        {
            velocity.Value = direction * dashSpeed;
            _world.SetComponent(evt.Actor, velocity);
        }
        else
        {
            _world.SetComponent(evt.Actor, new Velocity(direction * dashSpeed));
        }

        cooldown.RemainingSeconds = config.Cooldown;
        _world.SetComponent(evt.Actor, cooldown);
        _world.SetComponent(evt.Actor, new Invulnerable());

        if (_world.TryGetComponent(evt.Actor, out Position position))
        {
            _world.EventBus.Publish(new VfxSpawnEvent("dash_start", position.Value, VfxType.DashTrail));
            _world.EventBus.Publish(new SfxPlayEvent("DashStart", SfxCategory.Ability, position.Value, 0.9f));
            _hitStopSystem.TriggerCameraNudge(2.5f, 0.1f);
        }
    }
}



