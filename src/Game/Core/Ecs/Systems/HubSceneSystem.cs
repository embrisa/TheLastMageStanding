using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Component to track hub UI state.
/// </summary>
internal struct HubUIState
{
    public HubMenu ActiveMenu { get; set; }
    public int SelectedIndex { get; set; }
    
    public HubUIState()
    {
        ActiveMenu = HubMenu.Main;
        SelectedIndex = 0;
    }
}

internal enum HubMenu
{
    Main,           // Main hub menu with buttons
    StageSelect,    // Stage selection
    Skills,         // Skill selection (P key also opens this)
    Talents,        // Talent tree (already has PerkTreeUISystem)
    Equipment,      // Equipment/inventory (I key also opens this)
    Shop,           // Shop (future)
    Stats           // Meta progression stats
}

/// <summary>
/// Handles hub scene-specific logic and UI navigation.
/// </summary>
internal sealed class HubSceneSystem : IUpdateSystem, IUiDrawSystem, ILoadContentSystem
{
    private readonly StageSelectionUISystem? _stageSelectionUI;
    
    private SpriteFont _font = null!;
    private SpriteFont _titleFont = null!;
    private Texture2D _pixel = null!;
    
    private const int ButtonWidth = 300;
    private const int ButtonHeight = 60;
    private const int ButtonSpacing = 12;

    private readonly string[] _mainMenuOptions = 
    {
        "Stage Select",
        "Skills (P)",
        "Talents (P)",
        "Equipment (I)",
        "Stats",
        "Quit"
    };

    public HubSceneSystem(StageSelectionUISystem? stageSelectionUI = null)
    {
        _stageSelectionUI = stageSelectionUI;
    }

    public void Initialize(EcsWorld world)
    {
        // Create hub UI state entity
        var hubEntity = world.CreateEntity();
        world.SetComponent(hubEntity, new HubUIState());
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _font = content.Load<SpriteFont>("Fonts/FontRegularText");
        _titleFont = content.Load<SpriteFont>("Fonts/FontRegularTitle");
        _pixel = CreatePixel(graphicsDevice);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Find hub UI state
        Entity? hubEntity = null;
        world.ForEach<HubUIState>((Entity entity, ref HubUIState _) =>
        {
            hubEntity = entity;
        });

        if (!hubEntity.HasValue)
        {
            return;
        }

        var hubState = world.TryGetComponent<HubUIState>(hubEntity.Value, out var state) 
            ? state 
            : new HubUIState();

        // Handle main menu input
        if (hubState.ActiveMenu == HubMenu.Main)
        {
            // Navigate menu
            if (context.Input.MenuDownPressed)
            {
                hubState.SelectedIndex = (hubState.SelectedIndex + 1) % _mainMenuOptions.Length;
            }
            else if (context.Input.MenuUpPressed)
            {
                hubState.SelectedIndex = (hubState.SelectedIndex - 1 + _mainMenuOptions.Length) % _mainMenuOptions.Length;
            }

            // Select option
            if (context.Input.MenuConfirmPressed)
            {
                switch (hubState.SelectedIndex)
                {
                    case 0: // Stage Select
                        if (_stageSelectionUI != null)
                        {
                            StageSelectionUISystem.Open(world);
                        }
                        break;
                    case 1: // Skills
                        // Open skill selection UI (future)
                        break;
                    case 2: // Talents (opens perk tree)
                        // Simulate P key press - perk tree system handles this
                        break;
                    case 3: // Equipment (opens inventory)
                        // Simulate I key press - inventory system handles this
                        break;
                    case 4: // Stats
                        hubState.ActiveMenu = HubMenu.Stats;
                        hubState.SelectedIndex = 0;
                        break;
                    case 5: // Quit
                        // Request exit (will be handled by Game1)
                        break;
                }
            }
        }
        else
        {
            // Handle back to main menu
            if (context.Input.MenuBackPressed)
            {
                hubState.ActiveMenu = HubMenu.Main;
                hubState.SelectedIndex = 0;
            }
        }

        world.SetComponent(hubEntity.Value, hubState);
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        // Find hub UI state
        Entity? hubEntity = null;
        world.ForEach<HubUIState>((Entity entity, ref HubUIState _) =>
        {
            hubEntity = entity;
        });

        if (!hubEntity.HasValue)
        {
            return;
        }

        var hubState = world.TryGetComponent<HubUIState>(hubEntity.Value, out var state) 
            ? state 
            : new HubUIState();

        // Draw based on active menu
        switch (hubState.ActiveMenu)
        {
            case HubMenu.Main:
                DrawMainMenu(context.SpriteBatch, hubState);
                break;
            case HubMenu.StageSelect:
                DrawStageSelect(context.SpriteBatch, hubState);
                break;
            case HubMenu.Stats:
                DrawStats(context.SpriteBatch, hubState);
                break;
        }
    }

