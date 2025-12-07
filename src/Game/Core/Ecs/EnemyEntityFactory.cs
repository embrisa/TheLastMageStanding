using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;

namespace TheLastMageStanding.Game.Core.Ecs;

internal sealed class EnemyEntityFactory
{
    private readonly EcsWorld _world;

    public EnemyEntityFactory(EcsWorld world)
    {
        _world = world;
    }

    public Entity CreateEnemy(Vector2 spawnPosition, EnemyArchetype archetype)
    {
        var entity = _world.CreateEntity();

        _world.SetComponent(entity, Faction.Enemy);
        _world.SetComponent(entity, new Position(spawnPosition));
        _world.SetComponent(entity, new Velocity(Vector2.Zero));
        _world.SetComponent(entity, new MoveSpeed(archetype.MoveSpeed));
        _world.SetComponent(entity, new Health(archetype.MaxHealth, archetype.MaxHealth));
        _world.SetComponent(entity, new Hitbox(archetype.CollisionRadius));
        _world.SetComponent(entity, new AttackStats(
            damage: archetype.Damage,
            cooldownSeconds: archetype.AttackCooldownSeconds,
            range: archetype.AttackRange));
        _world.SetComponent(entity, new AiSeekTarget(Faction.Player));
        _world.SetComponent(entity, new Lifetime(20f));
        _world.SetComponent(entity, new Mass(archetype.Mass));
        _world.SetComponent(entity, Collider.CreateCircle(archetype.CollisionRadius, CollisionLayer.Enemy, CollisionLayer.Player | CollisionLayer.Enemy | CollisionLayer.WorldStatic, isTrigger: false));

        // Combat hitbox/hurtbox - enemies can be hit but don't spawn melee hitboxes (use contact damage)
        _world.SetComponent(entity, new Hurtbox { IsInvulnerable = false, InvulnerabilityEndsAt = 0f });

        _world.SetComponent(
            entity,
            new EnemyAnimationState
            {
                Facing = PlayerFacingDirection.South,
                ActiveClip = EnemyAnimationClip.Idle,
                Timer = 0f,
                FrameIndex = 0,
                IsMoving = false,
            });

        _world.SetComponent(
            entity,
            new EnemySpriteAssets(archetype.Visual.IdleAsset, archetype.Visual.RunAsset));
        _world.SetComponent(
            entity,
            new EnemyVisual(archetype.Visual.Origin, archetype.Visual.Scale, archetype.Visual.FrameSize, archetype.Visual.Tint));

        // Add ranged attacker component if archetype supports it
        if (archetype.RangedAttack.HasValue)
        {
            var rangedConfig = archetype.RangedAttack.Value;
            _world.SetComponent(entity, new RangedAttacker(
                projectileSpeed: rangedConfig.ProjectileSpeed,
                projectileDamage: rangedConfig.ProjectileDamage,
                optimalRange: rangedConfig.OptimalRange,
                windupSeconds: rangedConfig.WindupSeconds));
        }

        return entity;
    }
}

