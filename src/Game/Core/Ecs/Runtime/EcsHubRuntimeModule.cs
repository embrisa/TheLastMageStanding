using TheLastMageStanding.Game.Core.Ecs.Systems;

namespace TheLastMageStanding.Game.Core.Ecs.Runtime;

internal sealed class EcsHubRuntimeModule : IEcsRuntimeModule
{
    public EcsRuntimeModuleDefinition Definition { get; } = new(
        nameof(EcsHubRuntimeModule),
        nameof(EcsCommonRuntimeModule));

    public void Register(EcsRuntimeRegistration registration, EcsRuntimeModuleContext context)
    {
        registration.RequireCapability(EcsRuntimeCapability.SessionEntity, nameof(EcsHubRuntimeModule));

        var stageSelectionUi = new StageSelectionUISystem(context.StageRegistry, context.SceneManager, context.CampaignProgressionService);
        var skillSelectionUi = new SkillSelectionUISystem(context.SceneStateService, context.MetaProgressionManager, context.SkillRegistry);
        var runHistoryUi = new RunHistoryUISystem(context.MetaProgressionManager.HistoryService, context.SceneStateService);
        var hubMenuSystem = new HubMenuSystem(context.SceneStateService);
        var npcRenderSystem = new NpcRenderSystem();
        var proximityPromptRenderSystem = new ProximityPromptRenderSystem();

        registration.Update.Add(EcsSceneScope.Hub, stageSelectionUi);
        registration.Update.Add(EcsSceneScope.Hub, skillSelectionUi);
        registration.Update.Add(EcsSceneScope.Hub, runHistoryUi);
        registration.Update.Add(EcsSceneScope.Hub, new ProximityInteractionSystem());
        registration.Update.Add(EcsSceneScope.Hub, new InteractionInputSystem());
        registration.Update.Add(EcsSceneScope.Hub, hubMenuSystem);

        registration.Draw.Add(EcsSceneScope.Hub, npcRenderSystem);
        registration.Draw.Add(EcsSceneScope.Hub, proximityPromptRenderSystem);

        registration.UiDraw.Add(EcsSceneScope.Hub, hubMenuSystem);

        registration.ScreenSpaceUiDraw.Add(EcsSceneScope.Hub, stageSelectionUi);
        registration.ScreenSpaceUiDraw.Add(EcsSceneScope.Hub, skillSelectionUi);
        registration.ScreenSpaceUiDraw.Add(EcsSceneScope.Hub, runHistoryUi);

        RegisterLoadContent(registration, stageSelectionUi, skillSelectionUi, runHistoryUi, hubMenuSystem, npcRenderSystem, proximityPromptRenderSystem);
    }

    private static void RegisterLoadContent(EcsRuntimeRegistration registration, params ILoadContentSystem[] systems)
    {
        foreach (var system in systems)
        {
            registration.LoadContent.Add(system);
        }
    }
}
