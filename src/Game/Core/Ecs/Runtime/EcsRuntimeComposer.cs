namespace TheLastMageStanding.Game.Core.Ecs.Runtime;

internal static class EcsRuntimeComposer
{
    public static EcsRuntimeRegistration Compose(EcsRuntimeModuleContext context)
    {
        var registration = new EcsRuntimeRegistration();
        IEcsRuntimeModule[] modules =
        [
            new EcsDebugRuntimeModule(),
            new EcsCommonRuntimeModule(),
            new EcsHubRuntimeModule(),
            new EcsStageRuntimeModule(),
        ];

        foreach (var module in modules)
        {
            module.Register(registration, context);
        }

        return registration;
    }
}
