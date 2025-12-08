using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.Perks;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles perk tree UI rendering and input.
/// </summary>
internal sealed class PerkTreeUISystem : IUpdateSystem, IDrawSystem, ILoadContentSystem, IDisposable
{
    private readonly PerkTreeConfig _config;
    private readonly PerkService _perkService;
    private EcsWorld? _world;
    private Entity? _sessionEntity;
    private SpriteFont? _font;
    private SpriteFont? _titleFont;
    private Texture2D? _whitePixel;

    private const float MessageDuration = 2f;
    private const int PanelWidth = 800;
    private const int PanelHeight = 600;
    private const int PerkNodeSize = 80;
    private const int PerkSpacingX = 100;
    private const int PerkSpacingY = 100;

    public PerkTreeUISystem(PerkTreeConfig config, PerkService perkService)
    {
        _config = config;
        _perkService = perkService;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _font = content.Load<SpriteFont>("Fonts/Regular");
        _titleFont = content.Load<SpriteFont>("Fonts/Title");
        
        // Create white pixel for drawing rectangles
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public void Dispose()
    {
        _whitePixel?.Dispose();
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Find or cache session entity
        if (!TryCacheSessionEntity(world))
        {
            return;
        }

        if (!world.TryGetComponent<GameSession>(_sessionEntity!.Value, out var session))
        {
            return;
        }

        // Get/create perk UI component
        if (!world.TryGetComponent<PerkTreeUI>(_sessionEntity.Value, out var perkUI))
        {
            perkUI = new PerkTreeUI();
        }

        // Toggle perk tree with 'P' key (only when not game over)
        if (session.State != GameState.GameOver && context.Input.PerkTreePressed)
        {
            perkUI.IsOpen = !perkUI.IsOpen;
            if (perkUI.IsOpen)
            {
                // Reset selection when opening
                perkUI.SelectedPerkIndex = 0;
                perkUI.MessageText = null;
            }
            world.SetComponent(_sessionEntity.Value, perkUI);
            return;
        }

        if (!perkUI.IsOpen)
        {
            world.SetComponent(_sessionEntity.Value, perkUI);
            return;
        }

        // Update message timer
        if (perkUI.MessageTimer > 0f)
        {
            perkUI.MessageTimer -= context.DeltaSeconds;
            if (perkUI.MessageTimer <= 0f)
            {
                perkUI.MessageText = null;
            }
        }

        // Handle navigation
        var availablePerks = _config.Perks.OrderBy(p => p.GridPosition.Row)
            .ThenBy(p => p.GridPosition.Column)
            .ToList();

        if (availablePerks.Count == 0)
        {
            world.SetComponent(_sessionEntity.Value, perkUI);
            return;
        }

        var oldIndex = perkUI.SelectedPerkIndex;

        if (context.Input.MenuUpPressed && perkUI.SelectedPerkIndex > 0)
        {
            perkUI.SelectedPerkIndex--;
        }
        else if (context.Input.MenuDownPressed && perkUI.SelectedPerkIndex < availablePerks.Count - 1)
        {
            perkUI.SelectedPerkIndex++;
        }

        // Clamp selection
        perkUI.SelectedPerkIndex = Math.Clamp(perkUI.SelectedPerkIndex, 0, availablePerks.Count - 1);

        // Update hovered perk
        if (perkUI.SelectedPerkIndex != oldIndex)
        {
            perkUI.HoveredPerkId = availablePerks[perkUI.SelectedPerkIndex].Id;
        }
        else if (perkUI.HoveredPerkId == null)
        {
            perkUI.HoveredPerkId = availablePerks[perkUI.SelectedPerkIndex].Id;
        }

        // Find player entity
        Entity? playerEntity = null;
        world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) => playerEntity = entity);

        if (!playerEntity.HasValue)
        {
            world.SetComponent(_sessionEntity.Value, perkUI);
            return;
        }

        var player = playerEntity.Value;

        // Get player perk data
        if (!world.TryGetComponent<PlayerPerks>(player, out var playerPerks))
        {
            playerPerks = new PlayerPerks();
            world.SetComponent(player, playerPerks);
        }

