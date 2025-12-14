using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Config;

internal readonly record struct EnemyVisualDefinition(
    string IdleAsset,
    string RunAsset,
    Vector2 Origin,
    float Scale,
    Color Tint,
    int FrameSize);

/// <summary>
/// Defines the tier of an enemy for gameplay and reward purposes.
/// </summary>
internal enum EnemyTier
{
    Normal,
    Elite,
    Boss
}

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
    EnemyTier Tier = EnemyTier.Normal,
    RangedAttackDefinition? RangedAttack = null,
    AiRoleConfig? RoleConfig = null);

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
        IReadOnlyList<EnemySpawnProfile> enemyProfiles,
        EliteModifierConfig? modifierConfig = null)
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
        ModifierConfig = modifierConfig ?? EliteModifierConfig.Default;
    }

    public float WaveIntervalSeconds { get; }
    public int BaseEnemiesPerWave { get; }
    public int EnemiesPerWaveGrowth { get; }
    public float SpawnRadiusMin { get; }
    public float SpawnRadiusMax { get; }
    public int MaxActiveEnemies { get; }
    public IReadOnlyList<EnemySpawnProfile> EnemyProfiles { get; }
    public EliteModifierConfig ModifierConfig { get; }

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
                    Weight: 0.8f,
                    UnlockWave: 1),
                new EnemySpawnProfile(
                    Archetype: ScoutHexer(),
                    Weight: 1.2f,
                    UnlockWave: 2),
                new EnemySpawnProfile(
                    Archetype: BoneMage(),
                    Weight: 0.8f,
                    UnlockWave: 3),
                new EnemySpawnProfile(
                    Archetype: ChargerHexer(),
                    Weight: 0.6f,
                    UnlockWave: 4),
                new EnemySpawnProfile(
                    Archetype: ProtectorHexer(),
                    Weight: 0.4f,
                    UnlockWave: 6),
                new EnemySpawnProfile(
                    Archetype: BufferHexer(),
                    Weight: 0.4f,
                    UnlockWave: 6),
                new EnemySpawnProfile(
                    Archetype: EliteHexer(),
                    Weight: 0.3f,
                    UnlockWave: 5),
                new EnemySpawnProfile(
                    Archetype: SkeletonBoss(),
                    Weight: 0.15f,
                    UnlockWave: 10),
            },
            modifierConfig: EliteModifierConfig.Default);

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

    private static EnemyArchetype ChargerHexer() =>
        new(
            Id: "charger_hexer",
            MoveSpeed: 110f,
            MaxHealth: 26f,
            Damage: 10f,
            AttackCooldownSeconds: 1.2f,
            AttackRange: 12f,
            CollisionRadius: 5.5f,
            Mass: 0.8f,
            Visual: new EnemyVisualDefinition(
                IdleAsset: "Sprites/enemies/BoneHexer/Idle",
                RunAsset: "Sprites/enemies/BoneHexer/Run",
                Origin: new Vector2(64f, 96f),
                Scale: 1.05f,
                Tint: new Color(255, 80, 80),
                FrameSize: 128),
            RoleConfig: ChargerRole());

    private static EnemyArchetype ProtectorHexer() =>
        new(
            Id: "protector_hexer",
            MoveSpeed: 85f,
            MaxHealth: 28f,
            Damage: 6f,
            AttackCooldownSeconds: 1.4f,
            AttackRange: 7f,
            CollisionRadius: 6f,
            Mass: 0.9f,
            Visual: new EnemyVisualDefinition(
                IdleAsset: "Sprites/enemies/BoneHexer/Idle",
                RunAsset: "Sprites/enemies/BoneHexer/Run",
                Origin: new Vector2(64f, 96f),
                Scale: 1.02f,
                Tint: new Color(80, 140, 255),
                FrameSize: 128),
            RoleConfig: ProtectorRole());

    private static EnemyArchetype BufferHexer() =>
        new(
            Id: "buffer_hexer",
            MoveSpeed: 90f,
            MaxHealth: 18f,
            Damage: 5f,
            AttackCooldownSeconds: 1.6f,
            AttackRange: 7f,
            CollisionRadius: 5f,
            Mass: 0.6f,
            Visual: new EnemyVisualDefinition(
                IdleAsset: "Sprites/enemies/BoneHexer/Idle",
                RunAsset: "Sprites/enemies/BoneHexer/Run",
                Origin: new Vector2(64f, 96f),
                Scale: 0.98f,
                Tint: new Color(120, 220, 120),
                FrameSize: 128),
            RoleConfig: BufferRole());

    private static EnemyArchetype EliteHexer() =>
        new(
            Id: "elite_hexer",
            MoveSpeed: 95f,
            MaxHealth: 80f, // Much tankier than normal
            Damage: 15f,
            AttackCooldownSeconds: 1.0f,
            AttackRange: 7f,
            CollisionRadius: 7f, // Larger collision radius
            Mass: 1.2f, // Harder to knock back
            Visual: new EnemyVisualDefinition(
                IdleAsset: "Sprites/enemies/BoneHexer/Idle",
                RunAsset: "Sprites/enemies/BoneHexer/Run",
                Origin: new Vector2(64f, 96f),
                Scale: 1.4f, // Noticeably larger
                Tint: new Color(255, 200, 50), // Gold/orange tint for elite
                FrameSize: 128),
            Tier: EnemyTier.Elite);

    private static EnemyArchetype SkeletonBoss() =>
        new(
            Id: "skeleton_boss",
            MoveSpeed: 70f, // Slower but menacing
            MaxHealth: 250f, // Boss-level health
            Damage: 0f, // Damage comes from projectiles
            AttackCooldownSeconds: 1.8f,
            AttackRange: 180f,
            CollisionRadius: 9f, // Very large
            Mass: 2.5f, // Nearly immovable
            Visual: new EnemyVisualDefinition(
                IdleAsset: "Sprites/enemies/BoneHexer/Idle",
                RunAsset: "Sprites/enemies/BoneHexer/Run",
                Origin: new Vector2(64f, 96f),
                Scale: 1.8f, // Much larger than normal enemies
                Tint: new Color(150, 50, 200), // Deep purple for boss
                FrameSize: 128),
            Tier: EnemyTier.Boss,
            RangedAttack: new RangedAttackDefinition(
                ProjectileSpeed: 220f,
                ProjectileDamage: 20f,
                OptimalRange: 160f,
                WindupSeconds: 1.0f)); // Longer telegraph for boss attack

    private static EnemyArchetype Act1HeadmasterBoss()
    {
        var baseBoss = SkeletonBoss();
        var ranged = baseBoss.RangedAttack ?? new RangedAttackDefinition(220f, 20f, 160f, 1.0f);
        ranged = ranged with
        {
            ProjectileDamage = 18f,
            WindupSeconds = 0.9f,
            OptimalRange = 170f
        };

        return baseBoss with
        {
            Id = "boss_act1_headmaster",
            MaxHealth = 280f,
            MoveSpeed = 72f,
            Visual = baseBoss.Visual with { Tint = new Color(120, 60, 210) },
            RangedAttack = ranged
        };
    }

    private static EnemyArchetype Act2BogLichBoss()
    {
        var baseBoss = SkeletonBoss();
        var ranged = baseBoss.RangedAttack ?? new RangedAttackDefinition(220f, 20f, 160f, 1.0f);
        ranged = ranged with
        {
            ProjectileDamage = 22f,
            WindupSeconds = 1.05f,
            OptimalRange = 150f
        };

        return baseBoss with
        {
            Id = "boss_act2_bog_lich",
            MaxHealth = 320f,
            MoveSpeed = 68f,
            Visual = baseBoss.Visual with { Tint = new Color(90, 160, 110) },
            RangedAttack = ranged
        };
    }

    private static EnemyArchetype Act3FrostSentinelBoss()
    {
        var baseBoss = SkeletonBoss();
        var ranged = baseBoss.RangedAttack ?? new RangedAttackDefinition(220f, 20f, 160f, 1.0f);
        ranged = ranged with
        {
            ProjectileDamage = 24f,
            WindupSeconds = 1.1f,
            OptimalRange = 185f
        };

        return baseBoss with
        {
            Id = "boss_act3_frost_sentinel",
            MaxHealth = 360f,
            MoveSpeed = 66f,
            Visual = baseBoss.Visual with { Tint = new Color(120, 200, 255) },
            RangedAttack = ranged
        };
    }

    private static EnemyArchetype Act4VoidArchonBoss()
    {
        var baseBoss = SkeletonBoss();
        var ranged = baseBoss.RangedAttack ?? new RangedAttackDefinition(220f, 20f, 160f, 1.0f);
        ranged = ranged with
        {
            ProjectileDamage = 28f,
            WindupSeconds = 1.15f,
            OptimalRange = 200f
        };

        return baseBoss with
        {
            Id = "boss_act4_void_archon",
            MaxHealth = 420f,
            MoveSpeed = 64f,
            Visual = baseBoss.Visual with { Tint = new Color(200, 70, 230) },
            RangedAttack = ranged
        };
    }

    private static AiRoleConfig ChargerRole() =>
        new(
            Role: EnemyRole.Charger,
            CommitRangeMin: 60f,
            CommitRangeMax: 120f,
            WindupDuration: 0.4f,
            CooldownDuration: 3.5f,
            KnockbackForce: 400f,
            Telegraph: new TelegraphData(
                duration: 0.4f,
                color: new Color(255, 50, 50, 180),
                radius: 46f,
                offset: Vector2.Zero));

    private static AiRoleConfig ProtectorRole() =>
        new(
            Role: EnemyRole.Protector,
            ShieldRange: 80f,
            ShieldDuration: 1.5f,
            ShieldDetectionRange: 120f,
            CooldownDuration: 5.0f,
            ShieldBlocksProjectiles: true,
            Telegraph: new TelegraphData(
                duration: 1.5f,
                color: new Color(80, 140, 255, 180),
                radius: 82f,
                offset: Vector2.Zero));

    private static AiRoleConfig BufferRole()
    {
        var modifiers = StatModifiers.Zero;
        modifiers.MoveSpeedMultiplicative = 1.3f;

        return new AiRoleConfig(
            Role: EnemyRole.Buffer,
            BuffRange: 100f,
            BuffDuration: 4.0f,
            CooldownDuration: 6.0f,
            BuffType: BuffType.MoveSpeedBuff,
            BuffModifiers: modifiers,
            Telegraph: new TelegraphData(
                duration: 0.5f,
                color: new Color(120, 220, 120, 180),
                radius: 100f,
                offset: Vector2.Zero));
    }

    /// <summary>
    /// Create an elite enemy for debug spawning (F7 key).
    /// </summary>
    public static EnemyArchetype CreateEliteForDebug() => EliteHexer();

    public static EnemyArchetype CreateBossArchetype(string bossArchetypeId) => bossArchetypeId switch
    {
        "boss_act1_headmaster" => Act1HeadmasterBoss(),
        "boss_act2_bog_lich" => Act2BogLichBoss(),
        "boss_act3_frost_sentinel" => Act3FrostSentinelBoss(),
        "boss_act4_void_archon" => Act4VoidArchonBoss(),
        _ => SkeletonBoss()
    };

    public static EnemyArchetype CreateArchetypeById(string archetypeId) => archetypeId switch
    {
        "bone_hexer" => BaseHexer(),
        "bone_scout" => ScoutHexer(),
        "bone_mage" => BoneMage(),
        "charger_hexer" => ChargerHexer(),
        "protector_hexer" => ProtectorHexer(),
        "buffer_hexer" => BufferHexer(),
        "elite_hexer" => EliteHexer(),
        "skeleton_boss" => SkeletonBoss(),
        "boss_act1_headmaster" => Act1HeadmasterBoss(),
        "boss_act2_bog_lich" => Act2BogLichBoss(),
        "boss_act3_frost_sentinel" => Act3FrostSentinelBoss(),
        "boss_act4_void_archon" => Act4VoidArchonBoss(),
        _ => BaseHexer()
    };

    /// <summary>
    /// Create a boss enemy for debug spawning (F8 key).
    /// </summary>
    public static EnemyArchetype CreateBossForDebug() => SkeletonBoss();
}

