using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Audio;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.Diagnostics;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.UI;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private const int VirtualWidth = 960;
    private const int VirtualHeight = 540;
    private const int WindowScale = 2;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private RenderTarget2D _renderTarget = null!;
    private Camera2D _camera = null!;
    private SceneStateService _sceneStateService = null!;
    private SceneManager _sceneManager = null!;
    private InputState _input = null!;
    private AudioSettingsStore _audioSettingsStore = null!;
    private AudioSettingsConfig _audioSettings = null!;
    private VideoSettingsStore _videoSettingsStore = null!;
    private VideoSettingsConfig _videoSettings = null!;
    private InputBindingsStore _inputBindingsStore = null!;
    private InputBindingsConfig _inputBindings = null!;
    private RuntimeSettingsService _runtimeSettingsService = null!;
    private MusicService _musicService = null!;
    private VideoSettingsApplier _videoSettingsApplier = null!;
    private EventBus _eventBus = null!;
    private PersistenceRoot _persistenceRoot = null!;
    private SaveSlotService _saveSlotService = null!;
    private StageRegistry _stageRegistry = null!;
    private SceneRuntimeService _sceneRuntimeService = null!;
    private MyraMainMenuScreen _myraMenu = null!;
    private GameSettingsController _settingsController = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.HardwareModeSwitch = false; // Prefer borderless fullscreen to avoid macOS mode switches

        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);

        _graphics.PreferredBackBufferWidth = VirtualWidth * WindowScale;
        _graphics.PreferredBackBufferHeight = VirtualHeight * WindowScale;
    }

    protected override void Initialize()
    {
        MyraEnvironment.Game = this;

        _camera = new Camera2D(VirtualWidth, VirtualHeight);
        _audioSettingsStore = new AudioSettingsStore();
        _audioSettings = _audioSettingsStore.LoadOrDefault();
        _videoSettingsStore = new VideoSettingsStore();
        _videoSettings = _videoSettingsStore.LoadOrDefault();
        _inputBindingsStore = new InputBindingsStore();
        _inputBindings = _inputBindingsStore.LoadOrDefault();
        _videoSettingsApplier = new VideoSettingsApplier(_graphics, Window);
        _videoSettingsApplier.Apply(_videoSettings, applyChanges: false);
        _musicService = new MusicService(_audioSettings);
        RuntimeLog.Configure(new ConsoleRuntimeLogger(RuntimeLogSettings.FromEnvironment()));
        _runtimeSettingsService = new RuntimeSettingsService(
            _audioSettings,
            _audioSettingsStore,
            _videoSettings,
            _videoSettingsStore,
            _inputBindings,
            _inputBindingsStore,
            _musicService);
        _eventBus = new EventBus();
        _sceneStateService = new SceneStateService();
        _sceneManager = new SceneManager(_sceneStateService, _eventBus);
        _stageRegistry = new StageRegistry();
        _input = new InputState(_sceneStateService, VirtualWidth, VirtualHeight, _inputBindings);
        _persistenceRoot = new PersistenceRoot(new DefaultFileSystem());
        _saveSlotService = _persistenceRoot.CreateSaveSlotService();
        _sceneRuntimeService = new SceneRuntimeService(
            _camera,
            _audioSettings,
            _audioSettingsStore,
            _videoSettings,
            _videoSettingsStore,
            _inputBindings,
            _inputBindingsStore,
            _musicService,
            _eventBus,
            _stageRegistry,
            _sceneStateService,
            _sceneManager,
            _persistenceRoot,
            _saveSlotService);
        _myraMenu = new MyraMainMenuScreen(_saveSlotService);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);

        _videoSettingsApplier.Apply(_videoSettings, applyChanges: true);

        UiFonts.Load(Content);
        var uiSoundPlayer = new DirectUiSoundPlayer(Content, _audioSettings);
        _myraMenu.SetSoundPlayer(uiSoundPlayer);
        _myraMenu.Initialize(this);
        _settingsController = new GameSettingsController(
            _eventBus,
            _runtimeSettingsService,
            _videoSettings,
            _inputBindings,
            _input,
            _videoSettingsApplier,
            uiSoundPlayer);
        _sceneRuntimeService.LoadContent(Content);
    }

    protected override void Update(GameTime gameTime)
    {
        // Use Window.ClientBounds for mouse scaling because Mouse.GetState() returns window coordinates (points),
        // whereas GraphicsDevice.Viewport returns backbuffer coordinates (pixels).
        // On HiDPI (Retina), these differ.
        var clientBounds = Window.ClientBounds;
        _input.Update(clientBounds.Width, clientBounds.Height);

        // Process pending scene transitions
        _sceneRuntimeService.ProcessPendingSceneTransition(Content, GraphicsDevice);

        if (_sceneStateService.IsInMainMenu())
        {
            if (_settingsController.IsOpen)
            {
                _settingsController.Update(gameTime, _input);
            }
            else
            {
                var menuResult = _myraMenu.Update(gameTime, _input);
                HandleMainMenuResult(menuResult);
            }

            base.Update(gameTime);
            return;
        }

        _sceneRuntimeService.EnsureSceneReady(Content, GraphicsDevice);
        _sceneRuntimeService.Update(gameTime, _input);

        if (_sceneRuntimeService.ExitRequested)
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 1f, 0);

        if (!_sceneStateService.IsInMainMenu())
        {
            _sceneRuntimeService.MapService?.Draw(_camera.Transform);

            _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: SamplerState.PointClamp);
            _sceneRuntimeService.WorldRunner?.Draw(_spriteBatch);
            _spriteBatch.End();

            // Draw UI to render target (screen space relative to virtual resolution)
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _sceneRuntimeService.WorldRunner?.DrawUI(_spriteBatch);
            _spriteBatch.End();
        }

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(
            _renderTarget,
            destinationRectangle: new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
            color: Color.White);
        _spriteBatch.End();

        if (!_sceneStateService.IsInMainMenu())
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _sceneRuntimeService.WorldRunner?.DrawScreenSpaceUI(_spriteBatch);
            _spriteBatch.End();
        }

        if (_sceneStateService.IsInMainMenu())
        {
            _myraMenu.Draw();
            _settingsController.Draw();
        }

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sceneRuntimeService?.Dispose();
            _myraMenu?.Dispose();
            _settingsController?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void HandleMainMenuResult(MainMenuResult result)
    {
        switch (result.Action)
        {
            case MainMenuAction.StartSlot when !string.IsNullOrEmpty(result.SlotId):
                _sceneRuntimeService.ActivateSlot(result.SlotId);
                _settingsController.Close();
                _sceneManager.TransitionToHub();
                break;
            case MainMenuAction.CreateNewSlot:
                _sceneRuntimeService.CreateNextSlot();
                _settingsController.Close();
                _sceneManager.TransitionToHub();
                break;
            case MainMenuAction.Settings:
                _settingsController.Open("audio");
                break;
            case MainMenuAction.Quit:
                _settingsController.Close();
                Exit();
                break;
        }
    }
}