    private void DrawMainMenu(SpriteBatch spriteBatch, HubUIState hubState)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;
        var centerX = viewport.Width / 2;

        const int titleButtonsSpacing = 24;
        const int buttonsInstructionsSpacing = 20;

        var title = "The Last Mage Standing - Hub";
        var titleSize = _titleFont.MeasureString(title);

        var instructions = "Up/Down: Navigate  |  Enter/Space: Select  |  ESC: Quit";
        var instructionsSize = _font.MeasureString(instructions);

        var buttonCount = _mainMenuOptions.Length;
        var buttonsHeight = buttonCount * ButtonHeight + (buttonCount - 1) * ButtonSpacing;
        var totalHeight = titleSize.Y + titleButtonsSpacing + buttonsHeight + buttonsInstructionsSpacing + instructionsSize.Y;
        var contentTop = Math.Max(0f, (viewport.Height - totalHeight) / 2f);

        var titlePosition = new Vector2(centerX - titleSize.X / 2f, contentTop);
        var startY = contentTop + titleSize.Y + titleButtonsSpacing;
        var instructionsY = startY + buttonsHeight + buttonsInstructionsSpacing;

        // Draw title
        spriteBatch.DrawString(_titleFont, title, titlePosition, Color.Gold);

        // Draw menu options
        for (int i = 0; i < _mainMenuOptions.Length; i++)
        {
            var y = (int)(startY + i * (ButtonHeight + ButtonSpacing));
            var buttonRect = new Rectangle(
                centerX - ButtonWidth / 2,
                y,
                ButtonWidth,
                ButtonHeight);

            var isSelected = i == hubState.SelectedIndex;
            var bgColor = isSelected ? new Color(80, 80, 100, 200) : new Color(40, 40, 50, 180);
            var textColor = isSelected ? Color.Yellow : Color.White;

            // Draw button background
            spriteBatch.Draw(_pixel, buttonRect, bgColor);

            // Draw button border
            var borderColor = isSelected ? Color.Gold : Color.Gray;
            DrawBorder(spriteBatch, buttonRect, borderColor, 2);

            // Draw button text
            var text = _mainMenuOptions[i];
            var textSize = _font.MeasureString(text);
            spriteBatch.DrawString(
                _font,
                text,
                new Vector2(buttonRect.X + (ButtonWidth - textSize.X) / 2, buttonRect.Y + (ButtonHeight - textSize.Y) / 2),
                textColor);
        }

        // Draw instructions
        spriteBatch.DrawString(
            _font,
            instructions,
            new Vector2(centerX - instructionsSize.X / 2f, instructionsY),
            Color.LightGray);
    }

    private void DrawStageSelect(SpriteBatch spriteBatch, HubUIState hubState)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;
        var centerX = viewport.Width / 2;

        // Draw placeholder for stage selection
        var title = "Stage Selection";
        var titleSize = _titleFont.MeasureString(title);
        spriteBatch.DrawString(
            _titleFont,
            title,
            new Vector2(centerX - titleSize.X / 2, 100),
            Color.Gold);

        var placeholder = "Stage selection UI coming soon...";
        var placeholderSize = _font.MeasureString(placeholder);
        spriteBatch.DrawString(
            _font,
            placeholder,
            new Vector2(centerX - placeholderSize.X / 2, viewport.Height / 2),
            Color.White);

        var instructions = "ESC: Back to Hub";
        var instructionsSize = _font.MeasureString(instructions);
        spriteBatch.DrawString(
            _font,
            instructions,
            new Vector2(centerX - instructionsSize.X / 2, viewport.Height - 80),
            Color.LightGray);
    }

    private void DrawStats(SpriteBatch spriteBatch, HubUIState hubState)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;
        var centerX = viewport.Width / 2;

        // Draw placeholder for stats
        var title = "Meta Progression Stats";
        var titleSize = _titleFont.MeasureString(title);
        spriteBatch.DrawString(
            _titleFont,
            title,
            new Vector2(centerX - titleSize.X / 2, 100),
            Color.Gold);

        var placeholder = "Stats UI coming soon...";
        var placeholderSize = _font.MeasureString(placeholder);
        spriteBatch.DrawString(
            _font,
            placeholder,
            new Vector2(centerX - placeholderSize.X / 2, viewport.Height / 2),
            Color.White);

        var instructions = "ESC: Back to Hub";
        var instructionsSize = _font.MeasureString(instructions);
        spriteBatch.DrawString(
            _font,
            instructions,
            new Vector2(centerX - instructionsSize.X / 2, viewport.Height - 80),
            Color.LightGray);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        // Top
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        // Left
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var pixel = new Texture2D(graphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        return pixel;
    }
}
