using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Loot;

namespace TheLastMageStanding.Game.Core.Events;

/// <summary>
/// Published when loot is dropped in the world.
/// </summary>
internal readonly struct LootDroppedEvent
{
    public Entity LootEntity { get; init; }
    public ItemInstance Item { get; init; }
    public Vector2 Position { get; init; }

    public LootDroppedEvent(Entity lootEntity, ItemInstance item, Vector2 position)
    {
        LootEntity = lootEntity;
        Item = item;
        Position = position;
    }
}

/// <summary>
/// Published when a player picks up loot.
/// </summary>
internal readonly struct LootPickedUpEvent
{
    public Entity Player { get; init; }
    public ItemInstance Item { get; init; }

    public LootPickedUpEvent(Entity player, ItemInstance item)
    {
        Player = player;
        Item = item;
    }
}

/// <summary>
/// Published when an item is equipped.
/// </summary>
internal readonly struct ItemEquippedEvent
{
    public Entity Player { get; init; }
    public ItemInstance Item { get; init; }
    public ItemInstance? PreviousItem { get; init; }

    public ItemEquippedEvent(Entity player, ItemInstance item, ItemInstance? previousItem)
    {
        Player = player;
        Item = item;
        PreviousItem = previousItem;
    }
}

/// <summary>
/// Published when an item is unequipped.
/// </summary>
internal readonly struct ItemUnequippedEvent
{
    public Entity Player { get; init; }
    public ItemInstance Item { get; init; }
    public EquipSlot Slot { get; init; }

    public ItemUnequippedEvent(Entity player, ItemInstance item, EquipSlot slot)
    {
        Player = player;
        Item = item;
        Slot = slot;
    }
}
