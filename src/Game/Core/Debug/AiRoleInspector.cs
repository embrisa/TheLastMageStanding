using System.Globalization;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Debug;

internal static class AiRoleInspector
{
    public static string InspectAiBehavior(EcsWorld world, Entity entity)
    {
        if (!world.TryGetComponent(entity, out AiRoleConfig role) ||
            !world.TryGetComponent(entity, out AiBehaviorStateMachine state))
        {
            return "AI: None";
        }

        var target = state.HasTarget ? state.TargetEntity.Id.ToString(CultureInfo.InvariantCulture) : "none";
        return $"{role.Role} | {state.State} ({state.StateTimer:F2}s) | CD: {state.CooldownTimer:F2}s | Target: {target}";
    }
}



