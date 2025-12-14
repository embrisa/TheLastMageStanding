using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Loot;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles loot drops when enemies die.
/// </summary>
internal sealed class LootDropSystem : IUpdateSystem
{
    private readonly ItemFactory _itemFactory;
    private readonly LootDropConfig _config;
    private readonly Random _rng;
    private EcsWorld? _world;

    public LootDropSystem(ItemFactory itemFactory, LootDropConfig config, Random? rng = null)
    {
        _itemFactory = itemFactory;
        _config = config;
        _rng = rng ?? new Random();
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        // Subscribe to entity death events
        world.EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // No per-frame logic, loot drops are event-driven
    }

    private void OnEnemyDied(EnemyDiedEvent evt)
    {
        if (_world == null) return;
        var dropper = evt.Dropper;

        // Roll for drop
        float dropChance = dropper.DropChance;
        if (dropper.IsBoss)
            dropChance = _config.BossDropChance;
        else if (dropper.IsElite)
            dropChance = _config.EliteDropChance;

        dropChance *= MathF.Max(1f, dropper.ModifierRewardMultiplier);
        dropChance = MathF.Min(dropChance, dropper.IsBoss ? 1f : 0.95f);

        if (_rng.NextDouble() > dropChance)
            return;

        // Generate random item
        var item = _itemFactory.GenerateRandomItem();
        if (item == null)
            return;

        // Spawn loot entity
        SpawnLootEntity(item, evt.Position);
    }

    private void SpawnLootEntity(ItemInstance item, Vector2 position)
    {
        if (_world == null) return;
        
        var lootEntity = _world.CreateEntity();
        
        _world.SetComponent(lootEntity, new Position(position));
        _world.SetComponent(lootEntity, new DroppedLoot(item, pickupCooldown: 0.3f));
        _world.SetComponent(lootEntity, new LootVisuals(item.GetRarityColor()));
        
        // Add collision for pickup detection
        _world.SetComponent(lootEntity, new Collider
        {
            Shape = ColliderShape.Circle,
            Width = 8f,
            Layer = CollisionLayer.Pickup
        });

        // Publish event
        _world.EventBus.Publish(new LootDroppedEvent(lootEntity, item, position));
    }
}

/// <summary>
/// Handles loot pickup when player collides with dropped loot.
/// </summary>
internal sealed class LootPickupSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
        // No initialization needed
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;
        
        // Update pickup cooldowns and despawn timers for all dropped loot
        world.ForEach<DroppedLoot, Position>((Entity entity, ref DroppedLoot loot, ref Position pos) =>
        {
            // Update cooldown
            if (loot.PickupCooldown > 0)
            {
                loot.PickupCooldown -= deltaSeconds;
            }
            
            // Update despawn timer
            if (loot.DespawnTimer > 0)
            {
                loot.DespawnTimer -= deltaSeconds;
                if (loot.DespawnTimer <= 0)
                {
                    world.DestroyEntity(entity);
                    return;
                }
            }
            
            // Check for player pickup if cooldown expired
            if (loot.PickupCooldown <= 0)
            {
                TryPickupByNearbyPlayer(world, entity, loot, pos);
            }
        });
    }

    private static void TryPickupByNearbyPlayer(EcsWorld world, Entity lootEntity, DroppedLoot loot, Position lootPos)
    {
        // Find nearby players with all required components
        world.ForEach<PlayerTag, Position, Inventory>((Entity playerEntity, ref PlayerTag _, ref Position playerPos, ref Inventory inventory) =>
        {
            // Try to get pickup radius (optional component)
            float pickupRadiusSq = 32f * 32f; // Default
            if (world.TryGetComponent<LootPickupRadius>(playerEntity, out var pickupRadius))
            {
                pickupRadiusSq = pickupRadius.Radius * pickupRadius.Radius;
            }
            
            // Check distance
            var distanceSq = Vector2.DistanceSquared(lootPos.Value, playerPos.Value);
            if (distanceSq <= pickupRadiusSq)
            {
                // Try to add to inventory
                if (inventory.TryAddItem(loot.Item))
                {
                    // Pickup successful
                    world.EventBus.Publish(new LootPickedUpEvent(playerEntity, loot.Item));
                    world.DestroyEntity(lootEntity);
                }
            }
        });
    }
}

/// <summary>
/// Updates computed stats when equipment changes.
/// </summary>
internal sealed class EquipmentStatSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
        // Subscribe to equipment events
        world.EventBus.Subscribe<ItemEquippedEvent>(OnItemEquipped);
        world.EventBus.Subscribe<ItemUnequippedEvent>(OnItemUnequipped);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Check for dirty equipment and recalculate stats
        world.ForEach<Equipment, ComputedStats>((Entity entity, ref Equipment equipment, ref ComputedStats computed) =>
        {
            if (!equipment.ModifiersDirty)
                return;

            // Recalculate equipment modifiers
            var equipmentMods = StatModifiers.Zero;
            foreach (var kvp in equipment.Slots)
            {
                var item = kvp.Value;
                var itemMods = item.CalculateStatModifiers();
                equipmentMods = CombineModifiers(equipmentMods, itemMods);
            }

            // Update entity's equipment modifiers
            world.SetComponent(entity, new EquipmentModifiers { Value = equipmentMods });
            
            // Mark computed stats as dirty so they get recalculated
            computed.IsDirty = true;

            equipment.ModifiersDirty = false;
        });
    }

    private static void OnItemEquipped(ItemEquippedEvent evt)
    {
        // Stat recalculation will happen in Update
    }

    private static void OnItemUnequipped(ItemUnequippedEvent evt)
    {
        // Stat recalculation will happen in Update
    }

    private static StatModifiers CombineModifiers(StatModifiers a, StatModifiers b)
    {
        return new StatModifiers
        {
            PowerAdditive = a.PowerAdditive + b.PowerAdditive,
            PowerMultiplicative = a.PowerMultiplicative * b.PowerMultiplicative,
            AttackSpeedAdditive = a.AttackSpeedAdditive + b.AttackSpeedAdditive,
            AttackSpeedMultiplicative = a.AttackSpeedMultiplicative * b.AttackSpeedMultiplicative,
            CritChanceAdditive = a.CritChanceAdditive + b.CritChanceAdditive,
            CritMultiplierAdditive = a.CritMultiplierAdditive + b.CritMultiplierAdditive,
            CooldownReductionAdditive = a.CooldownReductionAdditive + b.CooldownReductionAdditive,
            ArmorAdditive = a.ArmorAdditive + b.ArmorAdditive,
            ArmorMultiplicative = a.ArmorMultiplicative * b.ArmorMultiplicative,
            ArcaneResistAdditive = a.ArcaneResistAdditive + b.ArcaneResistAdditive,
            ArcaneResistMultiplicative = a.ArcaneResistMultiplicative * b.ArcaneResistMultiplicative,
            MoveSpeedAdditive = a.MoveSpeedAdditive + b.MoveSpeedAdditive,
            MoveSpeedMultiplicative = a.MoveSpeedMultiplicative * b.MoveSpeedMultiplicative
        };
    }
}
