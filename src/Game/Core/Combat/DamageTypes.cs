using System;

namespace TheLastMageStanding.Game.Core.Combat;

/// <summary>
/// Damage type flags. Can be combined for hybrid damage.
/// </summary>
[Flags]
internal enum DamageType
{
    None = 0,
    Physical = 1 << 0,
    Arcane = 1 << 1,
    True = 1 << 2, // Bypasses all resistances
}

/// <summary>
/// Additional flags for damage behavior.
/// </summary>
[Flags]
internal enum DamageFlags
{
    None = 0,
    CanCrit = 1 << 0,
    IgnoreArmor = 1 << 1,
    IgnoreResist = 1 << 2,
}

/// <summary>
/// Complete damage information for a single damage instance.
/// </summary>
internal readonly struct DamageInfo
{
    public float BaseDamage { get; }
    public DamageType DamageType { get; }
    public DamageFlags Flags { get; }

    public DamageInfo(float baseDamage, DamageType damageType = DamageType.Physical, DamageFlags flags = DamageFlags.CanCrit)
    {
        BaseDamage = baseDamage;
        DamageType = damageType;
        Flags = flags;
    }

    public bool HasFlag(DamageFlags flag) => (Flags & flag) != 0;
    public bool HasType(DamageType type) => (DamageType & type) != 0;
}

/// <summary>
/// Result of a damage calculation including all modifiers.
/// </summary>
internal readonly struct DamageResult
{
    public float FinalDamage { get; }
    public float BaseBeforeMultipliers { get; }
    public bool IsCritical { get; }
    public float DamageReduction { get; }
    public DamageType DamageType { get; }

    public DamageResult(float finalDamage, float baseBeforeMultipliers, bool isCritical, float damageReduction, DamageType damageType)
    {
        FinalDamage = finalDamage;
        BaseBeforeMultipliers = baseBeforeMultipliers;
        IsCritical = isCritical;
        DamageReduction = damageReduction;
        DamageType = damageType;
    }
}
