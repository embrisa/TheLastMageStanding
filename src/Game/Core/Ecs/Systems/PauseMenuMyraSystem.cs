using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Renders the pause/audio menu overlay using Myra and listens to view-model events.
/// </summary>
internal sealed class PauseMenuMyraSystem : IUiDrawSystem, ILoadContentSystem, IDisposable
{
    private readonly SceneStateService _sceneStateService;
    private MyraPauseMenuScreen _screen = null!;
    private MyraSettingsScreen _settingsScreen = null!;
    private UiEventBridge? _bridge;

    public PauseMenuMyraSystem(SceneStateService sceneStateService)
    {
        _sceneStateService = sceneStateService;
    }

    public void Initialize(EcsWorld world)
    {
        var uiSoundPlayer = new EventBusUiSoundPlayer(world.EventBus);
        _screen = new MyraPauseMenuScreen(uiSoundPlayer);
        _settingsScreen = new MyraSettingsScreen(uiSoundPlayer);
        _bridge = new UiEventBridge(world.EventBus);
        _bridge.Subscribe<PauseMenuViewModelEvent>(OnViewModel);
        _bridge.Subscribe<SettingsMenuViewModelEvent>(OnSettingsViewModel);

        _screen.ActionRequested += action =>
        {
            world.EventBus.Publish(new PauseMenuActionRequestedEvent { Action = action });
        };
        _screen.AudioSettingChanged += change =>
        {
            world.EventBus.Publish(change);
        };

        _settingsScreen.AudioSettingChanged += change => world.EventBus.Publish(change);
        _settingsScreen.VideoSettingChanged += change => world.EventBus.Publish(change);
        _settingsScreen.InputBindingChanged += change => world.EventBus.Publish(change);
        _settingsScreen.TabChangedEvent += tabId =>
        {
            world.EventBus.Publish(new SettingsTabChangedEvent { TabId = tabId });
        };
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!_sceneStateService.IsInStage())
        {
            return;
        }

        if (!_screen.IsVisible && !_settingsScreen.IsVisible)
        {
            return;
        }

        context.SpriteBatch.End();
        if (_screen.IsVisible)
        {
            _screen.Update(new GameTime());
            _screen.Render();
        }

        if (_settingsScreen.IsVisible)
        {
            _settingsScreen.Update(new GameTime());
            _settingsScreen.Render();
        }
        context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
    }

    public void Dispose()
    {
        _bridge?.Dispose();
        _screen?.Dispose();
        _settingsScreen?.Dispose();
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        UiFonts.Load(content);
    }

    private void OnViewModel(PauseMenuViewModelEvent evt)
    {
        _screen.ApplyViewModel(evt.ViewModel);
    }

    private void OnSettingsViewModel(SettingsMenuViewModelEvent evt)
    {
        _settingsScreen.ApplyViewModel(evt.ViewModel);
    }
}

