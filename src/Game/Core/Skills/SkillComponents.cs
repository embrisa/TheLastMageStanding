using System.Collections.Generic;

namespace TheLastMageStanding.Game.Core.Skills;

/// <summary>
/// Component tracking equipped skills for an entity (typically player).
/// </summary>
internal struct EquippedSkills
{
    /// <summary>
    /// Primary attack skill (left-click or main attack button).
    /// </summary>
    public SkillId PrimarySkill { get; set; }
    
    /// <summary>
    /// Secondary skills bound to hotkeys (1-4).
    /// </summary>
    public Dictionary<int, SkillId> HotkeySkills { get; set; }

    /// <summary>
    /// When true, skill loadout is locked and cannot be changed (e.g., during Stage runs).
    /// </summary>
    public bool IsLocked { get; set; }

    public EquippedSkills()
    {
        PrimarySkill = SkillId.Firebolt; // Default starting skill
        HotkeySkills = new Dictionary<int, SkillId>
        {
            [1] = SkillId.None,
            [2] = SkillId.None,
            [3] = SkillId.None,
            [4] = SkillId.None
        };
        IsLocked = false;
    }

    public SkillId GetSkill(int slot)
    {
        return slot == 0 ? PrimarySkill : HotkeySkills.GetValueOrDefault(slot, SkillId.None);
    }

    public void SetSkill(int slot, SkillId skillId)
    {
        if (slot == 0)
        {
            PrimarySkill = skillId;
        }
        else if (slot >= 1 && slot <= 4)
        {
            HotkeySkills[slot] = skillId;
        }
    }
}

/// <summary>
/// Component tracking skill cooldowns for an entity.
/// </summary>
internal struct SkillCooldowns
{
    /// <summary>
    /// Remaining cooldown time in seconds for each skill.
    /// </summary>
    public Dictionary<SkillId, float> Cooldowns { get; set; }

    public SkillCooldowns()
    {
        Cooldowns = new Dictionary<SkillId, float>();
    }

    public float GetCooldown(SkillId skillId)
    {
        return Cooldowns.GetValueOrDefault(skillId, 0f);
    }

    public void SetCooldown(SkillId skillId, float cooldownSeconds)
    {
        Cooldowns[skillId] = cooldownSeconds;
    }

    public bool IsOnCooldown(SkillId skillId)
    {
        return GetCooldown(skillId) > 0f;
    }

    public void TickCooldown(SkillId skillId, float deltaSeconds)
    {
        if (Cooldowns.TryGetValue(skillId, out var current) && current > 0f)
        {
            Cooldowns[skillId] = System.Math.Max(0f, current - deltaSeconds);
        }
    }
}

/// <summary>
/// Component holding skill-specific modifiers from perks, talents, and equipment.
/// </summary>
internal struct PlayerSkillModifiers
{
    /// <summary>
    /// Global modifiers applied to all skills.
    /// </summary>
    public SkillModifiers GlobalModifiers { get; set; }
    
    /// <summary>
    /// Element-specific modifiers (e.g., +20% Fire damage).
    /// </summary>
    public Dictionary<SkillElement, SkillModifiers> ElementModifiers { get; set; }
    
    /// <summary>
    /// Skill-specific modifiers (e.g., Firebolt +1 projectile).
    /// </summary>
    public Dictionary<SkillId, SkillModifiers> SkillSpecificModifiers { get; set; }
    
    /// <summary>
    /// Dirty flag to trigger recalculation.
    /// </summary>
    public bool IsDirty { get; set; }

    public PlayerSkillModifiers()
    {
        GlobalModifiers = SkillModifiers.Zero;
        ElementModifiers = new Dictionary<SkillElement, SkillModifiers>();
        SkillSpecificModifiers = new Dictionary<SkillId, SkillModifiers>();
        IsDirty = true;
    }

    /// <summary>
    /// Get combined modifiers for a specific skill.
    /// Stacking order: global → element → skill-specific.
    /// </summary>
    public SkillModifiers GetModifiersForSkill(SkillId skillId, SkillElement element)
    {
        var result = GlobalModifiers;
        
        if (ElementModifiers.TryGetValue(element, out var elementMods))
        {
            result = SkillModifiers.Combine(result, elementMods);
        }
        
        if (SkillSpecificModifiers.TryGetValue(skillId, out var skillMods))
        {
            result = SkillModifiers.Combine(result, skillMods);
        }
        
        return result;
    }

    public static void MarkDirty(ref PlayerSkillModifiers modifiers)
    {
        modifiers.IsDirty = true;
    }
}

/// <summary>
/// Skill modifiers granted by in-run level-up choices.
/// Reset on stage restart.
/// </summary>
internal struct LevelUpSkillModifiers
{
    public Dictionary<SkillId, SkillModifiers> SkillSpecificModifiers { get; set; }

    public LevelUpSkillModifiers()
    {
        SkillSpecificModifiers = new Dictionary<SkillId, SkillModifiers>();
    }
}

/// <summary>
/// Component for entities currently casting a skill.
/// </summary>
internal struct SkillCasting
{
    public SkillId SkillId { get; set; }
    public float CastTimeRemaining { get; set; }
    public float TotalCastTime { get; set; }
    
    public SkillCasting(SkillId skillId, float castTime)
    {
        SkillId = skillId;
        CastTimeRemaining = castTime;
        TotalCastTime = castTime;
    }

    public float CastProgress => TotalCastTime > 0f ? 1f - (CastTimeRemaining / TotalCastTime) : 1f;
    public bool IsComplete => CastTimeRemaining <= 0f;
}
