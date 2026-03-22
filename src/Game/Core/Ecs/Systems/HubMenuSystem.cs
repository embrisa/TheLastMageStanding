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
    private readonly SceneManager _sceneManager;
    private readonly string[] _menuOptions = ["Settings", "Return to Main Menu"];

    public HubMenuSystem(SceneStateService sceneStateService, SceneManager sceneManager)
    {
        _sceneStateService = sceneStateService;
        _sceneManager = sceneManager;
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

        if (TryHandleBackAction(world, context, ref state))
        {
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
            HandleMenuSelection(world, ref state, state.SelectedIndex);
            world.SetComponent(_menuEntity.Value, state);
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
        
        // Use virtual resolution (960x540 based on game design)
        const int virtualWidth = 960;
        const int virtualHeight = 540;

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

    private static bool TryHandleBackAction(EcsWorld world, in EcsUpdateContext context, ref HubMenuState state)
    {
        if (IsSettingsOpen(world) && (context.Input.PausePressed || context.Input.MenuBackPressed))
        {
            SetSettingsOpen(world, isOpen: false);
            return true;
        }

        if (!context.Input.PausePressed)
        {
            return false;
        }

        if (!state.IsOpen && HubModalState.HasBlockingModalOpen(world))
        {
            return false;
        }

        state.IsOpen = !state.IsOpen;
        state.SelectedIndex = 0;
        return true;
    }

    private void HandleMenuSelection(EcsWorld world, ref HubMenuState state, int index)
    {
        switch (index)
        {
            case 0:
                state.IsOpen = false;
                SetSettingsOpen(world, isOpen: true, activeTab: "audio");
                break;
            case 1:
                state.IsOpen = false;
                _sceneManager.TransitionToMainMenu();
                break;
        }
    }

    private static bool IsSettingsOpen(EcsWorld world)
    {
        return HubModalState.IsSettingsOpen(world);
    }

    private static void SetSettingsOpen(EcsWorld world, bool isOpen, string? activeTab = null)
    {
        world.ForEach<SettingsMenuState>((Entity entity, ref SettingsMenuState state) =>
        {
            state.IsOpen = isOpen;
            if (isOpen && !string.IsNullOrWhiteSpace(activeTab))
            {
                state.ActiveTab = activeTab;
            }

            world.SetComponent(entity, state);
        });
    }
}

internal struct HubMenuState
{
    public bool IsOpen { get; set; }
    public int SelectedIndex { get; set; }
}
