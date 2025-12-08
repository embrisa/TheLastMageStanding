using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Loot;

namespace TheLastMageStanding.Game.Core.Player;

/// <summary>
/// Service for managing player inventory and equipment operations.
/// </summary>
internal sealed class InventoryService
{
    private readonly EcsWorld _world;

    public InventoryService(EcsWorld world)
    {
        _world = world;
    }

    /// <summary>
    /// Equip an item from inventory to a specific slot.
    /// Returns the previously equipped item if any.
    /// </summary>
    public ItemInstance? EquipItem(Entity player, ItemInstance item)
    {
        if (!_world.TryGetComponent(player, out Equipment equipment))
            return null;
        
        if (!_world.TryGetComponent(player, out Inventory inventory))
            return null;

        // Remove from inventory
        if (!inventory.RemoveItem(item))
            return null; // Item not in inventory

        // Get previously equipped item
        var previousItem = equipment.GetItem(item.EquipSlot);

        // Equip the new item
        equipment.EquipItem(item);
        
        // Update components
        _world.SetComponent(player, equipment);
        _world.SetComponent(player, inventory);

        // Publish event
        _world.EventBus.Publish(new ItemEquippedEvent(player, item, previousItem));

        // Return previous item to inventory if it exists
        if (previousItem != null)
        {
            inventory.TryAddItem(previousItem);
            _world.SetComponent(player, inventory);
        }

        return previousItem;
    }

    /// <summary>
    /// Unequip an item from a slot back to inventory.
    /// </summary>
    public bool UnequipItem(Entity player, EquipSlot slot)
    {
        if (!_world.TryGetComponent(player, out Equipment equipment))
            return false;
        
        if (!_world.TryGetComponent(player, out Inventory inventory))
            return false;

        var item = equipment.UnequipItem(slot);
        if (item == null)
            return false;

        // Add to inventory
        if (!inventory.TryAddItem(item))
        {
            // Inventory full, re-equip the item
            equipment.EquipItem(item);
            _world.SetComponent(player, equipment);
            return false;
        }

        // Update components
        _world.SetComponent(player, equipment);
        _world.SetComponent(player, inventory);

        // Publish event
        _world.EventBus.Publish(new ItemUnequippedEvent(player, item, slot));

        return true;
    }

    /// <summary>
    /// Get all items in player's inventory.
    /// </summary>
    public ItemInstance[] GetInventoryItems(Entity player)
    {
        if (!_world.TryGetComponent(player, out Inventory inventory))
            return System.Array.Empty<ItemInstance>();

        return inventory.Items.ToArray();
    }

    /// <summary>
    /// Get all equipped items.
    /// </summary>
    public (EquipSlot slot, ItemInstance item)[] GetEquippedItems(Entity player)
    {
        if (!_world.TryGetComponent(player, out Equipment equipment))
            return System.Array.Empty<(EquipSlot, ItemInstance)>();

        var result = new List<(EquipSlot, ItemInstance)>();
        foreach (var kvp in equipment.Slots)
        {
            result.Add((kvp.Key, kvp.Value));
        }

        return result.ToArray();
    }

    /// <summary>
    /// Check if inventory has space.
    /// </summary>
    public bool HasInventorySpace(Entity player)
    {
        if (!_world.TryGetComponent(player, out Inventory inventory))
            return false;

        return !inventory.IsFull;
    }
}
