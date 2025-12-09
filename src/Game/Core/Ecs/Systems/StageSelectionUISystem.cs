using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.SceneState;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Component to track stage selection UI state.
/// </summary>
internal struct StageSelectionUIState
{
    public bool IsOpen { get; set; }
    public int SelectedStageIndex { get; set; }
    public int SelectedActIndex { get; set; }
    
    public StageSelectionUIState()
    {
        IsOpen = false;
        SelectedStageIndex = 0;
        SelectedActIndex = 0;
    }
}

/// <summary>
/// Handles stage selection UI in the hub.
/// </summary>
internal sealed class StageSelectionUISystem : IUpdateSystem, IUiDrawSystem, ILoadContentSystem
{
    private readonly StageRegistry _stageRegistry;
    private readonly SceneManager _sceneManager;
    private readonly PlayerProfileService _profileService;
    
    private SpriteFont _font = null!;
    private SpriteFont _titleFont = null!;
    private Texture2D _pixel = null!;
    
    private const int ButtonWidth = 600;
    private const int ButtonHeight = 80;
    private const int ButtonSpacing = 15;
    private const int StartY = 180;

    public StageSelectionUISystem(
        StageRegistry stageRegistry,
        SceneManager sceneManager,
        PlayerProfileService profileService)
    {
        _stageRegistry = stageRegistry;
        _sceneManager = sceneManager;
        _profileService = profileService;
    }

    public void Initialize(EcsWorld world)
    {
        // Create UI state entity
        var uiEntity = world.CreateEntity();
        world.SetComponent(uiEntity, new StageSelectionUIState());
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _font = content.Load<SpriteFont>("Fonts/FontRegularText");
        _titleFont = content.Load<SpriteFont>("Fonts/FontRegularTitle");
        _pixel = CreatePixel(graphicsDevice);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Find stage selection UI state
        Entity? uiEntity = null;
        world.ForEach<StageSelectionUIState>((Entity entity, ref StageSelectionUIState _) =>
        {
            uiEntity = entity;
        });

        if (!uiEntity.HasValue)
        {
            return;
        }

        var uiState = world.TryGetComponent<StageSelectionUIState>(uiEntity.Value, out var state) 
            ? state 
            : new StageSelectionUIState();

        if (!uiState.IsOpen)
        {
            world.SetComponent(uiEntity.Value, uiState);
            return;
        }

        // Get available stages for selected act
        var actStages = _stageRegistry.GetStagesForAct(uiState.SelectedActIndex + 1);
        if (actStages.Count == 0)
        {
            world.SetComponent(uiEntity.Value, uiState);
            return;
        }

        // Navigate stages
        if (context.Input.MenuDownPressed)
        {
            uiState.SelectedStageIndex = (uiState.SelectedStageIndex + 1) % actStages.Count;
        }
        else if (context.Input.MenuUpPressed)
        {
            uiState.SelectedStageIndex = (uiState.SelectedStageIndex - 1 + actStages.Count) % actStages.Count;
        }

        // Navigate acts (left/right)
        if (context.Input.MenuLeftPressed)
        {
            uiState.SelectedActIndex = Math.Max(0, uiState.SelectedActIndex - 1);
            uiState.SelectedStageIndex = 0;
        }
        else if (context.Input.MenuRightPressed)
        {
            // TODO: Max act count from config
            uiState.SelectedActIndex = Math.Min(3, uiState.SelectedActIndex + 1);
            uiState.SelectedStageIndex = 0;
        }

        // Select stage and start
        if (context.Input.MenuConfirmPressed)
        {
            var selectedStage = actStages[uiState.SelectedStageIndex];
            var profile = _profileService.LoadProfile();

            // Check if stage is unlocked
            if (IsStageUnlocked(selectedStage, profile))
            {
                // Transition to stage
                _sceneManager.TransitionToStage(selectedStage.StageId);
                uiState.IsOpen = false;
            }
            else
            {
                // TODO: Show "stage locked" message
                Console.WriteLine($"[StageSelection] Stage {selectedStage.StageId} is locked!");
            }
        }

        // Close UI
        if (context.Input.MenuBackPressed)
        {
            uiState.IsOpen = false;
        }

        world.SetComponent(uiEntity.Value, uiState);
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        // Find stage selection UI state
        Entity? uiEntity = null;
        world.ForEach<StageSelectionUIState>((Entity entity, ref StageSelectionUIState _) =>
        {
            uiEntity = entity;
        });

        if (!uiEntity.HasValue)
        {
            return;
        }

        var uiState = world.TryGetComponent<StageSelectionUIState>(uiEntity.Value, out var state) 
            ? state 
            : new StageSelectionUIState();

        if (!uiState.IsOpen)
        {
            return;
        }

        DrawStageSelectionUI(context.SpriteBatch, uiState);
    }

