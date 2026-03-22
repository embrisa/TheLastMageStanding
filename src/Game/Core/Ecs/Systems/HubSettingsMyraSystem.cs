using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Renders the shared settings overlay while the player is in the hub.
/// </summary>
internal sealed class HubSettingsMyraSystem : IUiDrawSystem, ILoadContentSystem, IDisposable
{
    private readonly SceneStateService _sceneStateService;
    private MyraSettingsScreen _settingsScreen = null!;
    private UiEventBridge? _bridge;

    public HubSettingsMyraSystem(SceneStateService sceneStateService)
    {
        _sceneStateService = sceneStateService;
    }

    public void Initialize(EcsWorld world)
    {
        var uiSoundPlayer = new EventBusUiSoundPlayer(world.EventBus);
        _settingsScreen = new MyraSettingsScreen(uiSoundPlayer);
        _bridge = new UiEventBridge(world.EventBus);
        _bridge.Subscribe<SettingsMenuViewModelEvent>(OnSettingsViewModel);

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
        if (!_sceneStateService.IsInHub() || !_settingsScreen.IsVisible)
        {
            return;
        }

        context.SpriteBatch.End();
        _settingsScreen.Update(new GameTime());
        _settingsScreen.Render();
        context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
    }

    public void Dispose()
    {
        _bridge?.Dispose();
        _settingsScreen?.Dispose();
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        UiFonts.Load(content);
    }

    private void OnSettingsViewModel(SettingsMenuViewModelEvent evt)
    {
        _settingsScreen.ApplyViewModel(evt.ViewModel);
    }
}
