using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Loot;
using TheLastMageStanding.Game.Core.Player;
using System.Collections.Generic;
using System.Linq;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Component to track inventory UI state.
/// </summary>
internal struct InventoryUiState
{
    public bool IsOpen { get; set; }
    public int SelectedIndex { get; set; }
    public InventoryUiMode Mode { get; set; } // Inventory vs Equipment view
    
    public InventoryUiState()
    {
        IsOpen = false;
        SelectedIndex = 0;
        Mode = InventoryUiMode.Inventory;
    }
}

internal enum InventoryUiMode
{
    Inventory,  // View inventory items
    Equipment   // View equipped items
}

/// <summary>
/// Renders and handles input for inventory/equipment UI.
/// </summary>
internal sealed class InventoryUiSystem : IUpdateSystem, IDrawSystem, ILoadContentSystem
{
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;
    private InventoryService _inventoryService = null!;
    
    private KeyboardState _previousKeyboardState;
    private GamePadState _previousGamePadState;

    public void Initialize(EcsWorld world)
    {
        _inventoryService = new InventoryService(world);
        
        // Create UI state entity if it doesn't exist
        world.ForEach<InventoryUiState>((Entity _, ref InventoryUiState _) => { });
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        _font = content.Load<SpriteFont>("Fonts/FontRegularText");
        _pixel = CreatePixel(graphicsDevice);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var keyboard = Keyboard.GetState();
        var gamePad = GamePad.GetState(PlayerIndex.One);

        // Find or create UI state entity
        Entity? uiEntity = null;
        world.ForEach<InventoryUiState>((Entity entity, ref InventoryUiState _) =>
        {
            uiEntity = entity;
        });

        if (!uiEntity.HasValue)
        {
            uiEntity = world.CreateEntity();
            world.SetComponent(uiEntity.Value, new InventoryUiState());
        }

        var uiState = world.TryGetComponent<InventoryUiState>(uiEntity.Value, out var state) 
            ? state 
            : new InventoryUiState();

        // Toggle inventory with Tab or Y button
        if (IsKeyJustPressed(keyboard, Keys.Tab) || IsButtonJustPressed(gamePad, Buttons.Y))
        {
            uiState.IsOpen = !uiState.IsOpen;
            uiState.SelectedIndex = 0;
        }

        if (!uiState.IsOpen)
        {
            world.SetComponent(uiEntity.Value, uiState);
            _previousKeyboardState = keyboard;
            _previousGamePadState = gamePad;
            return;
        }

        // Switch between inventory and equipment view with Q/E or LB/RB
        if (IsKeyJustPressed(keyboard, Keys.Q) || IsButtonJustPressed(gamePad, Buttons.LeftShoulder))
        {
            uiState.Mode = InventoryUiMode.Inventory;
            uiState.SelectedIndex = 0;
        }
        if (IsKeyJustPressed(keyboard, Keys.E) || IsButtonJustPressed(gamePad, Buttons.RightShoulder))
        {
            uiState.Mode = InventoryUiMode.Equipment;
            uiState.SelectedIndex = 0;
        }

        // Get player entity
        Entity? player = null;
        world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) =>
        {
            player = entity;
        });

        if (!player.HasValue)
        {
            world.SetComponent(uiEntity.Value, uiState);
            _previousKeyboardState = keyboard;
            _previousGamePadState = gamePad;
            return;
        }

        // Navigate with arrow keys or D-pad
        int itemCount = uiState.Mode == InventoryUiMode.Inventory
            ? _inventoryService.GetInventoryItems(player.Value).Length
            : _inventoryService.GetEquippedItems(player.Value).Length;

        if (itemCount > 0)
        {
            if (IsKeyJustPressed(keyboard, Keys.Up) || IsButtonJustPressed(gamePad, Buttons.DPadUp))
            {
                uiState.SelectedIndex = (uiState.SelectedIndex - 1 + itemCount) % itemCount;
            }
            if (IsKeyJustPressed(keyboard, Keys.Down) || IsButtonJustPressed(gamePad, Buttons.DPadDown))
            {
                uiState.SelectedIndex = (uiState.SelectedIndex + 1) % itemCount;
            }

            // Equip/unequip with Enter or A button
            if (IsKeyJustPressed(keyboard, Keys.Enter) || IsButtonJustPressed(gamePad, Buttons.A))
            {
                if (uiState.Mode == InventoryUiMode.Inventory)
                {
                    var items = _inventoryService.GetInventoryItems(player.Value);
                    if (uiState.SelectedIndex < items.Length)
                    {
                        _inventoryService.EquipItem(player.Value, items[uiState.SelectedIndex]);
                    }
                }
                else
                {
                    var equipped = _inventoryService.GetEquippedItems(player.Value);
                    if (uiState.SelectedIndex < equipped.Length)
                    {
                        _inventoryService.UnequipItem(player.Value, equipped[uiState.SelectedIndex].slot);
                    }
                }
                uiState.SelectedIndex = 0;
            }
        }

        world.SetComponent(uiEntity.Value, uiState);
        _previousKeyboardState = keyboard;
        _previousGamePadState = gamePad;
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        // Find UI state
        Entity? uiEntity = null;
        InventoryUiState uiState = default;
        world.ForEach<InventoryUiState>((Entity entity, ref InventoryUiState state) =>
        {
            uiEntity = entity;
            uiState = state;
        });

        if (!uiEntity.HasValue || !uiState.IsOpen)
            return;

        // Find player
        Entity? player = null;
        world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) =>
        {
            player = entity;
        });

        if (!player.HasValue)
            return;

        var spriteBatch = context.SpriteBatch;
        var viewport = spriteBatch.GraphicsDevice.Viewport;

        // Draw semi-transparent background
        spriteBatch.Draw(
            _pixel,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black * 0.7f);

        // Draw UI panel
        var panelWidth = 600;
        var panelHeight = 500;
        var panelX = (viewport.Width - panelWidth) / 2;
        var panelY = (viewport.Height - panelHeight) / 2;
        var panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);

        spriteBatch.Draw(_pixel, panelRect, Color.DarkSlateGray * 0.9f);
        spriteBatch.Draw(_pixel, new Rectangle(panelX, panelY, panelWidth, 2), Color.White);
        spriteBatch.Draw(_pixel, new Rectangle(panelX, panelY + panelHeight - 2, panelWidth, 2), Color.White);
        spriteBatch.Draw(_pixel, new Rectangle(panelX, panelY, 2, panelHeight), Color.White);
        spriteBatch.Draw(_pixel, new Rectangle(panelX + panelWidth - 2, panelY, 2, panelHeight), Color.White);

        // Draw title
        var titleText = uiState.Mode == InventoryUiMode.Inventory ? "INVENTORY" : "EQUIPMENT";
        var titlePos = new Vector2(panelX + 20, panelY + 20);
        spriteBatch.DrawString(_font, titleText, titlePos, Color.White);

        // Draw mode switcher hint
        var modeHint = "[Q] Inventory  [E] Equipment";
        var modeHintPos = new Vector2(panelX + panelWidth - 300, panelY + 20);
        spriteBatch.DrawString(_font, modeHint, modeHintPos, Color.Gray);

        // Draw items
        var itemY = panelY + 60;
        if (uiState.Mode == InventoryUiMode.Inventory)
        {
            DrawInventoryItems(spriteBatch, player.Value, panelX, itemY, panelWidth, uiState.SelectedIndex);
        }
        else
        {
            DrawEquippedItems(spriteBatch, player.Value, panelX, itemY, panelWidth, uiState.SelectedIndex);
        }

        // Draw controls hint
        var hint = "[Tab] Close  [↑↓] Navigate  [Enter] Equip/Unequip";
        var hintPos = new Vector2(panelX + 20, panelY + panelHeight - 40);
        spriteBatch.DrawString(_font, hint, hintPos, Color.Gray);
    }

    private void DrawInventoryItems(SpriteBatch spriteBatch, Entity player, int x, int y, int width, int selectedIndex)
    {
        var items = _inventoryService.GetInventoryItems(player);
        if (items.Length == 0)
        {
            spriteBatch.DrawString(_font, "No items in inventory", new Vector2(x + 20, y), Color.Gray);
            return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var isSelected = i == selectedIndex;
            var itemY = y + i * 70;

            // Highlight selected
            if (isSelected)
            {
                spriteBatch.Draw(_pixel, new Rectangle(x + 10, itemY - 5, width - 20, 65), Color.Yellow * 0.3f);
            }

            // Draw item name with rarity color
            var namePos = new Vector2(x + 20, itemY);
            spriteBatch.DrawString(_font, item.Name, namePos, item.GetRarityColor());

            // Draw slot and rarity
            var infoText = $"{item.EquipSlot} - {item.Rarity}";
            var infoPos = new Vector2(x + 20, itemY + 22);
            spriteBatch.DrawString(_font, infoText, infoPos, Color.Gray);

            // Draw affix count
            var affixText = $"{item.Affixes.Count} affixes";
            var affixPos = new Vector2(x + 20, itemY + 40);
            spriteBatch.DrawString(_font, affixText, affixPos, Color.LightGray);
        }
    }

    private void DrawEquippedItems(SpriteBatch spriteBatch, Entity player, int x, int y, int width, int selectedIndex)
    {
        var equipped = _inventoryService.GetEquippedItems(player);
        if (equipped.Length == 0)
        {
            spriteBatch.DrawString(_font, "No items equipped", new Vector2(x + 20, y), Color.Gray);
            return;
        }

        for (int i = 0; i < equipped.Length; i++)
        {
            var (slot, item) = equipped[i];
            var isSelected = i == selectedIndex;
            var itemY = y + i * 70;

            // Highlight selected
            if (isSelected)
            {
                spriteBatch.Draw(_pixel, new Rectangle(x + 10, itemY - 5, width - 20, 65), Color.Yellow * 0.3f);
            }

            // Draw item name with rarity color
            var namePos = new Vector2(x + 20, itemY);
            spriteBatch.DrawString(_font, item.Name, namePos, item.GetRarityColor());

            // Draw slot and rarity
            var infoText = $"{slot} - {item.Rarity}";
            var infoPos = new Vector2(x + 20, itemY + 22);
            spriteBatch.DrawString(_font, infoText, infoPos, Color.Gray);

            // Draw affix count
            var affixText = $"{item.Affixes.Count} affixes";
            var affixPos = new Vector2(x + 20, itemY + 40);
            spriteBatch.DrawString(_font, affixText, affixPos, Color.LightGray);
        }
    }

    private bool IsKeyJustPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
    }

    private bool IsButtonJustPressed(GamePadState current, Buttons button)
    {
        return current.IsButtonDown(button) && !_previousGamePadState.IsButtonDown(button);
    }

    private static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }
}
