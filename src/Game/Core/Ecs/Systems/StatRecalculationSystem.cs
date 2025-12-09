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

            var baseMoveSpeed = moveSpeed.Value;
            if (world.TryGetComponent(entity, out BaseMoveSpeed baseSpeed))
            {
                baseMoveSpeed = baseSpeed.Value;
            }

            // Aggregate modifiers from all sources
            var modifiers = StatModifiers.Zero;

            if (world.TryGetComponent(entity, out PerkModifiers perkMods))
            {
                modifiers = StatModifiers.Combine(modifiers, perkMods.Value);
            }

            if (world.TryGetComponent(entity, out EquipmentModifiers equipMods))
            {
                modifiers = StatModifiers.Combine(modifiers, equipMods.Value);
            }

            if (world.TryGetComponent(entity, out StatusEffectModifiers statusMods))
            {
                modifiers = StatModifiers.Combine(modifiers, statusMods.Value);
            }

            if (world.TryGetComponent(entity, out LevelUpStatModifiers levelUpMods))
            {
                modifiers = StatModifiers.Combine(modifiers, levelUpMods.Value);
            }

            if (world.TryGetComponent(entity, out ActiveBuffs activeBuffs) && activeBuffs.Buffs != null)
            {
                foreach (var buff in activeBuffs.Buffs)
                {
                    modifiers = StatModifiers.Combine(modifiers, buff.Modifiers);
                }
            }

            // Update the aggregate StatModifiers component for other systems to use
            world.SetComponent(entity, modifiers);

            // Recalculate
            computed = StatCalculator.RecalculateStats(
                in offense,
                in defense,
                baseMoveSpeed,
                in modifiers);

            // Update the move speed with the effective value
            moveSpeed.Value = computed.EffectiveMoveSpeed;
            world.SetComponent(entity, moveSpeed);
        });
    }
}
