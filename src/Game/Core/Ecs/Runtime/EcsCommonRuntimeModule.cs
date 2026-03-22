using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Ecs.Systems.Collision;
using TheLastMageStanding.Game.Core.Rendering.UI;

namespace TheLastMageStanding.Game.Core.Ecs.Runtime;

internal sealed class EcsCommonRuntimeModule : IEcsRuntimeModule
{
    public EcsRuntimeModuleDefinition Definition { get; } = new(nameof(EcsCommonRuntimeModule));

    public void Register(EcsRuntimeRegistration registration, EcsRuntimeModuleContext context)
    {
        registration.RequireCapability(EcsRuntimeCapability.SessionEntity, nameof(EcsCommonRuntimeModule));
        registration.ProvideCapability(
            EcsRuntimeCapability.SessionSettingsState,
            $"{nameof(EcsCommonRuntimeModule)}.{nameof(SettingsMenuSystem)}");

        var playerAnimationSystem = new PlayerAnimationSystem();
        var playerRenderSystem = new PlayerRenderSystem();
        var inventoryUiSystem = new InventoryUiSystem();
        var perkTreeUiSystem = new PerkTreeUISystem(context.PerkTreeConfig, context.PerkService);
        var hudRenderSystem = new HudRenderSystem();
        var skillHotbarRenderer = new SkillHotbarRenderer(context.SkillRegistry);
        var equippedSkillsSyncSystem = new EquippedSkillsProfileSyncSystem(context.SceneStateService, context.MetaProgressionManager);
        var levelUpChoiceSystem = new LevelUpChoiceSystem(context.LevelUpChoiceGenerator);

        registration.Update.Add(EcsSceneScope.Common, new InputSystem());
        registration.Update.Add(EcsSceneScope.Common, equippedSkillsSyncSystem);
        registration.Update.Add(EcsSceneScope.Common, levelUpChoiceSystem);
        registration.Update.Add(EcsSceneScope.Common, new MovementIntentSystem());
        registration.Update.Add(EcsSceneScope.Common, new MovementSystem());
        registration.Update.Add(EcsSceneScope.Common, new CameraFollowSystem());
        registration.Update.Add(EcsSceneScope.Common, new CollisionResolutionSystem());
        registration.Update.Add(EcsSceneScope.Common, new CollisionSystem());
        registration.Update.Add(EcsSceneScope.Common, context.SfxSystem);
        registration.Update.Add(EcsSceneScope.Common, playerAnimationSystem);
        registration.Update.Add(EcsSceneScope.Common, new PerkAutoSaveSystem(context.SlotPersistence.PerkPersistence));
        registration.Update.Add(EcsSceneScope.Common, perkTreeUiSystem);
        registration.Update.Add(EcsSceneScope.Common, inventoryUiSystem);
        registration.Update.Add(EcsSceneScope.Common, new CleanupSystem());
        registration.Update.Add(EcsSceneScope.Common, new IntentResetSystem());

        registration.Draw.Add(EcsSceneScope.Common, playerRenderSystem);

        registration.UiDraw.Add(EcsSceneScope.Common, hudRenderSystem);
        registration.UiDraw.Add(EcsSceneScope.Common, skillHotbarRenderer);
        registration.UiDraw.Add(EcsSceneScope.Common, perkTreeUiSystem);

        registration.ScreenSpaceUiDraw.Add(EcsSceneScope.Common, inventoryUiSystem);

        RegisterLoadContent(registration, context.SfxSystem, playerRenderSystem, inventoryUiSystem, perkTreeUiSystem, hudRenderSystem, skillHotbarRenderer);
    }

    private static void RegisterLoadContent(EcsRuntimeRegistration registration, params ILoadContentSystem[] systems)
    {
        foreach (var system in systems)
        {
            registration.LoadContent.Add(system);
        }
    }
}
