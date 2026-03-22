using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Diagnostics;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.UI;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.Composition;

internal static class GameRuntimeFactory
{
    public static GameRuntime Create(
        Game1 game,
        GraphicsDeviceManager graphics,
        int virtualWidth,
        int virtualHeight)
    {
        MyraEnvironment.Game = game;

        var camera = new Camera2D(virtualWidth, virtualHeight);
        var audioSettingsStore = new AudioSettingsStore();
        var audioSettings = audioSettingsStore.LoadOrDefault();
        var videoSettingsStore = new VideoSettingsStore();
        var videoSettings = videoSettingsStore.LoadOrDefault();
        var inputBindingsStore = new InputBindingsStore();
        var inputBindings = inputBindingsStore.LoadOrDefault();
        var videoSettingsApplier = new VideoSettingsApplier(graphics, game.Window);
        videoSettingsApplier.Apply(videoSettings, applyChanges: false);

        var musicService = new MusicService(audioSettings);
        RuntimeLog.Configure(new ConsoleRuntimeLogger(RuntimeLogSettings.FromEnvironment()));

        var runtimeSettingsService = new RuntimeSettingsService(
            audioSettings,
            audioSettingsStore,
            videoSettings,
            videoSettingsStore,
            inputBindings,
            inputBindingsStore,
            musicService);

        var eventBus = new EventBus();
        var sceneStateService = new SceneStateService();
        var sceneManager = new SceneManager(sceneStateService, eventBus);
        var stageRegistry = new StageRegistry();
        var input = new InputState(sceneStateService, virtualWidth, virtualHeight, inputBindings);
        var persistenceRoot = new PersistenceRoot(new DefaultFileSystem());
        var saveSlotService = persistenceRoot.CreateSaveSlotService();
        var slotController = new ActiveSaveSlotController(saveSlotService);
        var worldFactory = new EcsWorldFactory(
            camera,
            audioSettings,
            audioSettingsStore,
            videoSettings,
            videoSettingsStore,
            inputBindings,
            inputBindingsStore,
            musicService,
            eventBus,
            stageRegistry,
            sceneStateService,
            sceneManager,
            persistenceRoot);

        var sceneRuntimeService = new SceneRuntimeService(
            camera,
            musicService,
            stageRegistry,
            sceneStateService,
            sceneManager,
            slotController,
            worldFactory);

        return new GameRuntime(
            camera,
            input,
            sceneStateService,
            sceneManager,
            runtimeSettingsService,
            videoSettings,
            videoSettingsApplier,
            eventBus,
            sceneRuntimeService,
            new MyraMainMenuScreen(saveSlotService),
            audioSettings,
            inputBindings);
    }
}
