using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Config;

internal static class EliteModifierRegistry
{
    private static readonly Dictionary<EliteModifierType, EliteModifierDefinition> _definitions = new()
    {
        [EliteModifierType.ExtraProjectiles] = new EliteModifierDefinition(
            EliteModifierType.ExtraProjectiles,
            "Extra Projectiles",
            new TelegraphData(
                duration: float.MaxValue,
                color: new Color(255, 150, 70, 90),
                radius: 110f,
                offset: Vector2.Zero),
            new Color(255, 180, 80),
            RewardMultiplier: 1.25f,
            AllowStacking: false,
            SfxOnApply: "elite_extra_projectiles"),
        [EliteModifierType.Vampiric] = new EliteModifierDefinition(
            EliteModifierType.Vampiric,
            "Vampiric",
            new TelegraphData(
                duration: float.MaxValue,
                color: new Color(210, 40, 40, 90),
                radius: 80f,
                offset: Vector2.Zero),
            new Color(255, 80, 80),
            RewardMultiplier: 1.25f,
            AllowStacking: false,
            SfxOnApply: "elite_vampiric"),
        [EliteModifierType.ExplosiveDeath] = new EliteModifierDefinition(
            EliteModifierType.ExplosiveDeath,
            "Explosive Death",
            new TelegraphData(
                duration: float.MaxValue,
                color: new Color(255, 120, 40, 80),
                radius: 70f,
                offset: Vector2.Zero),
            new Color(255, 200, 120),
            RewardMultiplier: 1.3f,
            AllowStacking: false,
            SfxOnApply: "elite_explosive"),
        [EliteModifierType.Shielded] = new EliteModifierDefinition(
            EliteModifierType.Shielded,
            "Shielded",
            new TelegraphData(
                duration: float.MaxValue,
                color: new Color(80, 140, 255, 120),
                radius: 90f,
                offset: Vector2.Zero),
            new Color(120, 170, 255),
            RewardMultiplier: 1.25f,
            AllowStacking: false,
            SfxOnApply: "elite_shielded")
    };

    public static EliteModifierDefinition Get(EliteModifierType type) => _definitions[type];

    public static IEnumerable<EliteModifierDefinition> All => _definitions.Values;
}

internal sealed class EliteModifierConfig
{
    public EliteModifierConfig(
        Dictionary<EliteModifierType, int> unlockWaveByModifier,
        Dictionary<EliteModifierType, float> modifierWeights,
        int firstModifierWave,
        int secondModifierWave,
        int thirdModifierWave)
    {
        UnlockWaveByModifier = unlockWaveByModifier;
        ModifierWeights = modifierWeights;
        FirstModifierWave = firstModifierWave;
        SecondModifierWave = secondModifierWave;
        ThirdModifierWave = thirdModifierWave;
    }

    public Dictionary<EliteModifierType, int> UnlockWaveByModifier { get; }
    public Dictionary<EliteModifierType, float> ModifierWeights { get; }
    public int FirstModifierWave { get; }
    public int SecondModifierWave { get; }
    public int ThirdModifierWave { get; }

    public int GetMinModifierCount(int waveIndex)
    {
        if (waveIndex < FirstModifierWave)
        {
            return 0;
        }

        if (waveIndex >= ThirdModifierWave)
        {
            return 2;
        }

        return 1;
    }

    public int GetMaxModifierCount(int waveIndex)
    {
        if (waveIndex < FirstModifierWave)
        {
            return 0;
        }

        if (waveIndex < SecondModifierWave)
        {
            return 1;
        }

        if (waveIndex < ThirdModifierWave)
        {
            return 2;
        }

        return 3;
    }

    public List<EliteModifierType> RollModifiers(int waveIndex, Random rng, int? forcedCount = null)
    {
        var unlocked = GetUnlockedModifiers(waveIndex).ToList();
        if (unlocked.Count == 0)
        {
            return new List<EliteModifierType>();
        }

        var min = GetMinModifierCount(waveIndex);
        var max = GetMaxModifierCount(waveIndex);
        var count = forcedCount ?? rng.Next(min, max + 1);
        count = Math.Clamp(count, min, max);

        var result = new List<EliteModifierType>();
        var available = new List<EliteModifierType>(unlocked);

        while (result.Count < count && available.Count > 0)
        {
            var rolled = RollWeighted(available, rng);
            var definition = EliteModifierRegistry.Get(rolled);

            if (!definition.AllowStacking && result.Contains(rolled))
            {
                available.Remove(rolled);
                continue;
            }

            result.Add(rolled);

            if (!definition.AllowStacking)
            {
                available.Remove(rolled);
            }
        }

        return result;
    }

    public IEnumerable<EliteModifierType> GetUnlockedModifiers(int waveIndex)
    {
        foreach (var pair in UnlockWaveByModifier)
        {
            if (waveIndex >= pair.Value)
            {
                yield return pair.Key;
            }
        }
    }

    private EliteModifierType RollWeighted(List<EliteModifierType> candidates, Random rng)
    {
        var totalWeight = 0f;
        foreach (var candidate in candidates)
        {
            if (!ModifierWeights.TryGetValue(candidate, out var weight) || weight <= 0f)
            {
                continue;
            }

            totalWeight += weight;
        }

        if (totalWeight <= 0f)
        {
            return candidates[0];
        }

        var roll = rng.NextSingle() * totalWeight;
        var cursor = 0f;
        foreach (var candidate in candidates)
        {
            var weight = ModifierWeights.TryGetValue(candidate, out var w) ? w : 0f;
            if (weight <= 0f)
            {
                continue;
            }

            cursor += weight;
            if (roll <= cursor)
            {
                return candidate;
            }
        }

        return candidates[^1];
    }

    public static EliteModifierConfig Default => new(
        new Dictionary<EliteModifierType, int>
        {
            { EliteModifierType.ExtraProjectiles, 5 },
            { EliteModifierType.Vampiric, 7 },
            { EliteModifierType.ExplosiveDeath, 10 },
            { EliteModifierType.Shielded, 8 }
        },
        new Dictionary<EliteModifierType, float>
        {
            { EliteModifierType.ExtraProjectiles, 1.1f },
            { EliteModifierType.Vampiric, 1.0f },
            { EliteModifierType.ExplosiveDeath, 1.0f },
            { EliteModifierType.Shielded, 0.9f }
        },
        firstModifierWave: 5,
        secondModifierWave: 12,
        thirdModifierWave: 20);
}

