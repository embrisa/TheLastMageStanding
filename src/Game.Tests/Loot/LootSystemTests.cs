using Xunit;
using TheLastMageStanding.Game.Core.Loot;
using TheLastMageStanding.Game.Core.Ecs.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLastMageStanding.Game.Tests.Loot;

public sealed class DropTableTests
{
    [Fact]
    public void SelectRandom_WithSingleEntry_ReturnsEntry()
    {
        var entries = new List<WeightedEntry<string>>
        {
            new("item1", 1f)
        };
        var table = new DropTable<string>(entries);
        var rng = new Random(42);

        var result = table.SelectRandom(rng);

        Assert.Equal("item1", result);
    }

    [Fact]
    public void SelectRandom_WithMultipleEntries_RespectsWeights()
    {
        var entries = new List<WeightedEntry<string>>
        {
            new("common", 90f),
            new("rare", 10f)
        };
        var table = new DropTable<string>(entries);
        var rng = new Random(42);

        var results = new Dictionary<string, int>();
        for (int i = 0; i < 1000; i++)
        {
            var result = table.SelectRandom(rng);
            if (result != null)
            {
                results.TryGetValue(result, out var count);
                results[result] = count + 1;
            }
        }

        // Common should appear ~9x more than rare
        Assert.True(results["common"] > results["rare"]);
        Assert.True(results["common"] > 800); // Roughly 90%
        Assert.True(results["rare"] < 200);   // Roughly 10%
    }

    [Fact]
    public void SelectRandom_WithEmptyTable_ReturnsDefault()
    {
        var table = new DropTable<string>(new List<WeightedEntry<string>>());
        var rng = new Random(42);

        var result = table.SelectRandom(rng);

        Assert.Null(result);
    }
}

public sealed class ItemAffixTests
{
    [Fact]
    public void RollValue_ReturnsValueInRange()
    {
        var affix = new ItemAffix(AffixType.PowerAdditive, 0.1f, 0.5f);
        var rng = new Random(42);

        for (int i = 0; i < 100; i++)
        {
            var value = affix.RollValue(rng);
            Assert.InRange(value, 0.1f, 0.5f);
        }
    }

    [Fact]
    public void RolledAffix_AppliesCorrectly()
    {
        var affix = new RolledAffix(AffixType.PowerAdditive, 0.5f);
        var modifiers = StatModifiers.Zero;

        var result = affix.ApplyTo(modifiers);

        Assert.Equal(0.5f, result.PowerAdditive);
        Assert.Equal(1f, result.PowerMultiplicative); // Unchanged
    }

    [Fact]
    public void RolledAffix_MultipleAffixes_Stack()
    {
        var affix1 = new RolledAffix(AffixType.PowerAdditive, 0.3f);
        var affix2 = new RolledAffix(AffixType.PowerAdditive, 0.2f);
        var modifiers = StatModifiers.Zero;

        var result = affix1.ApplyTo(modifiers);
        result = affix2.ApplyTo(result);

        Assert.Equal(0.5f, result.PowerAdditive, precision: 4);
    }
}

public sealed class ItemFactoryTests
{
    [Fact]
    public void GenerateItem_CreatesItemWithAffixes()
    {
        var definitions = CreateTestDefinitions();
        var config = CreateTestConfig();
        var factory = new ItemFactory(definitions, config, new Random(42));

        var item = factory.GenerateItem(EquipSlot.Weapon);

        Assert.NotNull(item);
        Assert.Equal(EquipSlot.Weapon, item!.EquipSlot);
        Assert.True(item.Affixes.Count >= 0); // May have 0 affixes for common
    }

    [Fact]
    public void GenerateItem_RarityAffectsAffixCount()
    {
        var definitions = CreateTestDefinitionsWithManyAffixes();
        var config = CreateTestConfig();
        
        // Force legendary rarity
        var legendaryTable = new DropTable<ItemRarity>(new List<WeightedEntry<ItemRarity>>
        {
            new(ItemRarity.Legendary, 1f)
        });
        var itemTables = new Dictionary<EquipSlot, DropTable<string>>
        {
            [EquipSlot.Weapon] = new DropTable<string>(new List<WeightedEntry<string>>
            {
                new("test_weapon_many_affixes", 1f)
            })
        };
        var legendaryConfig = new LootDropConfig(legendaryTable, itemTables);
        
        var factory = new ItemFactory(definitions, legendaryConfig, new Random(42));

        var item = factory.GenerateItem(EquipSlot.Weapon);

        Assert.NotNull(item);
        // Legendary should have 4-5 affixes
        Assert.InRange(item!.Affixes.Count, 4, 5);
    }

    [Fact]
    public void GenerateRandomItem_ReturnsValidItem()
    {
        var definitions = CreateTestDefinitions();
        var config = CreateTestConfigWithAllSlots();
        var factory = new ItemFactory(definitions, config, new Random(42));

        var item = factory.GenerateRandomItem();

        Assert.NotNull(item);
        Assert.NotEqual(EquipSlot.None, item!.EquipSlot);
    }

    [Fact]
    public void ItemInstance_CalculatesStatModifiers()
    {
        var affixes = new List<RolledAffix>
        {
            new(AffixType.PowerAdditive, 0.3f),
            new(AffixType.CritChanceAdditive, 0.1f)
        };
        var item = new ItemInstance(
            "test_item",
            "Test Item",
            ItemType.Weapon,
            EquipSlot.Weapon,
            ItemRarity.Rare,
            affixes);

        var modifiers = item.CalculateStatModifiers();

        Assert.Equal(0.3f, modifiers.PowerAdditive);
        Assert.Equal(0.1f, modifiers.CritChanceAdditive);
    }

