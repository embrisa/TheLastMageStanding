using System;
using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Loot;

/// <summary>
/// Rarity tiers for loot drops. Higher rarity = better stats and more affixes.
/// </summary>
public enum ItemRarity
{
    Common = 0,     // White - basic items, 0-1 affixes
    Uncommon = 1,   // Green - decent items, 1-2 affixes
    Rare = 2,       // Blue - good items, 2-3 affixes
    Epic = 3,       // Purple - great items, 3-4 affixes
    Legendary = 4   // Orange - best items, 4-5 affixes
}

/// <summary>
/// Equipment slot types.
/// </summary>
public enum EquipSlot
{
    None = 0,
    Weapon = 1,     // Main hand weapon
    Armor = 2,      // Chest armor
    Amulet = 3,     // Neck slot
    Ring1 = 4,      // First ring slot
    Ring2 = 5       // Second ring slot
}

/// <summary>
/// Item type categories.
/// </summary>
public enum ItemType
{
    None = 0,
    Weapon = 1,
    Armor = 2,
    Accessory = 3
}

/// <summary>
/// Single stat affix that can roll on items.
/// </summary>
public readonly struct ItemAffix
{
    public AffixType Type { get; init; }
    public float MinValue { get; init; }
    public float MaxValue { get; init; }

    public ItemAffix(AffixType type, float minValue, float maxValue)
    {
        Type = type;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    /// <summary>
    /// Roll a random value for this affix.
    /// </summary>
    public float RollValue(Random rng)
    {
        return MinValue + (float)rng.NextDouble() * (MaxValue - MinValue);
    }
}

/// <summary>
/// Types of affixes that can roll on items.
/// Maps directly to stat modifiers.
/// </summary>
public enum AffixType
{
    // Offensive
    PowerAdditive,
    PowerMultiplicative,
    AttackSpeedAdditive,
    AttackSpeedMultiplicative,
    CritChanceAdditive,
    CritMultiplierAdditive,
    CooldownReductionAdditive,
    
    // Defensive
    ArmorAdditive,
    ArmorMultiplicative,
    ArcaneResistAdditive,
    ArcaneResistMultiplicative,
    
    // Movement
    MoveSpeedAdditive,
    MoveSpeedMultiplicative
}

/// <summary>
/// Rolled affix with actual value.
/// </summary>
public readonly struct RolledAffix
{
    public AffixType Type { get; init; }
    public float Value { get; init; }

    public RolledAffix(AffixType type, float value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    /// Apply this affix to stat modifiers.
    /// </summary>
    internal StatModifiers ApplyTo(StatModifiers modifiers)
    {
        return Type switch
        {
            AffixType.PowerAdditive => modifiers with { PowerAdditive = modifiers.PowerAdditive + Value },
            AffixType.PowerMultiplicative => modifiers with { PowerMultiplicative = modifiers.PowerMultiplicative * Value },
            AffixType.AttackSpeedAdditive => modifiers with { AttackSpeedAdditive = modifiers.AttackSpeedAdditive + Value },
            AffixType.AttackSpeedMultiplicative => modifiers with { AttackSpeedMultiplicative = modifiers.AttackSpeedMultiplicative * Value },
            AffixType.CritChanceAdditive => modifiers with { CritChanceAdditive = modifiers.CritChanceAdditive + Value },
            AffixType.CritMultiplierAdditive => modifiers with { CritMultiplierAdditive = modifiers.CritMultiplierAdditive + Value },
            AffixType.CooldownReductionAdditive => modifiers with { CooldownReductionAdditive = modifiers.CooldownReductionAdditive + Value },
            AffixType.ArmorAdditive => modifiers with { ArmorAdditive = modifiers.ArmorAdditive + Value },
            AffixType.ArmorMultiplicative => modifiers with { ArmorMultiplicative = modifiers.ArmorMultiplicative * Value },
            AffixType.ArcaneResistAdditive => modifiers with { ArcaneResistAdditive = modifiers.ArcaneResistAdditive + Value },
            AffixType.ArcaneResistMultiplicative => modifiers with { ArcaneResistMultiplicative = modifiers.ArcaneResistMultiplicative * Value },
            AffixType.MoveSpeedAdditive => modifiers with { MoveSpeedAdditive = modifiers.MoveSpeedAdditive + Value },
            AffixType.MoveSpeedMultiplicative => modifiers with { MoveSpeedMultiplicative = modifiers.MoveSpeedMultiplicative * Value },
            _ => modifiers
        };
    }
}

/// <summary>
/// Base item definition with affix pools.
/// </summary>
public sealed class ItemDefinition
{
    public string Id { get; }
    public string Name { get; }
    public ItemType ItemType { get; }
    public EquipSlot EquipSlot { get; }
    public ItemRarity BaseRarity { get; }
    public List<ItemAffix> AffixPool { get; }

    public ItemDefinition(
        string id,
        string name,
        ItemType itemType,
        EquipSlot equipSlot,
        ItemRarity baseRarity,
        List<ItemAffix> affixPool)
    {
        Id = id;
        Name = name;
        ItemType = itemType;
        EquipSlot = equipSlot;
        BaseRarity = baseRarity;
        AffixPool = affixPool;
    }
}

/// <summary>
/// Item instance with rolled affixes.
/// </summary>
public sealed class ItemInstance
{
    public string DefinitionId { get; }
    public string Name { get; }
    public ItemType ItemType { get; }
    public EquipSlot EquipSlot { get; }
    public ItemRarity Rarity { get; }
    public List<RolledAffix> Affixes { get; }
    
    // Unique instance ID for tracking
    public Guid InstanceId { get; }

    public ItemInstance(
        string definitionId,
        string name,
        ItemType itemType,
        EquipSlot equipSlot,
        ItemRarity rarity,
        List<RolledAffix> affixes)
    {
        DefinitionId = definitionId;
        Name = name;
        ItemType = itemType;
        EquipSlot = equipSlot;
        Rarity = rarity;
        Affixes = affixes;
        InstanceId = Guid.NewGuid();
    }

    /// <summary>
    /// Calculate total stat modifiers from all affixes.
    /// </summary>
    internal StatModifiers CalculateStatModifiers()
    {
        var modifiers = StatModifiers.Zero;
        foreach (var affix in Affixes)
        {
            modifiers = affix.ApplyTo(modifiers);
        }
        return modifiers;
    }

    /// <summary>
    /// Get display color for rarity.
    /// </summary>
    public Microsoft.Xna.Framework.Color GetRarityColor()
    {
        return Rarity switch
        {
            ItemRarity.Common => Microsoft.Xna.Framework.Color.White,
            ItemRarity.Uncommon => Microsoft.Xna.Framework.Color.LightGreen,
            ItemRarity.Rare => Microsoft.Xna.Framework.Color.CornflowerBlue,
            ItemRarity.Epic => Microsoft.Xna.Framework.Color.MediumPurple,
            ItemRarity.Legendary => Microsoft.Xna.Framework.Color.Orange,
            _ => Microsoft.Xna.Framework.Color.Gray
        };
    }
}
