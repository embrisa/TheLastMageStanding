using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLastMageStanding.Game.Core.Loot;

/// <summary>
/// Weighted entry for drop table selection.
/// </summary>
/// <typeparam name="T">Type of item in the table.</typeparam>
public readonly struct WeightedEntry<T>
{
    public T Item { get; init; }
    public float Weight { get; init; }

    public WeightedEntry(T item, float weight)
    {
        Item = item;
        Weight = weight;
    }
}

/// <summary>
/// Drop table with weighted random selection.
/// </summary>
/// <typeparam name="T">Type of items in the table.</typeparam>
public sealed class DropTable<T>
{
    private readonly List<WeightedEntry<T>> _entries;
    private readonly float _totalWeight;

    public DropTable(List<WeightedEntry<T>> entries)
    {
        _entries = entries;
        _totalWeight = entries.Sum(e => e.Weight);
    }

    /// <summary>
    /// Select a random entry from the table.
    /// Returns default if table is empty.
    /// </summary>
    public T? SelectRandom(Random rng)
    {
        if (_entries.Count == 0 || _totalWeight <= 0)
            return default;

        var roll = (float)rng.NextDouble() * _totalWeight;
        float cumulative = 0f;

        foreach (var entry in _entries)
        {
            cumulative += entry.Weight;
            if (roll <= cumulative)
                return entry.Item;
        }

        // Fallback to last entry
        return _entries[^1].Item;
    }
}

/// <summary>
/// Configuration for loot drop rates and tables.
/// </summary>
public sealed class LootDropConfig
{
    // Rarity weights for drop rolls
    public DropTable<ItemRarity> RarityTable { get; }
    
    // Item definition tables by slot
    public Dictionary<EquipSlot, DropTable<string>> ItemTables { get; }
    
    // Drop chance per enemy kill
    public float BaseDropChance { get; set; }
    
    // Drop chance for elite enemies
    public float EliteDropChance { get; set; }
    
    // Drop chance for bosses
    public float BossDropChance { get; set; }
    
    // Number of affixes per rarity
    public Dictionary<ItemRarity, (int min, int max)> AffixCountByRarity { get; }

    public LootDropConfig(
        DropTable<ItemRarity> rarityTable,
        Dictionary<EquipSlot, DropTable<string>> itemTables,
        float baseDropChance = 0.15f,
        float eliteDropChance = 0.5f,
        float bossDropChance = 1.0f)
    {
        RarityTable = rarityTable;
        ItemTables = itemTables;
        BaseDropChance = baseDropChance;
        EliteDropChance = eliteDropChance;
        BossDropChance = bossDropChance;
        
        // Default affix counts per rarity
        AffixCountByRarity = new Dictionary<ItemRarity, (int min, int max)>
        {
            { ItemRarity.Common, (0, 1) },
            { ItemRarity.Uncommon, (1, 2) },
            { ItemRarity.Rare, (2, 3) },
            { ItemRarity.Epic, (3, 4) },
            { ItemRarity.Legendary, (4, 5) }
        };
    }

    /// <summary>
    /// Create default loot config for testing.
    /// </summary>
    public static LootDropConfig CreateDefault()
    {
        // Rarity weights (Common most common, Legendary very rare)
        var rarityTable = new DropTable<ItemRarity>(new List<WeightedEntry<ItemRarity>>
        {
            new(ItemRarity.Common, 50f),      // 50%
            new(ItemRarity.Uncommon, 30f),    // 30%
            new(ItemRarity.Rare, 15f),        // 15%
            new(ItemRarity.Epic, 4f),         // 4%
            new(ItemRarity.Legendary, 1f)     // 1%
        });

        // Item definition ID tables per slot
        var itemTables = new Dictionary<EquipSlot, DropTable<string>>
        {
            [EquipSlot.Weapon] = new DropTable<string>(new List<WeightedEntry<string>>
            {
                new("weapon_staff", 1f),
                new("weapon_wand", 1f)
            }),
            [EquipSlot.Armor] = new DropTable<string>(new List<WeightedEntry<string>>
            {
                new("armor_robe", 1f),
                new("armor_cloak", 1f)
            }),
            [EquipSlot.Amulet] = new DropTable<string>(new List<WeightedEntry<string>>
            {
                new("amulet_arcane", 1f),
                new("amulet_power", 1f)
            }),
            [EquipSlot.Ring1] = new DropTable<string>(new List<WeightedEntry<string>>
            {
                new("ring_crit", 1f),
                new("ring_speed", 1f)
            }),
            [EquipSlot.Ring2] = new DropTable<string>(new List<WeightedEntry<string>>
            {
                new("ring_crit", 1f),
                new("ring_speed", 1f)
            })
        };

        return new LootDropConfig(rarityTable, itemTables);
    }
}

/// <summary>
/// Factory for generating item instances with rolled affixes.
/// </summary>
public sealed class ItemFactory
{
    private readonly Dictionary<string, ItemDefinition> _definitions;
    private readonly LootDropConfig _config;
    private readonly Random _rng;

    public ItemFactory(Dictionary<string, ItemDefinition> definitions, LootDropConfig config, Random? rng = null)
    {
        _definitions = definitions;
        _config = config;
        _rng = rng ?? new Random();
    }

    /// <summary>
    /// Generate a random item for a specific slot.
    /// </summary>
    public ItemInstance? GenerateItem(EquipSlot slot)
    {
        // Select definition ID from table
        if (!_config.ItemTables.TryGetValue(slot, out var table))
            return null;

        var definitionId = table.SelectRandom(_rng);
        if (definitionId == null || !_definitions.TryGetValue(definitionId, out var definition))
            return null;

        // Roll rarity
        var rarity = _config.RarityTable.SelectRandom(_rng);

        // Roll affixes
        var affixCount = RollAffixCount(rarity);
        var rolledAffixes = RollAffixes(definition.AffixPool, affixCount);

        return new ItemInstance(
            definition.Id,
            definition.Name,
            definition.ItemType,
            definition.EquipSlot,
            rarity,
            rolledAffixes);
    }

    /// <summary>
    /// Generate a random item from any slot.
    /// </summary>
    public ItemInstance? GenerateRandomItem()
    {
        var slots = new[] { EquipSlot.Weapon, EquipSlot.Armor, EquipSlot.Amulet, EquipSlot.Ring1 };
        var slot = slots[_rng.Next(slots.Length)];
        return GenerateItem(slot);
    }

    private int RollAffixCount(ItemRarity rarity)
    {
        if (!_config.AffixCountByRarity.TryGetValue(rarity, out var range))
            return 0;

        return _rng.Next(range.min, range.max + 1);
    }

    private List<RolledAffix> RollAffixes(List<ItemAffix> affixPool, int count)
    {
        var result = new List<RolledAffix>();
        if (affixPool.Count == 0 || count <= 0)
            return result;

        // Select random affixes without duplicates
        var availableAffixes = new List<ItemAffix>(affixPool);
        for (int i = 0; i < count && availableAffixes.Count > 0; i++)
        {
            var index = _rng.Next(availableAffixes.Count);
            var affix = availableAffixes[index];
            availableAffixes.RemoveAt(index);

            // Roll value for the affix
            var value = affix.RollValue(_rng);
            result.Add(new RolledAffix(affix.Type, value));
        }

        return result;
    }
}
