using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Loot;

namespace TheLastMageStanding.Game.Core.Player;

/// <summary>
/// System that auto-saves equipment and inventory periodically during a run.
/// </summary>
internal sealed class EquipmentAutoSaveSystem : IUpdateSystem
{
    private readonly EquipmentPersistenceService _persistenceService;
    private float _timeSinceLastSave;
    private const float SaveInterval = 30f; // Save every 30 seconds

    public EquipmentAutoSaveSystem()
    {
        _persistenceService = new EquipmentPersistenceService();
    }

    public void Initialize(EcsWorld world)
    {
        // Load saved equipment on initialization
        LoadEquipmentForPlayer(world);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        _timeSinceLastSave += context.DeltaSeconds;

        if (_timeSinceLastSave >= SaveInterval)
        {
            SaveEquipmentForPlayer(world);
            _timeSinceLastSave = 0f;
        }
    }

    /// <summary>
    /// Manually trigger a save (e.g., on map transition or important events).
    /// </summary>
    public void SaveNow(EcsWorld world)
    {
        SaveEquipmentForPlayer(world);
        _timeSinceLastSave = 0f;
    }

    private void SaveEquipmentForPlayer(EcsWorld world)
    {
        // Find player entity
        Entity? player = null;
        world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) =>
        {
            player = entity;
        });

        if (!player.HasValue)
            return;

        // Get equipment and inventory
        if (!world.TryGetComponent(player.Value, out Equipment equipment))
            return;
        if (!world.TryGetComponent(player.Value, out Inventory inventory))
            return;

        // Create snapshot
        var snapshot = new EquipmentSnapshot();

        // Save equipped items
        foreach (var kvp in equipment.Slots)
        {
            snapshot.EquippedItems[kvp.Key] = ItemInstanceData.FromItemInstance(kvp.Value);
        }

        // Save inventory items
        foreach (var item in inventory.Items)
        {
            snapshot.InventoryItems.Add(ItemInstanceData.FromItemInstance(item));
        }

        // Persist to disk
        _persistenceService.SaveEquipment(snapshot);
    }

    private void LoadEquipmentForPlayer(EcsWorld world)
    {
        // Find player entity
        Entity? player = null;
        world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) =>
        {
            player = entity;
        });

        if (!player.HasValue)
            return;

        // Load snapshot
        var snapshot = _persistenceService.LoadEquipment();
        if (snapshot == null)
            return;

        // Get equipment and inventory components
        if (!world.TryGetComponent(player.Value, out Equipment equipment))
            equipment = new Equipment();
        if (!world.TryGetComponent(player.Value, out Inventory inventory))
            inventory = new Inventory();

        // Restore equipped items
        equipment.Slots.Clear();
        foreach (var kvp in snapshot.EquippedItems)
        {
            var item = kvp.Value.ToItemInstance();
            equipment.Slots[kvp.Key] = item;
        }
        equipment.ModifiersDirty = true;

        // Restore inventory items
        inventory.Items.Clear();
        foreach (var itemData in snapshot.InventoryItems)
        {
            inventory.Items.Add(itemData.ToItemInstance());
        }

        // Update components
        world.SetComponent(player.Value, equipment);
        world.SetComponent(player.Value, inventory);
    }

    /// <summary>
    /// Clear the save file (e.g., on run end or new game).
    /// </summary>
    public void ClearSave()
    {
        _persistenceService.ClearSave();
    }
}
