using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace TheLastMageStanding.Game.Core.Ecs.Config;

internal readonly record struct EnemyVisualDefinition(
    string IdleAsset,
    string RunAsset,
    Vector2 Origin,
    float Scale,
    Color Tint,
    int FrameSize);

internal readonly record struct EnemyArchetype(
    string Id,
    float MoveSpeed,
    float MaxHealth,
    float Damage,
    float AttackCooldownSeconds,
    float AttackRange,
    float CollisionRadius,
    float Mass,
    EnemyVisualDefinition Visual,
    RangedAttackDefinition? RangedAttack = null);

/// <summary>
/// Optional configuration for ranged enemies that fire projectiles.
/// </summary>
internal readonly record struct RangedAttackDefinition(
    float ProjectileSpeed,
    float ProjectileDamage,
    float OptimalRange,
    float WindupSeconds);

internal readonly record struct EnemySpawnProfile(EnemyArchetype Archetype, float Weight, int UnlockWave);

internal sealed class EnemyWaveConfig
{
    public EnemyWaveConfig(
        float waveIntervalSeconds,
        int baseEnemiesPerWave,
        int enemiesPerWaveGrowth,
        float spawnRadiusMin,
        float spawnRadiusMax,
        int maxActiveEnemies,
        IReadOnlyList<EnemySpawnProfile> enemyProfiles)
    {
        if (enemyProfiles.Count == 0)
        {
            throw new ArgumentException("At least one enemy profile is required.", nameof(enemyProfiles));
        }

        WaveIntervalSeconds = waveIntervalSeconds;
        BaseEnemiesPerWave = baseEnemiesPerWave;
        EnemiesPerWaveGrowth = enemiesPerWaveGrowth;
        SpawnRadiusMin = spawnRadiusMin;
        SpawnRadiusMax = spawnRadiusMax;
        MaxActiveEnemies = maxActiveEnemies;
        EnemyProfiles = enemyProfiles;
    }

    public float WaveIntervalSeconds { get; }
    public int BaseEnemiesPerWave { get; }
    public int EnemiesPerWaveGrowth { get; }
    public float SpawnRadiusMin { get; }
    public float SpawnRadiusMax { get; }
    public int MaxActiveEnemies { get; }
    public IReadOnlyList<EnemySpawnProfile> EnemyProfiles { get; }

    public EnemyArchetype ChooseArchetype(int waveIndex, Random random)
    {
        var available = EnemyProfiles.Where(profile => waveIndex >= profile.UnlockWave && profile.Weight > 0f).ToList();
        if (available.Count == 0)
        {
            return EnemyProfiles[0].Archetype;
        }

        var totalWeight = available.Sum(profile => profile.Weight);
        var roll = random.NextSingle() * totalWeight;
        var cursor = 0f;
        foreach (var profile in available)
        {
            cursor += profile.Weight;
            if (roll <= cursor)
            {
                return profile.Archetype;
            }
        }

        return available[^1].Archetype;
    }

    public static EnemyWaveConfig Default =>
        new(
            waveIntervalSeconds: 5f,
            baseEnemiesPerWave: 3,
            enemiesPerWaveGrowth: 1,
            spawnRadiusMin: 260f,
            spawnRadiusMax: 420f,
            maxActiveEnemies: 40,
            enemyProfiles: new[]
            {
                new EnemySpawnProfile(
                    Archetype: BaseHexer(),
                    Weight: 1f,
                    UnlockWave: 1),
                new EnemySpawnProfile(
                    Archetype: ScoutHexer(),
                    Weight: 1.2f,
                    UnlockWave: 2),
                new EnemySpawnProfile(
                    Archetype: BoneMage(),
                    Weight: 0.8f,
                    UnlockWave: 3),
            });

    private static EnemyArchetype BaseHexer() =>
        new(
            Id: "bone_hexer",
            MoveSpeed: 80f,
            MaxHealth: 24f,
            Damage: 8f,
            AttackCooldownSeconds: 1.2f,
            AttackRange: 7f,
            CollisionRadius: 5.5f,
            Mass: 0.6f,
            Visual: new EnemyVisualDefinition(
                IdleAsset: "Sprites/enemies/BoneHexer/Idle",
                RunAsset: "Sprites/enemies/BoneHexer/Run",
                Origin: new Vector2(64f, 96f),
                Scale: 1f,
                Tint: Color.White,
                FrameSize: 128));

    private static EnemyArchetype ScoutHexer() =>
        new(
            Id: "bone_scout",
            MoveSpeed: 120f,
            MaxHealth: 16f,
            Damage: 5f,
            AttackCooldownSeconds: 0.9f,
            AttackRange: 7f,
            CollisionRadius: 5f,
            Mass: 0.4f,
            Visual: new EnemyVisualDefinition(
                IdleAsset: "Sprites/enemies/BoneHexer/Idle",
                RunAsset: "Sprites/enemies/BoneHexer/Run",
                Origin: new Vector2(64f, 96f),
                Scale: 0.92f,
                Tint: Color.LightSkyBlue,
                FrameSize: 128));

    private static EnemyArchetype BoneMage() =>
        new(
            Id: "bone_mage",
            MoveSpeed: 65f,
            MaxHealth: 20f,
            Damage: 0f, // Ranged enemy - damage comes from projectiles
            AttackCooldownSeconds: 2.5f,
            AttackRange: 150f, // Not used for ranged - OptimalRange is used instead
            CollisionRadius: 5.5f,
            Mass: 0.5f,
            Visual: new EnemyVisualDefinition(
                IdleAsset: "Sprites/enemies/BoneHexer/Idle",
                RunAsset: "Sprites/enemies/BoneHexer/Run",
                Origin: new Vector2(64f, 96f),
                Scale: 1.05f,
                Tint: new Color(200, 100, 255), // Purple tint for mages
                FrameSize: 128),
            RangedAttack: new RangedAttackDefinition(
                ProjectileSpeed: 180f,
                ProjectileDamage: 12f,
                OptimalRange: 140f,
                WindupSeconds: 0.6f));
}


