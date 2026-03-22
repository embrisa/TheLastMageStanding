using TheLastMageStanding.Game.Core.Ecs.Systems;

namespace TheLastMageStanding.Game.Core.Ecs.Runtime;

internal sealed class EcsStageRuntimeModule : IEcsRuntimeModule
{
    public EcsRuntimeModuleDefinition Definition { get; } = new(
        nameof(EcsStageRuntimeModule),
        nameof(EcsCommonRuntimeModule));

    public void Register(EcsRuntimeRegistration registration, EcsRuntimeModuleContext context)
    {
        registration.RequireCapability(EcsRuntimeCapability.SessionEntity, nameof(EcsStageRuntimeModule));

        var enemyRenderSystem = new EnemyRenderSystem();
        var enemyAnimationSystem = new EnemyAnimationSystem(enemyRenderSystem);
        var damageNumberLifecycleSystem = new DamageNumberLifecycleSystem();
        var damageNumberRenderSystem = new DamageNumberRenderSystem();
        var projectileRenderSystem = new ProjectileRenderSystem();
        var xpOrbRenderSystem = new XpOrbRenderSystem();
        var telegraphRenderSystem = new TelegraphRenderSystem();
        var pauseMenuUiSystem = new PauseMenuMyraSystem(context.SceneStateService);
        var levelUpChoiceUiSystem = new LevelUpChoiceMyraSystem(context.SceneStateService);
        var vfxSystem = new VfxSystem();
        var hitEffectSystem = new HitEffectSystem();
        var telegraphSystem = new TelegraphSystem();

        registration.ProvideCapability(
            EcsRuntimeCapability.SessionSettingsState,
            $"{nameof(EcsStageRuntimeModule)}.{nameof(SettingsMenuSystem)}");
        registration.ProvideCapability(
            EcsRuntimeCapability.StageRunState,
            $"{nameof(EcsStageRuntimeModule)}.{nameof(StageRunInitializationSystem)}");

        registration.StageSessionUpdate.Add(EcsSceneScope.Stage, context.SettingsMenuSystem);
        registration.StageSessionUpdate.Add(EcsSceneScope.Stage, context.SessionStateSystem);
        registration.StageSessionUpdate.Add(EcsSceneScope.Stage, context.SessionNotificationSystem);
        registration.RequireCapability(
            EcsRuntimeCapability.SessionSettingsState,
            $"{nameof(EcsStageRuntimeModule)}.{nameof(PauseMenuSystem)}");
        registration.StageSessionUpdate.Add(EcsSceneScope.Stage, context.PauseMenuSystem);

        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new StageRunInitializationSystem(context.SceneStateService, context.StageRegistry));
        registration.RequireCapability(
            EcsRuntimeCapability.StageRunState,
            $"{nameof(EcsStageRuntimeModule)}.{nameof(StageCompletionSystem)}");
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new StageCompletionSystem(context.SceneManager, context.SceneStateService, context.CampaignProgressionService));
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new DashInputSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new DashExecutionSystem(context.HitStopSystem));
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new DashMovementSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new TheLastMageStanding.Game.Core.Skills.PlayerSkillInputSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new TheLastMageStanding.Game.Core.Skills.SkillCastSystem(context.SkillRegistry));
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new TheLastMageStanding.Game.Core.Skills.SkillExecutionSystem(context.SkillRegistry));
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new HitReactionSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new StatusEffectApplicationSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new StatusEffectTickSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new EliteModifierSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new StatRecalculationSystem());
        registration.RequireCapability(
            EcsRuntimeCapability.StageRunState,
            $"{nameof(EcsStageRuntimeModule)}.{nameof(WaveSchedulerSystem)}");
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new WaveSchedulerSystem(context.WaveConfig));
        registration.RequireCapability(
            EcsRuntimeCapability.StageRunState,
            $"{nameof(EcsStageRuntimeModule)}.{nameof(SpawnSystem)}");
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new SpawnSystem(context.EnemyFactory));
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new BossPhaseSystem(context.StageRegistry));
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new AiSeekSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new RangedAttackSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new AiChargerSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new AiProtectorSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new AiBufferSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new BuffTickSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new ProjectileUpdateSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new KnockbackSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new DynamicSeparationSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new ContactDamageSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new MeleeHitSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new ProjectileHitSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new AnimationEventSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new CombatSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, vfxSystem);
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new StatusEffectVfxSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, telegraphSystem);
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, hitEffectSystem);
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new XpOrbSpawnSystem(context.ProgressionConfig));
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new XpCollectionSystem(context.ProgressionConfig));
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new LootDropSystem(context.ItemFactory, context.LootConfig));
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new LootPickupSystem());
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new LevelUpSystem(context.LevelUpChoiceGenerator));
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new PerkPointGrantSystem(context.PerkTreeConfig));
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, new PerkEffectApplicationSystem(context.PerkService));
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, enemyAnimationSystem);
        registration.StageGameplayUpdate.Add(EcsSceneScope.Stage, damageNumberLifecycleSystem);

        registration.StagePreGameplayUpdate.Add(EcsSceneScope.Stage, context.HitStopSystem);
        registration.StageHitStopFeedbackUpdate.Add(EcsSceneScope.Stage, vfxSystem);
        registration.StageHitStopFeedbackUpdate.Add(EcsSceneScope.Stage, hitEffectSystem);
        registration.StageHitStopFeedbackUpdate.Add(EcsSceneScope.Stage, telegraphSystem);

        registration.Draw.Add(EcsSceneScope.Stage, enemyRenderSystem);
        registration.Draw.Add(EcsSceneScope.Stage, projectileRenderSystem);
        registration.Draw.Add(EcsSceneScope.Stage, telegraphRenderSystem);
        registration.Draw.Add(EcsSceneScope.Stage, damageNumberRenderSystem);
        registration.Draw.Add(EcsSceneScope.Stage, xpOrbRenderSystem);

        registration.ScreenSpaceUiDraw.Add(EcsSceneScope.Stage, pauseMenuUiSystem);
        registration.ScreenSpaceUiDraw.Add(EcsSceneScope.Stage, levelUpChoiceUiSystem);

        RegisterLoadContent(registration, enemyRenderSystem, projectileRenderSystem, xpOrbRenderSystem, damageNumberRenderSystem, pauseMenuUiSystem, levelUpChoiceUiSystem);
    }

    private static void RegisterLoadContent(EcsRuntimeRegistration registration, params ILoadContentSystem[] systems)
    {
        foreach (var system in systems)
        {
            registration.LoadContent.Add(system);
        }
    }
}