        if (!world.TryGetComponent<PerkPoints>(player, out var perkPoints))
        {
            perkPoints = new PerkPoints(0, 0);
            world.SetComponent(player, perkPoints);
        }

        // Handle allocation
        if (context.Input.MenuConfirmPressed)
        {
            var selectedPerk = availablePerks[perkUI.SelectedPerkIndex];
            var result = _perkService.CanAllocate(selectedPerk.Id, playerPerks, perkPoints);

            if (result.CanAllocate)
            {
                if (_perkService.Allocate(selectedPerk.Id, ref playerPerks, ref perkPoints))
                {
                    var newRank = playerPerks.GetRank(selectedPerk.Id);
                    perkUI.MessageText = $"Allocated {selectedPerk.Name} (Rank {newRank})";
                    perkUI.MessageTimer = MessageDuration;

                    world.SetComponent(player, playerPerks);
                    world.SetComponent(player, perkPoints);
                    world.EventBus.Publish(new PerkAllocatedEvent(player, selectedPerk.Id, newRank));
                }
            }
            else
            {
                perkUI.MessageText = result.Message;
                perkUI.MessageTimer = MessageDuration;
            }
        }

        // Handle respec (hold Shift + press R)
        if (context.Input.RespecPressed)
        {
            _perkService.RespecAll(ref playerPerks, ref perkPoints);
            perkUI.MessageText = "All perks reset!";
            perkUI.MessageTimer = MessageDuration;

            world.SetComponent(player, playerPerks);
            world.SetComponent(player, perkPoints);
            world.EventBus.Publish(new PerksRespecedEvent(player));
        }

        world.SetComponent(_sessionEntity.Value, perkUI);
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        if (_world == null || _font == null || _titleFont == null || _whitePixel == null)
            return;

        if (!TryCacheSessionEntity(world))
            return;

        if (!world.TryGetComponent<PerkTreeUI>(_sessionEntity!.Value, out var perkUI) || !perkUI.IsOpen)
            return;

        var spriteBatch = context.SpriteBatch;
        var screenWidth = 1920; // Virtual resolution
        var screenHeight = 1080;

        // Draw semi-transparent background
        var fullScreenRect = new Rectangle(0, 0, screenWidth, screenHeight);
        spriteBatch.Draw(_whitePixel, fullScreenRect, Color.Black * 0.7f);

        // Draw panel background
        var panelX = (screenWidth - PanelWidth) / 2;
        var panelY = (screenHeight - PanelHeight) / 2;
        var panelRect = new Rectangle(panelX, panelY, PanelWidth, PanelHeight);
        spriteBatch.Draw(_whitePixel, panelRect, Color.Black * 0.9f);
        DrawBorder(spriteBatch, panelRect, 2, Color.Gold);

        // Draw title
        var titleText = "Perk Tree";
        var titleSize = _titleFont.MeasureString(titleText);
        var titlePos = new Vector2(panelX + (PanelWidth - titleSize.X) / 2, panelY + 20);
        spriteBatch.DrawString(_titleFont, titleText, titlePos, Color.Gold);

