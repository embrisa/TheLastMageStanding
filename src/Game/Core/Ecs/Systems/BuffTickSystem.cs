using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Counts down durations on timed buffs and removes expired entries.
/// </summary>
internal sealed class BuffTickSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;

        world.ForEach<ActiveBuffs>(
            (Entity entity, ref ActiveBuffs active) =>
            {
                if (active.Buffs == null || active.Buffs.Count == 0)
                {
                    world.RemoveComponent<ActiveBuffs>(entity);
                    MarkComputedStatsDirty(world, entity);
                    return;
                }

                var changed = false;
                for (var i = active.Buffs.Count - 1; i >= 0; i--)
                {
                    var buff = active.Buffs[i];
                    buff.RemainingDuration -= deltaSeconds;
                    if (buff.IsExpired)
                    {
                        active.Buffs.RemoveAt(i);
                        changed = true;
                        continue;
                    }

                    active.Buffs[i] = buff;
                }

                if (active.Buffs.Count == 0)
                {
                    world.RemoveComponent<ActiveBuffs>(entity);
                    MarkComputedStatsDirty(world, entity);
                    return;
                }

                if (changed)
                {
                    world.SetComponent(entity, active);
                    MarkComputedStatsDirty(world, entity);
                }
            });
    }

    private static void MarkComputedStatsDirty(EcsWorld world, Entity entity)
    {
        if (world.TryGetComponent(entity, out ComputedStats stats))
        {
            ComputedStats.MarkDirty(ref stats);
            world.SetComponent(entity, stats);
        }
    }
}

