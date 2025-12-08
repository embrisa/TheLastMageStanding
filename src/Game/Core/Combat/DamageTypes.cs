using System;
using TheLastMageStanding.Game.Core.Ecs.Components;

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
/// Source category for a damage instance.
/// </summary>
internal enum DamageSource
{
    None = 0,
    Melee = 1,
    Projectile = 2,
    ContactDamage = 3,
    StatusEffect = 4,
    Environmental = 5,
}

/// <summary>
/// Complete damage information for a single damage instance.
/// </summary>
internal readonly struct DamageInfo
{
    public float BaseDamage { get; }
    public DamageType DamageType { get; }
    public DamageFlags Flags { get; }
    public DamageSource Source { get; }
    public StatusEffectData? StatusEffect { get; }

    public DamageInfo(
        float baseDamage,
        DamageType damageType = DamageType.Physical,
        DamageFlags flags = DamageFlags.CanCrit,
        DamageSource source = DamageSource.None,
        StatusEffectData? statusEffect = null)
    {
        BaseDamage = baseDamage;
        DamageType = damageType;
        Flags = flags;
        Source = source;
        StatusEffect = statusEffect;
    }

    public bool HasFlag(DamageFlags flag) => (Flags & flag) != 0;
    public bool HasType(DamageType type) => (DamageType & type) != 0;

    public DamageInfo WithStatusEffect(StatusEffectData effect) =>
        new(BaseDamage, DamageType, Flags, Source, effect);
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
    public DamageSource Source { get; }

    public DamageResult(
        float finalDamage,
        float baseBeforeMultipliers,
        bool isCritical,
        float damageReduction,
        DamageType damageType,
        DamageSource source)
    {
        FinalDamage = finalDamage;
        BaseBeforeMultipliers = baseBeforeMultipliers;
        IsCritical = isCritical;
        DamageReduction = damageReduction;
        DamageType = damageType;
        Source = source;
    }
}
