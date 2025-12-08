using System;
using System.Collections.Generic;

namespace TheLastMageStanding.Game.Core.Loot;

/// <summary>
/// Central registry for all item definitions in the game.
/// </summary>
public sealed class ItemRegistry
{
    private readonly Dictionary<string, ItemDefinition> _definitions = new();

    public ItemRegistry()
    {
        RegisterDefaultItems();
    }

    public bool TryGetDefinition(string id, out ItemDefinition? definition)
    {
        return _definitions.TryGetValue(id, out definition);
    }

    public void RegisterDefinition(ItemDefinition definition)
    {
        _definitions[definition.Id] = definition;
    }

    public Dictionary<string, ItemDefinition> GetAllDefinitions() => _definitions;

    private void RegisterDefaultItems()
    {
        // Weapons
        RegisterDefinition(new ItemDefinition(
            "weapon_staff",
            "Arcane Staff",
            ItemType.Weapon,
            EquipSlot.Weapon,
            ItemRarity.Common,
            new List<ItemAffix>
            {
                new(AffixType.PowerAdditive, 0.1f, 0.3f),
                new(AffixType.PowerMultiplicative, 1.05f, 1.15f),
                new(AffixType.CritChanceAdditive, 0.02f, 0.08f),
                new(AffixType.AttackSpeedAdditive, 0.05f, 0.15f),
                new(AffixType.CooldownReductionAdditive, 0.05f, 0.15f)
            }));

        RegisterDefinition(new ItemDefinition(
            "weapon_wand",
            "Mystic Wand",
            ItemType.Weapon,
            EquipSlot.Weapon,
            ItemRarity.Common,
            new List<ItemAffix>
            {
                new(AffixType.PowerAdditive, 0.08f, 0.25f),
                new(AffixType.AttackSpeedAdditive, 0.1f, 0.25f),
                new(AffixType.CritChanceAdditive, 0.03f, 0.1f),
                new(AffixType.CritMultiplierAdditive, 0.1f, 0.3f)
            }));

        // Armor
        RegisterDefinition(new ItemDefinition(
            "armor_robe",
            "Mage Robe",
            ItemType.Armor,
            EquipSlot.Armor,
            ItemRarity.Common,
            new List<ItemAffix>
            {
                new(AffixType.ArmorAdditive, 5f, 20f),
                new(AffixType.ArcaneResistAdditive, 5f, 20f),
                new(AffixType.PowerAdditive, 0.05f, 0.15f),
                new(AffixType.MoveSpeedAdditive, 0.05f, 0.15f)
            }));

        RegisterDefinition(new ItemDefinition(
            "armor_cloak",
            "Shadow Cloak",
            ItemType.Armor,
            EquipSlot.Armor,
            ItemRarity.Common,
            new List<ItemAffix>
            {
                new(AffixType.ArmorAdditive, 3f, 15f),
                new(AffixType.MoveSpeedAdditive, 0.1f, 0.25f),
                new(AffixType.CooldownReductionAdditive, 0.05f, 0.15f),
                new(AffixType.ArcaneResistAdditive, 3f, 15f)
            }));

        // Amulets
        RegisterDefinition(new ItemDefinition(
            "amulet_arcane",
            "Amulet of Arcane Power",
            ItemType.Accessory,
            EquipSlot.Amulet,
            ItemRarity.Common,
            new List<ItemAffix>
            {
                new(AffixType.PowerAdditive, 0.15f, 0.4f),
                new(AffixType.PowerMultiplicative, 1.05f, 1.2f),
                new(AffixType.ArcaneResistAdditive, 10f, 30f),
                new(AffixType.CooldownReductionAdditive, 0.1f, 0.2f)
            }));

        RegisterDefinition(new ItemDefinition(
            "amulet_power",
            "Amulet of Might",
            ItemType.Accessory,
            EquipSlot.Amulet,
            ItemRarity.Common,
            new List<ItemAffix>
            {
                new(AffixType.PowerAdditive, 0.2f, 0.5f),
                new(AffixType.CritMultiplierAdditive, 0.2f, 0.5f),
                new(AffixType.ArmorAdditive, 10f, 30f)
            }));

        // Rings
        RegisterDefinition(new ItemDefinition(
            "ring_crit",
            "Ring of Precision",
            ItemType.Accessory,
            EquipSlot.Ring1,
            ItemRarity.Common,
            new List<ItemAffix>
            {
                new(AffixType.CritChanceAdditive, 0.05f, 0.15f),
                new(AffixType.CritMultiplierAdditive, 0.1f, 0.3f),
                new(AffixType.PowerAdditive, 0.05f, 0.15f),
                new(AffixType.AttackSpeedAdditive, 0.05f, 0.15f)
            }));

        RegisterDefinition(new ItemDefinition(
            "ring_speed",
            "Ring of Swiftness",
            ItemType.Accessory,
            EquipSlot.Ring1,
            ItemRarity.Common,
            new List<ItemAffix>
            {
                new(AffixType.AttackSpeedAdditive, 0.1f, 0.25f),
                new(AffixType.MoveSpeedAdditive, 0.1f, 0.25f),
                new(AffixType.CooldownReductionAdditive, 0.05f, 0.15f),
                new(AffixType.PowerAdditive, 0.05f, 0.15f)
            }));
    }
}
