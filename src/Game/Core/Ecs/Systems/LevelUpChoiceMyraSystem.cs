using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.SceneState;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Renders the level-up choice overlay using Myra and publishes selection events.
/// </summary>
internal sealed class LevelUpChoiceMyraSystem : IUiDrawSystem, ILoadContentSystem, IDisposable
{
    private readonly SceneStateService _sceneStateService;
    private MyraLevelUpChoiceScreen _screen = null!;
    private UiEventBridge? _bridge;

    public LevelUpChoiceMyraSystem(SceneStateService sceneStateService)
    {
        _sceneStateService = sceneStateService;
    }

    public void Initialize(EcsWorld world)
    {
        var uiSoundPlayer = new EventBusUiSoundPlayer(world.EventBus);
        _screen = new MyraLevelUpChoiceScreen(uiSoundPlayer);
        _bridge = new UiEventBridge(world.EventBus);
        _bridge.Subscribe<LevelUpChoiceViewModelEvent>(OnViewModel);

        _screen.ChoicePicked += choiceId =>
        {
            world.EventBus.Publish(new LevelUpChoicePickedEvent { ChoiceId = choiceId });
        };
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (!_sceneStateService.IsInStage())
        {
            return;
        }

        if (!_screen.IsVisible)
        {
            return;
        }

        context.SpriteBatch.End();
        _screen.Update(new GameTime());
        _screen.Render();
        context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
    }

    public void Dispose()
    {
        _bridge?.Dispose();
        _screen?.Dispose();
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        UiFonts.Load(content);
    }

    private void OnViewModel(LevelUpChoiceViewModelEvent evt)
    {
        _screen.ApplyViewModel(evt.ViewModel);
    }
}

