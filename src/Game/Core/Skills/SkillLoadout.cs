using System;
using TheLastMageStanding.Game.Core.MetaProgression;

namespace TheLastMageStanding.Game.Core.Skills;

internal readonly record struct SkillLoadout(
    SkillId Primary,
    SkillId Hotkey1,
    SkillId Hotkey2,
    SkillId Hotkey3,
    SkillId Hotkey4)
{
    public SkillId GetSlot(int slot) => slot switch
    {
        0 => Primary,
        1 => Hotkey1,
        2 => Hotkey2,
        3 => Hotkey3,
        4 => Hotkey4,
        _ => SkillId.None
    };

    public SkillLoadout SetSlot(int slot, SkillId skillId) => slot switch
    {
        0 => this with { Primary = skillId },
        1 => this with { Hotkey1 = skillId },
        2 => this with { Hotkey2 = skillId },
        3 => this with { Hotkey3 = skillId },
        4 => this with { Hotkey4 = skillId },
        _ => this
    };

    public static SkillLoadout FromEquippedSkills(EquippedSkills equipped) => new(
        Primary: equipped.PrimarySkill,
        Hotkey1: equipped.GetSkill(1),
        Hotkey2: equipped.GetSkill(2),
        Hotkey3: equipped.GetSkill(3),
        Hotkey4: equipped.GetSkill(4));

    public static EquippedSkills ApplyToEquippedSkills(EquippedSkills equipped, SkillLoadout loadout)
    {
        equipped.PrimarySkill = loadout.Primary == SkillId.None ? SkillId.Firebolt : loadout.Primary;
        equipped.SetSkill(1, loadout.Hotkey1);
        equipped.SetSkill(2, loadout.Hotkey2);
        equipped.SetSkill(3, loadout.Hotkey3);
        equipped.SetSkill(4, loadout.Hotkey4);
        return equipped;
    }

    public static SkillLoadout FromProfile(EquippedSkillsProfile profile)
    {
        var primary = ParseSkillId(profile.Primary, fallback: SkillId.Firebolt);
        return new SkillLoadout(
            Primary: primary,
            Hotkey1: ParseNullableSkillId(profile.Hotkey1),
            Hotkey2: ParseNullableSkillId(profile.Hotkey2),
            Hotkey3: ParseNullableSkillId(profile.Hotkey3),
            Hotkey4: ParseNullableSkillId(profile.Hotkey4));
    }

    public EquippedSkillsProfile ToProfile() => new()
    {
        Version = 1,
        Primary = (Primary == SkillId.None ? SkillId.Firebolt : Primary).ToString(),
        Hotkey1 = SerializeNullable(Hotkey1),
        Hotkey2 = SerializeNullable(Hotkey2),
        Hotkey3 = SerializeNullable(Hotkey3),
        Hotkey4 = SerializeNullable(Hotkey4),
    };

    private static string? SerializeNullable(SkillId skillId) => skillId == SkillId.None ? null : skillId.ToString();

    private static SkillId ParseNullableSkillId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SkillId.None;
        }

        return ParseSkillId(value, fallback: SkillId.None);
    }

    private static SkillId ParseSkillId(string value, SkillId fallback)
    {
        if (Enum.TryParse<SkillId>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }
}

