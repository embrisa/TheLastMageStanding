using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Loot;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Entity can drop loot on death.
/// </summary>
internal struct LootDropper
{
    /// <summary>
    /// Base drop chance (0-1). Modified by enemy type (elite/boss).
    /// </summary>
    public float DropChance { get; set; }
    
    /// <summary>
    /// Multiplier applied when elite modifiers are present.
    /// </summary>
    public float ModifierRewardMultiplier { get; set; }
    
    /// <summary>
    /// Is this an elite enemy with higher drop rates?
    /// </summary>
    public bool IsElite { get; set; }
    
    /// <summary>
    /// Is this a boss with guaranteed drops?
    /// </summary>
    public bool IsBoss { get; set; }

    public LootDropper()
    {
        DropChance = 0.15f; // 15% base drop chance
        ModifierRewardMultiplier = 1f;
        IsElite = false;
        IsBoss = false;
    }
}

/// <summary>
/// Dropped loot item in the world.
/// </summary>
internal struct DroppedLoot
{
    /// <summary>
    /// Item instance data.
    /// </summary>
    public ItemInstance Item { get; set; }
    
    /// <summary>
    /// Time until the item can be picked up (prevents instant pickup on drop).
    /// </summary>
    public float PickupCooldown { get; set; }
    
    /// <summary>
    /// Time until the item despawns (0 = never).
    /// </summary>
    public float DespawnTimer { get; set; }

    public DroppedLoot(ItemInstance item, float pickupCooldown = 0.3f, float despawnTimer = 0f)
    {
        Item = item;
        PickupCooldown = pickupCooldown;
        DespawnTimer = despawnTimer;
    }
}

/// <summary>
/// Visual presentation for dropped loot.
/// </summary>
internal struct LootVisuals
{
    /// <summary>
    /// Highlight color based on rarity.
    /// </summary>
    public Color HighlightColor { get; set; }
    
    /// <summary>
    /// Outline thickness.
    /// </summary>
    public float OutlineThickness { get; set; }
    
    /// <summary>
    /// Pulse animation timer.
    /// </summary>
    public float PulseTimer { get; set; }

    public LootVisuals(Color highlightColor)
    {
        HighlightColor = highlightColor;
        OutlineThickness = 2f;
        PulseTimer = 0f;
    }
}

/// <summary>
/// Player's inventory containing picked up items.
/// </summary>
internal struct Inventory
{
    /// <summary>
    /// All items in inventory (not equipped).
    /// </summary>
    public List<ItemInstance> Items { get; set; }
    
    /// <summary>
    /// Maximum inventory size.
    /// </summary>
    public int MaxSize { get; set; }

    public Inventory()
    {
        Items = new List<ItemInstance>();
        MaxSize = 50; // Reasonable limit to prevent hoarding
    }

    public bool IsFull => Items.Count >= MaxSize;
    
    public bool TryAddItem(ItemInstance item)
    {
        if (IsFull)
            return false;
            
        Items.Add(item);
        return true;
    }
    
    public bool RemoveItem(ItemInstance item)
    {
        return Items.Remove(item);
    }
}

/// <summary>
/// Equipped items on the player.
/// </summary>
internal struct Equipment
{
    /// <summary>
    /// Items equipped in each slot.
    /// </summary>
    public Dictionary<EquipSlot, ItemInstance> Slots { get; set; }
    
    /// <summary>
    /// Cached total stat modifiers from all equipped items.
    /// </summary>
    public bool ModifiersDirty { get; set; }

    public Equipment()
    {
        Slots = new Dictionary<EquipSlot, ItemInstance>();
        ModifiersDirty = true;
    }

    public bool HasItemInSlot(EquipSlot slot) => Slots.ContainsKey(slot);
    
    public ItemInstance? GetItem(EquipSlot slot)
    {
        return Slots.TryGetValue(slot, out var item) ? item : null;
    }
    
    public void EquipItem(ItemInstance item)
    {
        Slots[item.EquipSlot] = item;
        ModifiersDirty = true;
    }
    
    public ItemInstance? UnequipItem(EquipSlot slot)
    {
        if (Slots.Remove(slot, out var item))
        {
            ModifiersDirty = true;
            return item;
        }
        return null;
    }
}

/// <summary>
/// Pickup radius for collecting loot.
/// </summary>
internal struct LootPickupRadius
{
    public float Radius { get; set; }

    public LootPickupRadius()
    {
        Radius = 32f; // Default pickup radius
    }
}
