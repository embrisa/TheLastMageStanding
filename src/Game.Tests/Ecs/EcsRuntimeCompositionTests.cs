using System;
using System.IO;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Ecs.Runtime;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Loot;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.Perks;
using TheLastMageStanding.Game.Core.Progression;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.Skills;
using Xunit;

namespace TheLastMageStanding.Game.Tests.Ecs;

public sealed class EcsRuntimeCompositionTests
{
    [Fact]
    public void Compose_ReordersRealModules_ToSatisfyHubAndStageDependencies()
    {
        var context = CreateContext();

        var registration = EcsRuntimeComposer.Compose(
            context,
            [
                new EcsStageRuntimeModule(),
                new EcsHubRuntimeModule(),
                new EcsCommonRuntimeModule(),
                new EcsDebugRuntimeModule(),
            ]);
        var moduleOrder = registration.ModuleOrder.ToArray();

        Assert.True(
            Array.IndexOf(moduleOrder, nameof(EcsCommonRuntimeModule)) <
            Array.IndexOf(moduleOrder, nameof(EcsHubRuntimeModule)));
        Assert.True(
            Array.IndexOf(moduleOrder, nameof(EcsCommonRuntimeModule)) <
            Array.IndexOf(moduleOrder, nameof(EcsStageRuntimeModule)));
        Assert.Equal(nameof(EcsWorldRunner), registration.CapabilityProviders[EcsRuntimeCapability.SessionEntity]);
        Assert.Equal(
            $"{nameof(EcsStageRuntimeModule)}.{nameof(SettingsMenuSystem)}",
            registration.CapabilityProviders[EcsRuntimeCapability.SessionSettingsState]);
        Assert.Equal(
            $"{nameof(EcsStageRuntimeModule)}.{nameof(StageRunInitializationSystem)}",
            registration.CapabilityProviders[EcsRuntimeCapability.StageRunState]);
    }

    [Fact]
    public void Compose_HubModuleWithoutCommonModule_FailsFast()
    {
        var context = CreateContext();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            EcsRuntimeComposer.Compose(context, [new EcsHubRuntimeModule()]));

        Assert.Contains(nameof(EcsHubRuntimeModule), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(EcsCommonRuntimeModule), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_StageModuleWithoutCommonModule_FailsFast()
    {
        var context = CreateContext();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            EcsRuntimeComposer.Compose(context, [new EcsStageRuntimeModule()]));

        Assert.Contains(nameof(EcsStageRuntimeModule), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(EcsCommonRuntimeModule), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_ModuleThatConsumesMissingCapability_FailsFast()
    {
        var context = CreateContext();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            EcsRuntimeComposer.Compose(
                context,
                [
                    new EcsCommonRuntimeModule(),
                    new MissingSettingsConsumerModule(),
                ]));

        Assert.Contains(nameof(EcsRuntimeCapability.SessionSettingsState), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(MissingSettingsConsumerModule), exception.Message, StringComparison.Ordinal);
    }

    private static EcsRuntimeModuleContext CreateContext()
    {
        var world = new EcsWorld();
        var eventBus = new EventBus();
        world.EventBus = eventBus;

        var camera = new Camera2D(960, 540);
        var sceneStateService = new SceneStateService();
        var sceneManager = new SceneManager(sceneStateService, eventBus);
        var stageRegistry = new StageRegistry();
        var progressionConfig = ProgressionConfig.Default;
        var waveConfig = EnemyWaveConfig.Default;
        var audioSettings = AudioSettingsConfig.Default;
        var audioSettingsStore = new AudioSettingsStore(Path.Combine(CreateTempDirectory(), "audio-settings.json"));
        var videoSettings = VideoSettingsConfig.Default;
        var videoSettingsStore = new VideoSettingsStore(Path.Combine(CreateTempDirectory(), "video-settings.json"));
        var inputBindings = InputBindingsConfig.Default.Clone();
        var inputBindingsStore = new InputBindingsStore(Path.Combine(CreateTempDirectory(), "input-bindings.json"));
        var musicService = new MusicService(audioSettings);
        var persistenceRoot = new PersistenceRoot(new DefaultFileSystem(), CreateTempDirectory());
        var slotPersistence = persistenceRoot.ForSlot("test-slot");
        var metaProgressionManager = new MetaProgressionManager(eventBus, slotPersistence);
        var campaignProgressionService = new CampaignProgressionService(stageRegistry, slotPersistence.PlayerProfile);
        var perkTreeConfig = PerkTreeConfig.Default;
        var perkService = new PerkService(perkTreeConfig);
        var lootConfig = LootDropConfig.CreateDefault();
        var itemRegistry = new ItemRegistry();
        var itemFactory = new ItemFactory(itemRegistry.GetAllDefinitions(), lootConfig);
        var skillRegistry = new SkillRegistry();
        var levelUpChoiceGenerator = new LevelUpChoiceGenerator(LevelUpChoiceConfig.Default, skillRegistry);
        var playerFactory = new PlayerEntityFactory(world, progressionConfig);
        var enemyFactory = new EnemyEntityFactory(world);
        var sessionStateSystem = new SessionStateSystem(
            new StageRunEntityCleanupService(),
            new StageRunResetService(playerFactory));
        var pauseMenuSystem = new PauseMenuSystem();
        var settingsMenuSystem = new SettingsMenuSystem(new RuntimeSettingsService(
            audioSettings,
            audioSettingsStore,
            videoSettings,
            videoSettingsStore,
            inputBindings,
            inputBindingsStore,
            musicService));
        var sessionNotificationSystem = new SessionNotificationSystem();
        var hitStopSystem = new HitStopSystem();
        var sfxSystem = new SfxSystem(audioSettings);

        return new EcsRuntimeModuleContext(
            world,
            eventBus,
            camera,
            sceneStateService,
            sceneManager,
            stageRegistry,
            playerFactory,
            enemyFactory,
            waveConfig,
            progressionConfig,
            audioSettings,
            audioSettingsStore,
            videoSettings,
            videoSettingsStore,
            inputBindings,
            inputBindingsStore,
            musicService,
            slotPersistence,
            metaProgressionManager,
            campaignProgressionService,
            perkTreeConfig,
            perkService,
            lootConfig,
            itemFactory,
            skillRegistry,
            levelUpChoiceGenerator,
            sessionStateSystem,
            pauseMenuSystem,
            settingsMenuSystem,
            sessionNotificationSystem,
            hitStopSystem,
            sfxSystem);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class MissingSettingsConsumerModule : IEcsRuntimeModule
    {
        public EcsRuntimeModuleDefinition Definition { get; } = new(
            nameof(MissingSettingsConsumerModule),
            nameof(EcsCommonRuntimeModule));

        public void Register(EcsRuntimeRegistration registration, EcsRuntimeModuleContext context)
        {
            registration.RequireCapability(
                EcsRuntimeCapability.SessionSettingsState,
                nameof(MissingSettingsConsumerModule));
        }
    }
}