        // Find player
        Entity? playerEntity = null;
        world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) => playerEntity = entity);

        if (!playerEntity.HasValue)
            return;

        var player = playerEntity.Value;

        if (!world.TryGetComponent<PlayerPerks>(player, out var playerPerks))
        {
            playerPerks = new PlayerPerks();
        }

        if (!world.TryGetComponent<PerkPoints>(player, out var perkPoints))
        {
            perkPoints = new PerkPoints(0, 0);
        }

        // Draw available points
        var pointsText = $"Available Points: {perkPoints.AvailablePoints}";
        var pointsPos = new Vector2(panelX + 20, panelY + 60);
        spriteBatch.DrawString(_font, pointsText, pointsPos, Color.White);

        // Draw perks list
        var listY = panelY + 100;
        var lineHeight = 30;
        var availablePerks = _config.Perks.OrderBy(p => p.GridPosition.Row)
            .ThenBy(p => p.GridPosition.Column)
            .ToList();

        for (int i = 0; i < availablePerks.Count; i++)
        {
            var perk = availablePerks[i];
            var currentRank = playerPerks.GetRank(perk.Id);
            var isSelected = i == perkUI.SelectedPerkIndex;
            var yPos = listY + i * lineHeight;

            // Skip if off-screen
            if (yPos > panelY + PanelHeight - 100)
                break;

            // Highlight selected
            if (isSelected)
            {
                var highlightRect = new Rectangle(panelX + 10, (int)yPos - 2, PanelWidth - 20, lineHeight);
                spriteBatch.Draw(_whitePixel, highlightRect, Color.Gold * 0.3f);
            }

            // Check if can allocate
            var canAllocate = _perkService.CanAllocate(perk.Id, playerPerks, perkPoints);
            var textColor = canAllocate.CanAllocate ? Color.Lime : (currentRank > 0 ? Color.LightBlue : Color.Gray);

            // Draw perk name and rank
            var perkText = $"{perk.Name} [{currentRank}/{perk.MaxRank}]";
            var perkPos = new Vector2(panelX + 20, yPos);
            spriteBatch.DrawString(_font, perkText, perkPos, textColor);

            // Draw cost
            var costText = $"({perk.PointsPerRank} pts)";
            var costPos = new Vector2(panelX + PanelWidth - 120, yPos);
            spriteBatch.DrawString(_font, costText, costPos, Color.Yellow);
        }

        // Draw selected perk details
        if (perkUI.HoveredPerkId != null)
        {
            var selectedPerk = _config.GetPerk(perkUI.HoveredPerkId);
            if (selectedPerk != null)
            {
                var detailY = panelY + PanelHeight - 150;
                var detailX = panelX + 20;

                spriteBatch.DrawString(_font, "Description:", new Vector2(detailX, detailY), Color.Gold);
                spriteBatch.DrawString(_font, selectedPerk.Description, new Vector2(detailX, detailY + 25), Color.White);

                // Show prerequisites
                if (selectedPerk.Prerequisites.Count > 0)
                {
                    var prereqText = "Requires: " + string.Join(", ",
                        selectedPerk.Prerequisites.Select(p =>
                        {
                            var prereqDef = _config.GetPerk(p.PerkId);
                            return $"{prereqDef?.Name ?? p.PerkId} ({p.MinimumRank})";
                        }));
                    spriteBatch.DrawString(_font, prereqText, new Vector2(detailX, detailY + 50), Color.Orange);
                }
            }
        }

        // Draw message
        if (!string.IsNullOrEmpty(perkUI.MessageText) && perkUI.MessageTimer > 0f)
        {
            var messageSize = _font.MeasureString(perkUI.MessageText);
            var messagePos = new Vector2(
                panelX + (PanelWidth - messageSize.X) / 2,
                panelY + PanelHeight - 50);
            spriteBatch.DrawString(_font, perkUI.MessageText, messagePos, Color.Yellow);
        }

        // Draw controls hint
        var controlsText = "[↑↓] Navigate  [Enter] Allocate  [R] Respec All  [P] Close";
        var controlsSize = _font.MeasureString(controlsText);
        var controlsPos = new Vector2(
            panelX + (PanelWidth - controlsSize.X) / 2,
            panelY + PanelHeight - 25);
        spriteBatch.DrawString(_font, controlsText, controlsPos, Color.Gray);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, int thickness, Color color)
    {
        if (_whitePixel == null)
            return;

        // Top
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        // Left
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        spriteBatch.Draw(_whitePixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private bool TryCacheSessionEntity(EcsWorld world)
    {
        if (_sessionEntity is not null && world.IsAlive(_sessionEntity.Value))
        {
            return true;
        }

        _sessionEntity = null;
        world.ForEach<GameSession>((Entity entity, ref GameSession _) =>
        {
            _sessionEntity = entity;
        });

        return _sessionEntity.HasValue;
    }
}
