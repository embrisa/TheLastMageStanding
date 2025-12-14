using System;
using System.Collections.Generic;
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

    public Entity CreateEnemy(Vector2 spawnPosition, EnemyArchetype archetype, IReadOnlyList<EliteModifierType>? modifiers = null)
    {
        var entity = _world.CreateEntity();

        _world.SetComponent(entity, Faction.Enemy);
        _world.SetComponent(entity, new Position(spawnPosition));
        _world.SetComponent(entity, new Velocity(Vector2.Zero));
        _world.SetComponent(entity, new MoveSpeed(archetype.MoveSpeed));
        _world.SetComponent(entity, new BaseMoveSpeed(archetype.MoveSpeed));
        _world.SetComponent(entity, new Health(archetype.MaxHealth, archetype.MaxHealth));
        _world.SetComponent(entity, new Hitbox(archetype.CollisionRadius));
        _world.SetComponent(entity, new AttackStats(
            damage: archetype.Damage,
            cooldownSeconds: archetype.AttackCooldownSeconds,
            range: archetype.AttackRange));
        _world.SetComponent(entity, new AiSeekTarget(Faction.Player));
        if (archetype.RoleConfig.HasValue)
        {
            var roleConfig = archetype.RoleConfig.Value;
            _world.SetComponent(entity, roleConfig);
            _world.SetComponent(
                entity,
                new AiBehaviorStateMachine
                {
                    State = AiBehaviorState.Idle,
                    StateTimer = 0f,
                    CooldownTimer = 0f,
                    PerceptionTimer = 0f,
                    TargetEntity = Entity.None,
                    HasTarget = false,
                    AimDirection = Vector2.Zero,
                    PreviousMass = 0f
                });
        }
        _world.SetComponent(entity, new Lifetime(20f));
        _world.SetComponent(entity, new Mass(archetype.Mass));

        // Stat components for unified damage model
        _world.SetComponent(entity, new OffensiveStats
        {
            Power = 1.0f,
            AttackSpeed = 1.0f,
            CritChance = 0.0f, // Enemies don't crit by default
            CritMultiplier = 1.5f,
            CooldownReduction = 0.0f
        });
        _world.SetComponent(entity, new DefensiveStats
        {
            Armor = 0f,
            ArcaneResist = 0f,
            FireResist = 0f,
            FrostResist = 0f
        });
        _world.SetComponent(entity, StatModifiers.Zero);
        _world.SetComponent(entity, new ComputedStats { IsDirty = true });

        _world.SetComponent(entity, Collider.CreateCircle(archetype.CollisionRadius, CollisionLayer.Enemy, CollisionLayer.Player | CollisionLayer.Enemy | CollisionLayer.WorldStatic, isTrigger: false));

        // Combat hitbox/hurtbox - enemies can be hit but don't spawn melee hitboxes (use contact damage)
        _world.SetComponent(entity, new Hurtbox { IsInvulnerable = false, InvulnerabilityEndsAt = 0f });

        // Configure loot drops based on enemy tier
        var lootDropper = new LootDropper
        {
            DropChance = 0.15f, // Base chance
            ModifierRewardMultiplier = 1f,
            IsElite = archetype.Tier == EnemyTier.Elite,
            IsBoss = archetype.Tier == EnemyTier.Boss
        };
        _world.SetComponent(entity, lootDropper);

        // Add tier tags for easy querying
        if (archetype.Tier == EnemyTier.Elite)
        {
            _world.SetComponent(entity, new EliteTag());
        }
        else if (archetype.Tier == EnemyTier.Boss)
        {
            _world.SetComponent(entity, new BossTag());
        }

        // Apply stat modifiers for elite/boss enemies
        if (archetype.Tier == EnemyTier.Elite)
        {
            // Elites get some defensive bonuses
            _world.SetComponent(entity, new DefensiveStats
            {
                Armor = 5f, // Small armor bonus
                ArcaneResist = 10f, // 10% arcane resist
                FireResist = 20f,
                FrostResist = 20f
            });
        }
        else if (archetype.Tier == EnemyTier.Boss)
        {
            // Bosses get stronger defensive bonuses
            _world.SetComponent(entity, new DefensiveStats
            {
                Armor = 15f, // Significant armor
                ArcaneResist = 25f, // 25% arcane resist
                FireResist = 50f,
                FrostResist = 50f
            });
        }

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

        var normalizedModifiers = NormalizeModifiers(modifiers);
        if (normalizedModifiers.Count > 0)
        {
            _world.SetComponent(entity, new EliteModifierData(normalizedModifiers));
            ApplyModifierComponents(entity, normalizedModifiers);
            ApplyModifierVisuals(entity, normalizedModifiers);

            lootDropper.ModifierRewardMultiplier = CalculateRewardMultiplier(normalizedModifiers);
            _world.SetComponent(entity, lootDropper);
        }

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

    private static List<EliteModifierType> NormalizeModifiers(IReadOnlyList<EliteModifierType>? modifiers)
    {
        var result = new List<EliteModifierType>();
        if (modifiers == null)
        {
            return result;
        }

        var seen = new HashSet<EliteModifierType>();
        foreach (var modifier in modifiers)
        {
            var definition = EliteModifierRegistry.Get(modifier);
            if (!definition.AllowStacking && seen.Contains(modifier))
            {
                continue;
            }

            result.Add(modifier);
            if (!definition.AllowStacking)
            {
                seen.Add(modifier);
            }
        }

        return result;
    }

    private static float CalculateRewardMultiplier(IReadOnlyList<EliteModifierType> modifiers)
    {
        var multiplier = 1.0f;
        foreach (var modifier in modifiers)
        {
            var definition = EliteModifierRegistry.Get(modifier);
            multiplier *= MathF.Max(1.0f, definition.RewardMultiplier);
        }

        return MathF.Min(2.0f, multiplier);
    }

    private void ApplyModifierComponents(Entity entity, IReadOnlyList<EliteModifierType> modifiers)
    {
        foreach (var modifier in modifiers)
        {
            if (modifier == EliteModifierType.Shielded)
            {
                // Simple flat shield that recharges after a short delay
                var shield = new EliteShield
                {
                    Max = 45f,
                    Current = 45f,
                    RegenCooldown = 2.5f,
                    RegenRate = 12f,
                    CooldownTimer = 0f
                };
                _world.SetComponent(entity, shield);
            }
        }
    }

    private void ApplyModifierVisuals(Entity entity, IReadOnlyList<EliteModifierType> modifiers)
    {
        var hasTelegraph = false;
        TelegraphData telegraphData = default;

        foreach (var modifier in modifiers)
        {
            var definition = EliteModifierRegistry.Get(modifier);
            if (_world.TryGetComponent(entity, out EnemyVisual visual))
            {
                var tinted = BlendColors(visual.Tint, definition.TintOverlay);
                var updatedVisual = new EnemyVisual(visual.Origin, visual.Scale, visual.FrameSize, tinted);
                _world.SetComponent(entity, updatedVisual);
            }

            if (definition.AuraOrIndicator.HasValue)
            {
                var aura = definition.AuraOrIndicator.Value;
                if (!hasTelegraph)
                {
                    telegraphData = aura;
                    hasTelegraph = true;
                }
                else
                {
                    telegraphData = new TelegraphData(
                        duration: MathF.Max(telegraphData.Duration, aura.Duration),
                        color: BlendColors(telegraphData.Color, aura.Color),
                        radius: MathF.Max(telegraphData.Radius, aura.Radius),
                        offset: Vector2.Zero,
                        shape: aura.Shape);
                }
            }
        }

        if (hasTelegraph)
        {
            _world.SetComponent(entity, new ActiveTelegraph(float.MaxValue, telegraphData));
        }
    }

    private static Color BlendColors(Color a, Color b)
    {
        var av = a.ToVector3();
        var bv = b.ToVector3();
        var mix = (av + bv) * 0.5f;
        return new Color(mix);
    }
}

