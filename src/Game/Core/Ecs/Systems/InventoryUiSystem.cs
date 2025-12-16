using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using Myra.Graphics2D.UI;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Loot;
using TheLastMageStanding.Game.Core.Player;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Renders and handles input for inventory/equipment UI.
/// </summary>
internal sealed class InventoryUiSystem : IUpdateSystem, IUiDrawSystem, ILoadContentSystem, System.IDisposable
{
    private InventoryService _inventoryService = null!;
    private MyraInventoryScreen _screen = null!;
    
    private KeyboardState _previousKeyboardState;
    private GamePadState _previousGamePadState;
    private bool _queuedClose;
    private InventoryUiMode? _queuedMode;
    private int? _queuedRowActivation;

    public void Initialize(EcsWorld world)
    {
        _inventoryService = new InventoryService(world);
        _screen = new MyraInventoryScreen();
        _screen.TabRequested += mode => _queuedMode = mode;
        _screen.RowActivated += index => _queuedRowActivation = index;
        
        // Create UI state entity if it doesn't exist
        world.ForEach<InventoryUiState>((Entity _, ref InventoryUiState _) => { });
    }

    public void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content)
    {
        UiFonts.Load(content);
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

        ApplyQueuedUiActions(world, ref uiState);

        // Toggle inventory with I key (gated by scene state in InputState) or Tab or Y button
        if (context.Input.InventoryPressed || IsKeyJustPressed(keyboard, Keys.Tab) || IsButtonJustPressed(gamePad, Buttons.Y))
        {
            uiState.IsOpen = !uiState.IsOpen;
            uiState.SelectedIndex = 0;
        }

        if (!uiState.IsOpen)
        {
            world.SetComponent(uiEntity.Value, uiState);
            _previousKeyboardState = keyboard;
            _previousGamePadState = gamePad;
            _screen.ApplyViewModel(new InventoryOverlayViewModel(false, uiState.Mode, uiState.SelectedIndex, System.Array.Empty<InventoryRowViewModel>()));
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
            _screen.ApplyViewModel(new InventoryOverlayViewModel(true, uiState.Mode, uiState.SelectedIndex, System.Array.Empty<InventoryRowViewModel>()));
            return;
        }

        // Navigate with arrow keys or D-pad
        int itemCount = uiState.Mode == InventoryUiMode.Inventory
            ? _inventoryService.GetInventoryItems(player.Value).Length
            : GetSortedEquippedItems(player.Value).Length;

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
                    var equipped = GetSortedEquippedItems(player.Value);
                    if (uiState.SelectedIndex < equipped.Length)
                    {
                        _inventoryService.UnequipItem(player.Value, equipped[uiState.SelectedIndex].slot);
                    }
                }
                uiState.SelectedIndex = 0;
            }
        }

        world.SetComponent(uiEntity.Value, uiState);
        _screen.ApplyViewModel(BuildViewModel(player.Value, uiState));
        _previousKeyboardState = keyboard;
        _previousGamePadState = gamePad;
    }

    public void Draw(EcsWorld world, in EcsDrawContext context)
    {
        // Find UI state
        InventoryUiState uiState = default;
        var hasUiState = false;
        world.ForEach<InventoryUiState>((Entity _, ref InventoryUiState state) =>
        {
            uiState = state;
            hasUiState = true;
        });

        if (!hasUiState || !uiState.IsOpen || !_screen.IsVisible)
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
        _screen.Dispose();
    }

    private bool IsKeyJustPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
    }

    private bool IsButtonJustPressed(GamePadState current, Buttons button)
    {
        return current.IsButtonDown(button) && !_previousGamePadState.IsButtonDown(button);
    }

    private void ApplyQueuedUiActions(EcsWorld world, ref InventoryUiState uiState)
    {
        if (_queuedMode.HasValue)
        {
            uiState.Mode = _queuedMode.Value;
            uiState.SelectedIndex = 0;
            _queuedMode = null;
        }

        if (_queuedRowActivation.HasValue)
        {
            var index = _queuedRowActivation.Value;
            _queuedRowActivation = null;

            Entity? player = null;
            world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) => player = entity);
            if (player.HasValue)
            {
                if (uiState.Mode == InventoryUiMode.Inventory)
                {
                    var items = _inventoryService.GetInventoryItems(player.Value);
                    if (index >= 0 && index < items.Length)
                    {
                        uiState.SelectedIndex = index;
                        _inventoryService.EquipItem(player.Value, items[index]);
                        uiState.SelectedIndex = 0;
                    }
                }
                else
                {
                    var equipped = GetSortedEquippedItems(player.Value);
                    if (index >= 0 && index < equipped.Length)
                    {
                        uiState.SelectedIndex = index;
                        _inventoryService.UnequipItem(player.Value, equipped[index].slot);
                        uiState.SelectedIndex = 0;
                    }
                }
            }
        }

        if (_queuedClose)
        {
            uiState.IsOpen = false;
            uiState.SelectedIndex = 0;
            _queuedClose = false;
        }
    }

    private InventoryOverlayViewModel BuildViewModel(Entity player, InventoryUiState uiState)
    {
        var rows = new List<InventoryRowViewModel>();

        if (uiState.Mode == InventoryUiMode.Inventory)
        {
            var items = _inventoryService.GetInventoryItems(player);
            foreach (var item in items)
            {
                rows.Add(new InventoryRowViewModel(
                    item.Name,
                    item.GetRarityColor(),
                    $"{item.EquipSlot} • {item.Rarity} • {item.Affixes.Count} affixes",
                    "Click/Enter: Equip"));
            }
        }
        else
        {
            var equipped = GetSortedEquippedItems(player);
            foreach (var (slot, item) in equipped)
            {
                rows.Add(new InventoryRowViewModel(
                    item.Name,
                    item.GetRarityColor(),
                    $"{slot} • {item.Rarity} • {item.Affixes.Count} affixes",
                    "Click/Enter: Unequip"));
            }
        }

        var clampedSelected = rows.Count == 0 ? 0 : Math.Clamp(uiState.SelectedIndex, 0, rows.Count - 1);
        return new InventoryOverlayViewModel(true, uiState.Mode, clampedSelected, rows);
    }

    private (EquipSlot slot, ItemInstance item)[] GetSortedEquippedItems(Entity player)
    {
        var equipped = _inventoryService.GetEquippedItems(player);
        return equipped
            .OrderBy(entry => entry.slot)
            .ToArray();
    }
}
