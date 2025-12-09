using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.SceneState;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Hub-specific ESC menu for settings and quit (not the same as stage pause menu).
/// </summary>
internal sealed class HubMenuSystem : IUpdateSystem, IUiDrawSystem, ILoadContentSystem, IDisposable
{
    private Entity? _menuEntity;
    private SpriteFont? _font;
    private Texture2D? _whitePixel;
    private readonly SceneStateService _sceneStateService;
    private readonly string[] _menuOptions = ["Settings", "Quit to Desktop"];

    public HubMenuSystem(SceneStateService sceneStateService)
    {
        _sceneStateService = sceneStateService;
    }

    public void Initialize(EcsWorld world)
    {
        _menuEntity = world.CreateEntity();
        world.SetComponent(_menuEntity.Value, new HubMenuState
        {
            IsOpen = false,
            SelectedIndex = 0
        });
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _font = content.Load<SpriteFont>("Fonts/FontRegularText");
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        if (!_sceneStateService.IsInHub() || !_menuEntity.HasValue)
            return;

        if (!world.TryGetComponent<HubMenuState>(_menuEntity.Value, out var state))
            return;

        // Toggle menu with ESC
        if (context.Input.PausePressed)
        {
            state.IsOpen = !state.IsOpen;
            state.SelectedIndex = 0;
            world.SetComponent(_menuEntity.Value, state);
            return;
        }

        if (!state.IsOpen)
            return;

        // Navigation
        if (context.Input.MenuUpPressed)
        {
            state.SelectedIndex = (state.SelectedIndex - 1 + _menuOptions.Length) % _menuOptions.Length;
            world.SetComponent(_menuEntity.Value, state);
        }

        if (context.Input.MenuDownPressed)
        {
            state.SelectedIndex = (state.SelectedIndex + 1) % _menuOptions.Length;
            world.SetComponent(_menuEntity.Value, state);
        }

        // Confirm selection
        if (context.Input.MenuConfirmPressed)
        {
            HandleMenuSelection(state.SelectedIndex);
        }
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        // Not used for UI draw systems
    }

    public void DrawUi(EcsWorld world, in EcsDrawContext context)
    {
        if (!_sceneStateService.IsInHub() || !_menuEntity.HasValue || _font == null || _whitePixel == null)
            return;

        if (!world.TryGetComponent<HubMenuState>(_menuEntity.Value, out var state) || !state.IsOpen)
            return;

        var spriteBatch = context.SpriteBatch;
        
        // Use virtual resolution (1280x720 based on game design)
        const int virtualWidth = 1280;
        const int virtualHeight = 720;

        // Semi-transparent overlay
        spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, virtualWidth, virtualHeight), new Color(0, 0, 0, 180));

        // Title
        var title = "Hub Menu";
        var titleSize = _font.MeasureString(title);
        var titlePos = new Vector2((virtualWidth - titleSize.X) / 2, virtualHeight / 4);
        spriteBatch.DrawString(_font, title, titlePos, Color.White);

        // Menu options
        var startY = virtualHeight / 3;
        for (int i = 0; i < _menuOptions.Length; i++)
        {
            var option = _menuOptions[i];
            var optionSize = _font.MeasureString(option);
            var optionPos = new Vector2((virtualWidth - optionSize.X) / 2, startY + i * 40);
            var color = i == state.SelectedIndex ? Color.Yellow : Color.White;
            spriteBatch.DrawString(_font, option, optionPos, color);
        }

        // ESC hint
        var hint = "ESC to close";
        var hintSize = _font.MeasureString(hint);
        var hintPos = new Vector2((virtualWidth - hintSize.X) / 2, virtualHeight - 100);
        spriteBatch.DrawString(_font, hint, hintPos, Color.Gray);
    }

    public void Dispose()
    {
        _whitePixel?.Dispose();
    }

    private static void HandleMenuSelection(int index)
    {
        switch (index)
        {
            case 0: // Settings
                // TODO: Open settings UI (future task)
                break;
            case 1: // Quit to Desktop
                // TODO: Trigger quit event or game exit
                break;
        }
    }
}

internal struct HubMenuState
{
    public bool IsOpen { get; set; }
    public int SelectedIndex { get; set; }
}
