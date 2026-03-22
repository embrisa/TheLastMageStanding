using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Ecs.Systems.Collision;

namespace TheLastMageStanding.Game.Core.Ecs.Runtime;

internal sealed class EcsDebugRuntimeModule : IEcsRuntimeModule
{
    public void Register(EcsRuntimeRegistration registration, EcsRuntimeModuleContext context)
    {
        var collisionDebugRenderSystem = new CollisionDebugRenderSystem();
        var statusEffectDebugSystem = new StatusEffectDebugSystem();
        var aiDebugRenderSystem = new AiDebugRenderSystem();
        var renderDebugSystem = new RenderDebugSystem();

        registration.Update.Add(
            EcsSceneScope.Common,
            new DebugInputSystem(collisionDebugRenderSystem, context.EnemyFactory, statusEffectDebugSystem, aiDebugRenderSystem));
        registration.Update.Add(EcsSceneScope.Common, new DebugCommandSystem());

        registration.Draw.Add(EcsSceneScope.Stage, renderDebugSystem);
        registration.Draw.Add(EcsSceneScope.Stage, statusEffectDebugSystem);
        registration.Draw.Add(EcsSceneScope.Stage, aiDebugRenderSystem);
        registration.Draw.Add(EcsSceneScope.Stage, collisionDebugRenderSystem);

        RegisterLoadContent(registration, statusEffectDebugSystem, aiDebugRenderSystem, renderDebugSystem);
    }

    private static void RegisterLoadContent(EcsRuntimeRegistration registration, params ILoadContentSystem[] systems)
    {
        foreach (var system in systems)
        {
            registration.LoadContent.Add(system);
        }
    }
}
