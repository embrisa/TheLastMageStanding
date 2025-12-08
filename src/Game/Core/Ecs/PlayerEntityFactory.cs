using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;

namespace TheLastMageStanding.Game.Core.Ecs;

internal sealed class PlayerEntityFactory
{
    private readonly EcsWorld _world;
    private readonly ProgressionConfig _progressionConfig;

    public PlayerEntityFactory(EcsWorld world, ProgressionConfig progressionConfig)
    {
        _world = world;
        _progressionConfig = progressionConfig;
    }

    public Entity CreatePlayer(Vector2 spawnPosition)
    {
        var entity = _world.CreateEntity();

        _world.SetComponent(entity, new PlayerTag());
        _world.SetComponent(entity, new CameraTarget());
        _world.SetComponent(entity, Faction.Player);
        _world.SetComponent(entity, new Position(spawnPosition));
        _world.SetComponent(entity, new Velocity(Vector2.Zero));
        _world.SetComponent(entity, new MoveSpeed(220f));
        _world.SetComponent(entity, new InputIntent());
        _world.SetComponent(entity, new AttackStats(damage: 20f, cooldownSeconds: 0.35f, range: 42f));
        _world.SetComponent(entity, new Health(current: 100f, max: 100f));
        _world.SetComponent(entity, new Hitbox(radius: 6f));
        _world.SetComponent(entity, new Mass(1.0f)); // Standard player mass
        
        // Stat components for unified damage model
        _world.SetComponent(entity, new OffensiveStats
        {
            Power = 1.0f,
            AttackSpeed = 1.0f,
            CritChance = 0.05f, // 5% base crit
            CritMultiplier = 1.5f,
            CooldownReduction = 0.0f
        });
        _world.SetComponent(entity, new DefensiveStats
        {
            Armor = 0f,
            ArcaneResist = 0f
        });
        _world.SetComponent(entity, StatModifiers.Zero);
        _world.SetComponent(entity, new ComputedStats { IsDirty = true });
        
        _world.SetComponent(entity, Collider.CreateCircle(6f, CollisionLayer.Player, CollisionLayer.Enemy | CollisionLayer.Pickup | CollisionLayer.WorldStatic, isTrigger: false));

        // Combat hitbox/hurtbox components
        _world.SetComponent(entity, new Hurtbox { IsInvulnerable = false, InvulnerabilityEndsAt = 0f });
        _world.SetComponent(entity, new MeleeAttackConfig(hitboxRadius: 42f, hitboxOffset: Vector2.Zero, duration: 0.15f));

        // Animation-driven attack components
        _world.SetComponent(entity, new AnimationDrivenAttack("PlayerMelee"));
        _world.SetComponent(entity, DirectionalHitboxConfig.CreateDefault(forwardDistance: 24f));
        _world.SetComponent(entity, new AnimationEventState(0f, false));

        // Initialize XP/level progression
        var startingLevel = 1;
        var xpToNextLevel = _progressionConfig.CalculateXpForLevel(startingLevel + 1);
        _world.SetComponent(entity, new PlayerXp(currentXp: 0, level: startingLevel, xpToNextLevel: xpToNextLevel));

        return entity;
    }
}

