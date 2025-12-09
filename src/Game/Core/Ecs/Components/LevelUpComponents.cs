using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Core.Ecs.Components;

internal enum LevelUpChoiceKind
{
    StatBoost,
    SkillModifier
}

internal enum StatBoostType
{
    MaxHealth,
    AttackDamage,
    MoveSpeed,
    Armor,
    Power,
    CritChance
}

internal enum SkillModifierType
{
    DamagePercent,
    CooldownReductionPercent,
    AoePercent,
    ProjectileCount,
    PierceCount,
    CastSpeedPercent
}

/// <summary>
/// Choice data shown in the level-up UI.
/// </summary>
internal struct LevelUpChoice
{
    public string Id { get; set; }
    public LevelUpChoiceKind Kind { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    // Stat boost fields
    public StatBoostType StatType { get; set; }
    public float StatAmount { get; set; }

    // Skill modifier fields
    public SkillId SkillId { get; set; }
    public SkillModifierType SkillModifierType { get; set; }
    public float SkillModifierAmount { get; set; }
    public int SkillModifierIntAmount { get; set; }
}

/// <summary>
/// Current level-up choice state shown to the player.
/// Stored on the session entity to gate pause/input.
/// </summary>
internal struct LevelUpChoiceState
{
    public Entity Player { get; set; }
    public List<LevelUpChoice>? Choices { get; set; }
    public int SelectedIndex { get; set; }
    public int PendingLevels { get; set; }
    public bool IsOpen { get; set; }
}

/// <summary>
/// Simple history of selections during the current run.
/// </summary>
internal struct LevelUpChoiceHistory
{
    public List<string>? Selections { get; set; }
}

