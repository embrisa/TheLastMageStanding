using System.Globalization;
using System.Text;
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
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"  Tick: every {effect.Data.TickInterval:F2}s @ {effect.Data.Potency:F2}/s");
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Potency: {effect.Data.Potency:F2}");
            }
        }

        return sb.ToString();
    }
}

