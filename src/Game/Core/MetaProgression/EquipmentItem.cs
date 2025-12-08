namespace TheLastMageStanding.Game.Core.MetaProgression;

/// <summary>
/// Represents an equipment item that can be found during runs or purchased in the meta hub.
/// </summary>
public sealed class EquipmentItem
{
    /// <summary>
    /// Unique identifier for this equipment item.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the equipment.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of equipment (weapon, armor, accessory, etc.).
    /// </summary>
    public EquipmentType Type { get; set; }

    /// <summary>
    /// Rarity tier of the equipment.
    /// </summary>
    public EquipmentRarity Rarity { get; set; }

    /// <summary>
    /// Base damage bonus (for weapons).
    /// </summary>
    public float Damage { get; set; }

    /// <summary>
    /// Armor/defense bonus.
    /// </summary>
    public float Armor { get; set; }

    /// <summary>
    /// Health bonus.
    /// </summary>
    public float Health { get; set; }

    /// <summary>
    /// Movement speed multiplier (1.0 = no change, 1.1 = 10% faster).
    /// </summary>
    public float SpeedMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Critical hit chance bonus (0.05 = +5%).
    /// </summary>
    public float CritChance { get; set; }

    /// <summary>
    /// Critical hit damage multiplier (1.5 = 150% damage on crit).
    /// </summary>
    public float CritDamage { get; set; } = 1.5f;

    /// <summary>
    /// Reference to icon/sprite for UI display.
    /// </summary>
    public string IconPath { get; set; } = string.Empty;

    /// <summary>
    /// Description text for tooltips.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gold value for shop purchases.
    /// </summary>
    public int GoldCost { get; set; }
}

/// <summary>
/// Types of equipment that can be equipped.
/// </summary>
public enum EquipmentType
{
    Weapon,
    Armor,
    Accessory
}

/// <summary>
/// Rarity tiers for equipment.
/// </summary>
public enum EquipmentRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}
