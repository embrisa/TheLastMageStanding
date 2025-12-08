using System;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Combat;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// System that recalculates cached stats when base stats or modifiers change.
/// Ensures ComputedStats stays synchronized with stat components.
/// </summary>
internal sealed class StatRecalculationSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
        // No initialization needed
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Recalculate stats for entities with dirty computed stats
        world.ForEach<ComputedStats, OffensiveStats, DefensiveStats>((
            Entity entity,
            ref ComputedStats computed,
            ref OffensiveStats offense,
            ref DefensiveStats defense) =>
        {
            if (!computed.IsDirty)
                return;

            // Must have MoveSpeed component
            if (!world.TryGetComponent(entity, out MoveSpeed moveSpeed))
                return;

            // Get modifiers if they exist
            var modifiers = world.TryGetComponent(entity, out StatModifiers mods)
                ? mods
                : StatModifiers.Zero;

            // Recalculate
            computed = StatCalculator.RecalculateStats(
                in offense,
                in defense,
                moveSpeed.Value,
                in modifiers);

            // Update the move speed with the effective value
            moveSpeed.Value = computed.EffectiveMoveSpeed;
            world.SetComponent(entity, moveSpeed);
        });
    }
}