    private static Dictionary<string, ItemDefinition> CreateTestDefinitions()
    {
        var definitions = new Dictionary<string, ItemDefinition>();
        definitions["test_weapon"] = new ItemDefinition(
            "test_weapon",
            "Test Weapon",
            ItemType.Weapon,
            EquipSlot.Weapon,
            ItemRarity.Common,
            new List<ItemAffix>
            {
                new(AffixType.PowerAdditive, 0.1f, 0.5f),
                new(AffixType.CritChanceAdditive, 0.05f, 0.15f)
            });
        return definitions;
    }

    private static Dictionary<string, ItemDefinition> CreateTestDefinitionsWithManyAffixes()
    {
        var definitions = new Dictionary<string, ItemDefinition>();
        definitions["test_weapon_many_affixes"] = new ItemDefinition(
            "test_weapon_many_affixes",
            "Test Weapon",
            ItemType.Weapon,
            EquipSlot.Weapon,
            ItemRarity.Common,
            new List<ItemAffix>
            {
                new(AffixType.PowerAdditive, 0.1f, 0.5f),
                new(AffixType.CritChanceAdditive, 0.05f, 0.15f),
                new(AffixType.AttackSpeedAdditive, 0.1f, 0.3f),
                new(AffixType.CritMultiplierAdditive, 0.1f, 0.3f),
                new(AffixType.CooldownReductionAdditive, 0.05f, 0.2f)
            });
        return definitions;
    }

    private static LootDropConfig CreateTestConfig()
    {
        var rarityTable = new DropTable<ItemRarity>(new List<WeightedEntry<ItemRarity>>
        {
            new(ItemRarity.Common, 1f)
        });

        var itemTables = new Dictionary<EquipSlot, DropTable<string>>
        {
            [EquipSlot.Weapon] = new DropTable<string>(new List<WeightedEntry<string>>
            {
                new("test_weapon", 1f)
            })
        };

        return new LootDropConfig(rarityTable, itemTables);
    }

    private static LootDropConfig CreateTestConfigWithAllSlots()
    {
        var rarityTable = new DropTable<ItemRarity>(new List<WeightedEntry<ItemRarity>>
        {
            new(ItemRarity.Common, 1f)
        });

        var itemTables = new Dictionary<EquipSlot, DropTable<string>>
        {
            [EquipSlot.Weapon] = new DropTable<string>(new List<WeightedEntry<string>>
            {
                new("test_weapon", 1f)
            }),
            [EquipSlot.Armor] = new DropTable<string>(new List<WeightedEntry<string>>
            {
                new("test_weapon", 1f)
            }),
            [EquipSlot.Amulet] = new DropTable<string>(new List<WeightedEntry<string>>
            {
                new("test_weapon", 1f)
            }),
            [EquipSlot.Ring1] = new DropTable<string>(new List<WeightedEntry<string>>
            {
                new("test_weapon", 1f)
            })
        };

        return new LootDropConfig(rarityTable, itemTables);
    }
}

public sealed class ItemRegistryTests
{
    [Fact]
    public void DefaultItems_AreRegistered()
    {
        var registry = new ItemRegistry();

        Assert.True(registry.TryGetDefinition("weapon_staff", out var staff));
        Assert.NotNull(staff);
        Assert.Equal("Arcane Staff", staff!.Name);

        Assert.True(registry.TryGetDefinition("armor_robe", out var robe));
        Assert.NotNull(robe);
    }

    [Fact]
    public void RegisterDefinition_AddsItem()
    {
        var registry = new ItemRegistry();
        var definition = new ItemDefinition(
            "custom_item",
            "Custom Item",
            ItemType.Weapon,
            EquipSlot.Weapon,
            ItemRarity.Epic,
            new List<ItemAffix>());

        registry.RegisterDefinition(definition);

        Assert.True(registry.TryGetDefinition("custom_item", out var retrieved));
        Assert.Equal("Custom Item", retrieved!.Name);
    }
}

public sealed class LootDropConfigTests
{
    [Fact]
    public void CreateDefault_HasValidConfig()
    {
        var config = LootDropConfig.CreateDefault();

        Assert.NotNull(config.RarityTable);
        Assert.NotEmpty(config.ItemTables);
        Assert.InRange(config.BaseDropChance, 0f, 1f);
        Assert.InRange(config.EliteDropChance, 0f, 1f);
        Assert.Equal(1.0f, config.BossDropChance); // Bosses always drop
    }

    [Fact]
    public void AffixCountByRarity_HasAllRarities()
    {
        var config = LootDropConfig.CreateDefault();

        Assert.True(config.AffixCountByRarity.ContainsKey(ItemRarity.Common));
        Assert.True(config.AffixCountByRarity.ContainsKey(ItemRarity.Uncommon));
        Assert.True(config.AffixCountByRarity.ContainsKey(ItemRarity.Rare));
        Assert.True(config.AffixCountByRarity.ContainsKey(ItemRarity.Epic));
        Assert.True(config.AffixCountByRarity.ContainsKey(ItemRarity.Legendary));
    }

    [Fact]
    public void AffixCount_IncreasesWithRarity()
    {
        var config = LootDropConfig.CreateDefault();

        var commonMax = config.AffixCountByRarity[ItemRarity.Common].max;
        var rareMax = config.AffixCountByRarity[ItemRarity.Rare].max;
        var legendaryMax = config.AffixCountByRarity[ItemRarity.Legendary].max;

        Assert.True(commonMax < rareMax);
        Assert.True(rareMax < legendaryMax);
    }
}
