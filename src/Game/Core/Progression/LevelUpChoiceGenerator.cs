using System;
using System.Collections.Generic;
using System.Globalization;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Core.Progression;

/// <summary>
/// Builds the pool of available level-up choices and rolls a 3-option selection.
/// </summary>
internal sealed class LevelUpChoiceGenerator
{
    private readonly LevelUpChoiceConfig _config;
    private readonly SkillRegistry _skillRegistry;
    private readonly Random _rng = new();

    public LevelUpChoiceGenerator(LevelUpChoiceConfig config, SkillRegistry skillRegistry)
    {
        _config = config;
        _skillRegistry = skillRegistry;
    }

    public List<LevelUpChoice> GenerateChoices(EcsWorld world, Entity player)
    {
        var pool = new List<LevelUpChoice>();
        var ids = new HashSet<string>();

        AddStatChoices(pool, ids);
        AddSkillChoices(world, player, pool, ids);

        if (pool.Count <= 3)
        {
            return pool;
        }

        // Sample without replacement using a partial Fisher–Yates shuffle.
        for (var i = 0; i < 3; i++)
        {
            var j = _rng.Next(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        return new List<LevelUpChoice>(capacity: 3) { pool[0], pool[1], pool[2] };
    }

    private void AddStatChoices(List<LevelUpChoice> pool, HashSet<string> ids)
    {
        foreach (var template in _config.StatBoosts)
        {
            var id = $"stat:{template.Id}";
            if (!ids.Add(id))
            {
                continue;
            }

            pool.Add(new LevelUpChoice
            {
                Id = id,
                Kind = LevelUpChoiceKind.StatBoost,
                StatType = template.Type,
                StatAmount = template.Amount,
                Title = template.Title,
                Description = template.Description
            });
        }
    }

    private void AddSkillChoices(
        EcsWorld world,
        Entity player,
        List<LevelUpChoice> pool,
        HashSet<string> ids)
    {
        if (!world.TryGetComponent(player, out EquippedSkills equipped))
        {
            return;
        }

        var equippedSkills = GetEquippedSkills(equipped);
        foreach (var skillId in equippedSkills)
        {
            if (skillId == SkillId.None)
            {
                continue;
            }

            var skill = _skillRegistry.GetSkill(skillId);
            var skillName = skill?.Name ?? skillId.ToString();

            foreach (var template in _config.SkillModifiers)
            {
                var id = $"skill:{skillId}:{template.Id}";
                if (!ids.Add(id))
                {
                    continue;
                }

                pool.Add(new LevelUpChoice
                {
                    Id = id,
                    Kind = LevelUpChoiceKind.SkillModifier,
                    SkillId = skillId,
                    SkillModifierType = template.Type,
                    SkillModifierAmount = template.Amount,
                    SkillModifierIntAmount = template.IntAmount,
                    Title = $"{skillName}: {template.Title}",
                    Description = string.Format(CultureInfo.InvariantCulture, template.Description, skillName)
                });
            }
        }
    }

    private static IEnumerable<SkillId> GetEquippedSkills(EquippedSkills equipped)
    {
        yield return equipped.PrimarySkill;
        if (equipped.HotkeySkills == null)
        {
            yield break;
        }

        foreach (var kvp in equipped.HotkeySkills)
        {
            yield return kvp.Value;
        }
    }
}
