using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Ecs.Systems;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Loot;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.Perks;
using TheLastMageStanding.Game.Core.Progression;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Core.Ecs.Runtime;

internal interface IEcsRuntimeModule
{
    void Register(EcsRuntimeRegistration registration, EcsRuntimeModuleContext context);
}

internal sealed record EcsRuntimeModuleContext(
    EcsWorld World,
    EventBus EventBus,
    Camera2D Camera,
    SceneStateService SceneStateService,
    SceneManager SceneManager,
    StageRegistry StageRegistry,
    PlayerEntityFactory PlayerFactory,
    EnemyEntityFactory EnemyFactory,
    EnemyWaveConfig WaveConfig,
    ProgressionConfig ProgressionConfig,
    AudioSettingsConfig AudioSettings,
    AudioSettingsStore AudioSettingsStore,
    VideoSettingsConfig VideoSettings,
    VideoSettingsStore VideoSettingsStore,
    InputBindingsConfig InputBindings,
    InputBindingsStore InputBindingsStore,
    MusicService MusicService,
    SlotPersistenceScope SlotPersistence,
    MetaProgressionManager MetaProgressionManager,
    CampaignProgressionService CampaignProgressionService,
    PerkTreeConfig PerkTreeConfig,
    PerkService PerkService,
    LootDropConfig LootConfig,
    ItemFactory ItemFactory,
    SkillRegistry SkillRegistry,
    LevelUpChoiceGenerator LevelUpChoiceGenerator,
    SessionStateSystem SessionStateSystem,
    PauseMenuSystem PauseMenuSystem,
    SettingsMenuSystem SettingsMenuSystem,
    SessionNotificationSystem SessionNotificationSystem,
    HitStopSystem HitStopSystem,
    SfxSystem SfxSystem);
