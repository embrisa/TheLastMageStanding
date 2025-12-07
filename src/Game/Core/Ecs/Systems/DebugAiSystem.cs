using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class DebugAiSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;
        // Simple heartbeat to exercise AI stage: recharge attack timers and wiggle debug movers.
        world.ForEach<AttackStats>(
            (Entity entity, ref AttackStats attack) =>
            {
                attack.CooldownTimer = MathF.Max(0f, attack.CooldownTimer - deltaSeconds);
                if (attack.CooldownTimer <= 0f)
                {
                    attack.CooldownTimer = attack.CooldownSeconds;
                }
            });
    }
}

