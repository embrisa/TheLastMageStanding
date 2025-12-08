using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

internal enum EliteModifierType
{
    ExtraProjectiles,
    Vampiric,
    ExplosiveDeath,
    Shielded
}

internal readonly record struct EliteModifierDefinition(
    EliteModifierType Type,
    string DisplayName,
    TelegraphData? AuraOrIndicator,
    Color TintOverlay,
    float RewardMultiplier,
    bool AllowStacking = false,
    string? SfxOnApply = null,
    string? SfxOnTrigger = null);

internal struct EliteModifierData
{
    public EliteModifierData(List<EliteModifierType> modifiers)
    {
        ActiveModifiers = modifiers;
    }

    public List<EliteModifierType> ActiveModifiers { get; set; }

    public bool HasModifier(EliteModifierType type) => ActiveModifiers != null && ActiveModifiers.Contains(type);

    public int Count => ActiveModifiers?.Count ?? 0;
}

internal struct EliteShield
{
    public float Current;
    public float Max;
    public float RegenCooldown;
    public float RegenRate;
    public float CooldownTimer;

    public float Ratio => Max <= 0f ? 0f : Current / Max;
}

internal struct PendingExplosion
{
    public float RemainingTime;
    public float Radius;
    public float Damage;
    public Faction SourceFaction;
}

