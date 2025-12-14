using System.Globalization;
using System.Text;
using TheLastMageStanding.Game.Core.Combat;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Debug;

/// <summary>
/// Debug helper for inspecting active status effects on an entity.
/// </summary>
internal static class StatusEffectInspector
{
    public static string InspectStatusEffects(EcsWorld world, Entity entity)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"=== Status Effects for Entity {entity.Id} ===");

        if (!world.TryGetComponent(entity, out ActiveStatusEffects active) || active.Effects == null || active.Effects.Count == 0)
        {
            sb.AppendLine("None");
            return sb.ToString();
        }

        foreach (var effect in active.Effects)
        {
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} x{1}", effect.Data.Type, effect.CurrentStacks));
            sb.AppendLine(CultureInfo.InvariantCulture,
                $"  Duration: {effect.RemainingDuration:F2}s / {effect.Data.Duration:F2}s");

            if (effect.Data.TickInterval > 0f)
            {
                var nextTick = MathF.Max(0f, effect.Data.TickInterval - effect.AccumulatedTickTime);
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"  Tick: every {effect.Data.TickInterval:F2}s @ {effect.Data.Potency:F2}/s");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Next Tick: {nextTick:F2}s");
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Potency: {effect.Data.Potency:F2}");
            }
        }

        var defense = GetDefensiveStats(world, entity);
        sb.AppendLine();
        sb.AppendLine("Resistances:");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Fire: {defense.FireResist:F0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Frost: {defense.FrostResist:F0}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  Arcane: {defense.ArcaneResist:F0}");

        return sb.ToString();
    }

    public static StatusEffectData SimulateAppliedEffect(EcsWorld world, Entity target, StatusEffectData effect)
    {
        var defense = GetDefensiveStats(world, target);
        var resistance = DamageCalculator.CalculateStatusEffectResistance(effect.Type, in defense);

        if (world.TryGetComponent(target, out StatusEffectResistances extraResist))
        {
            var extra = Math.Clamp(extraResist.Get(effect.Type), 0f, 1f);
            resistance = 1f - ((1f - resistance) * (1f - extra));
        }

        var adjustedDuration = effect.Duration * (1f - resistance);
        var adjustedPotency = effect.Potency * (1f - resistance);

        return new StatusEffectData(
            effect.Type,
            adjustedPotency,
            adjustedDuration,
            effect.TickInterval,
            effect.MaxStacks,
            effect.InitialStacks);
    }

    private static DefensiveStats GetDefensiveStats(EcsWorld world, Entity target)
    {
        if (world.TryGetComponent(target, out ComputedStats computed))
        {
            return new DefensiveStats
            {
                Armor = computed.EffectiveArmor,
                ArcaneResist = computed.EffectiveArcaneResist,
                FireResist = computed.EffectiveFireResist,
                FrostResist = computed.EffectiveFrostResist
            };
        }

        if (world.TryGetComponent(target, out DefensiveStats defense))
        {
            return defense;
        }

        return DefensiveStats.Default;
    }
}
