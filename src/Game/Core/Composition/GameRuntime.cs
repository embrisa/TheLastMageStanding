using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.UI;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.Composition;

internal sealed class GameRuntime : IDisposable
{
    private readonly AudioSettingsConfig _audioSettings;
    private readonly InputBindingsConfig _inputBindings;

    public GameRuntime(
        Camera2D camera,
        InputState input,
        SceneStateService sceneStateService,
        SceneManager sceneManager,
        RuntimeSettingsService runtimeSettingsService,
        VideoSettingsConfig videoSettings,
        VideoSettingsApplier videoSettingsApplier,
        EventBus eventBus,
        SceneRuntimeService sceneRuntimeService,
        MyraMainMenuScreen mainMenu,
        AudioSettingsConfig audioSettings,
        InputBindingsConfig inputBindings)
    {
        Camera = camera;
        Input = input;
        SceneStateService = sceneStateService;
        SceneManager = sceneManager;
        RuntimeSettingsService = runtimeSettingsService;
        VideoSettings = videoSettings;
        VideoSettingsApplier = videoSettingsApplier;
        EventBus = eventBus;
        SceneRuntimeService = sceneRuntimeService;
        MainMenu = mainMenu;
        _audioSettings = audioSettings;
        _inputBindings = inputBindings;
    }

    public Camera2D Camera { get; }
    public InputState Input { get; }
    public SceneStateService SceneStateService { get; }
    public SceneManager SceneManager { get; }
    public RuntimeSettingsService RuntimeSettingsService { get; }
    public VideoSettingsConfig VideoSettings { get; }
    public VideoSettingsApplier VideoSettingsApplier { get; }
    public EventBus EventBus { get; }
    public SceneRuntimeService SceneRuntimeService { get; }
    public MyraMainMenuScreen MainMenu { get; }
    public GameSettingsController SettingsController { get; private set; } = null!;

    public void LoadContent(Game1 game, ContentManager content)
    {
        VideoSettingsApplier.Apply(VideoSettings, applyChanges: true);

        UiFonts.Load(content);
        var uiSoundPlayer = new DirectUiSoundPlayer(content, _audioSettings);
        MainMenu.SetSoundPlayer(uiSoundPlayer);
        MainMenu.Initialize(game);
        SettingsController = new GameSettingsController(
            EventBus,
            RuntimeSettingsService,
            VideoSettings,
            _inputBindings,
            Input,
            VideoSettingsApplier,
            uiSoundPlayer);
        SceneRuntimeService.LoadContent(content);
    }

    public void Dispose()
    {
        SceneRuntimeService.Dispose();
        MainMenu.Dispose();
        SettingsController?.Dispose();
    }
}