    /// <summary>
    /// Opens the stage selection UI from the hub menu.
    /// </summary>
    public static void Open(EcsWorld world)
    {
        world.ForEach<StageSelectionUIState>((Entity entity, ref StageSelectionUIState state) =>
        {
            state.IsOpen = true;
            world.SetComponent(entity, state);
        });
    }

    private void DrawStageSelectionUI(SpriteBatch spriteBatch, StageSelectionUIState uiState)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;
        var centerX = viewport.Width / 2;

        // Draw title
        var title = $"Act {uiState.SelectedActIndex + 1} - Stage Selection";
        var titleSize = _titleFont.MeasureString(title);
        spriteBatch.DrawString(
            _titleFont,
            title,
            new Vector2(centerX - titleSize.X / 2, 100),
            Color.Gold);

        // Get stages for selected act
        var actStages = _stageRegistry.GetStagesForAct(uiState.SelectedActIndex + 1);
        var profile = _profileService.LoadProfile();

        if (actStages.Count == 0)
        {
            var noStages = "No stages available for this act.";
            var noStagesSize = _font.MeasureString(noStages);
            spriteBatch.DrawString(
                _font,
                noStages,
                new Vector2(centerX - noStagesSize.X / 2, viewport.Height / 2),
                Color.Gray);
        }
        else
        {
            // Draw stage list
            for (int i = 0; i < actStages.Count; i++)
            {
                var stage = actStages[i];
                var y = StartY + i * (ButtonHeight + ButtonSpacing);
                var buttonRect = new Rectangle(
                    centerX - ButtonWidth / 2,
                    y,
                    ButtonWidth,
                    ButtonHeight);

                var isSelected = i == uiState.SelectedStageIndex;
                var isUnlocked = IsStageUnlocked(stage, profile);
                var isCompleted = profile.CompletedStages.Contains(stage.StageId);

                var bgColor = isSelected 
                    ? new Color(80, 80, 100, 200) 
                    : new Color(40, 40, 50, 180);
                
                if (!isUnlocked)
                {
                    bgColor = new Color(30, 30, 30, 180);
                }

                var textColor = isUnlocked 
                    ? (isSelected ? Color.Yellow : Color.White) 
                    : Color.Gray;

                // Draw button background
                spriteBatch.Draw(_pixel, buttonRect, bgColor);

                // Draw button border
                var borderColor = isSelected ? Color.Gold : (isUnlocked ? Color.Gray : Color.DarkGray);
                DrawBorder(spriteBatch, buttonRect, borderColor, 2);

                // Draw stage info
                var stageName = $"{stage.DisplayName}";
                if (isCompleted)
                {
                    stageName += " (Completed)";
                }
                else if (!isUnlocked)
                {
                    stageName += " [Locked]";
                }

                var nameSize = _font.MeasureString(stageName);
                spriteBatch.DrawString(
                    _font,
                    stageName,
                    new Vector2(buttonRect.X + 20, buttonRect.Y + 15),
                    textColor);

                // Draw stage description
                var description = stage.Description;
                if (!isUnlocked)
                {
                    description = $"Requires Meta Level {stage.RequiredMetaLevel}";
                }
                var descSize = _font.MeasureString(description);
                spriteBatch.DrawString(
                    _font,
                    description,
                    new Vector2(buttonRect.X + 20, buttonRect.Y + 45),
                    new Color(textColor, 0.7f));
            }
        }

        // Draw instructions
        var instructions = "Up/Down: Select  |  Left/Right: Change Act  |  Enter: Start  |  ESC: Back";
        var instructionsSize = _font.MeasureString(instructions);
        spriteBatch.DrawString(
            _font,
            instructions,
            new Vector2(centerX - instructionsSize.X / 2, viewport.Height - 80),
            Color.LightGray);

        // Draw player meta level
        var metaLevel = $"Meta Level: {profile.MetaLevel}";
        var metaLevelSize = _font.MeasureString(metaLevel);
        spriteBatch.DrawString(
            _font,
            metaLevel,
            new Vector2(viewport.Width - metaLevelSize.X - 20, 20),
            Color.Gold);
    }

    private static bool IsStageUnlocked(StageDefinition stage, PlayerProfile profile)
    {
        // Check meta level requirement
        if (profile.MetaLevel < stage.RequiredMetaLevel)
        {
            return false;
        }

        // Check previous stage completion
        if (!string.IsNullOrEmpty(stage.RequiredPreviousStageId))
        {
            if (!profile.CompletedStages.Contains(stage.RequiredPreviousStageId))
            {
                return false;
            }
        }

        return true;
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
