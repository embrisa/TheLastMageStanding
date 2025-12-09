using System.Collections.Generic;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Progression;

internal sealed class LevelUpChoiceConfig
{
    public IReadOnlyList<StatBoostChoiceTemplate> StatBoosts { get; }
    public IReadOnlyList<SkillModifierChoiceTemplate> SkillModifiers { get; }

    public LevelUpChoiceConfig(
        IReadOnlyList<StatBoostChoiceTemplate> statBoosts,
        IReadOnlyList<SkillModifierChoiceTemplate> skillModifiers)
    {
        StatBoosts = statBoosts;
        SkillModifiers = skillModifiers;
    }

    public static LevelUpChoiceConfig Default => new LevelUpChoiceConfig(
        new[]
        {
            new StatBoostChoiceTemplate("health-15", StatBoostType.MaxHealth, 15f, "+15 Max Health", "Increase max health by 15 and heal proportionally."),
            new StatBoostChoiceTemplate("damage-3", StatBoostType.AttackDamage, 3f, "+3 Attack Damage", "Increase base attack damage by 3."),
            new StatBoostChoiceTemplate("speed-8", StatBoostType.MoveSpeed, 8f, "+8 Move Speed", "Move 8 units/sec faster."),
            new StatBoostChoiceTemplate("armor-5", StatBoostType.Armor, 5f, "+5 Armor", "Gain 5 armor for damage reduction."),
            new StatBoostChoiceTemplate("power-0.15", StatBoostType.Power, 0.15f, "+0.15 Power", "Increase power multiplier by 0.15."),
            new StatBoostChoiceTemplate("crit-3", StatBoostType.CritChance, 0.03f, "+3% Crit Chance", "Increase critical strike chance by 3%."),
        },
        new[]
        {
            new SkillModifierChoiceTemplate("dmg-15", SkillModifierType.DamagePercent, 0.15f, 0, "+15% Damage", "Increase {0} damage by 15%."),
            new SkillModifierChoiceTemplate("cdr-10", SkillModifierType.CooldownReductionPercent, 0.10f, 0, "-10% Cooldown", "Reduce {0} cooldown by 10%."),
            new SkillModifierChoiceTemplate("aoe-20", SkillModifierType.AoePercent, 0.20f, 0, "+20% Area", "Increase {0} area/radius by 20%."),
            new SkillModifierChoiceTemplate("projectile-1", SkillModifierType.ProjectileCount, 0f, 1, "+1 Projectile", "Add 1 projectile to {0}."),
            new SkillModifierChoiceTemplate("pierce-1", SkillModifierType.PierceCount, 0f, 1, "+1 Pierce", "Add 1 pierce to {0}."),
            new SkillModifierChoiceTemplate("cast-10", SkillModifierType.CastSpeedPercent, 0.10f, 0, "+10% Cast Speed", "Reduce {0} cast time by 10%."),
        });
}

internal readonly record struct StatBoostChoiceTemplate(
    string Id,
    StatBoostType Type,
    float Amount,
    string Title,
    string Description);

internal readonly record struct SkillModifierChoiceTemplate(
    string Id,
    SkillModifierType Type,
    float Amount,
    int IntAmount,
    string Title,
    string Description);



