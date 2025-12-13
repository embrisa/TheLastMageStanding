using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Combat;

namespace TheLastMageStanding.Game.Core.Skills;

/// <summary>
/// System that executes completed skill casts by spawning projectiles, AoE, etc.
/// Integrates with existing collision and damage systems.
/// </summary>
internal sealed class SkillExecutionSystem : IUpdateSystem
{
    private EcsWorld _world = null!;
    private readonly SkillRegistry _skillRegistry;
    private readonly CombatRng _rng = new();

    public SkillExecutionSystem(SkillRegistry skillRegistry)
    {
        _skillRegistry = skillRegistry;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<SkillCastCompletedEvent>(OnSkillCastCompleted);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // This system is event-driven, no per-frame update needed
    }

    private void OnSkillCastCompleted(SkillCastCompletedEvent evt)
    {
        var definition = _skillRegistry.GetSkill(evt.SkillId);
        if (definition == null)
        {
            return;
        }

        // Get caster's faction
        if (!_world.TryGetComponent(evt.Caster, out Faction casterFaction))
        {
            return;
        }

        // Get skill modifiers
        var skillModifiers = GetCombinedSkillModifiers(evt.Caster, definition);
        var globalCdr = 0f;

        if (_world.TryGetComponent(evt.Caster, out ComputedStats stats))
        {
            globalCdr = stats.EffectiveCooldownReduction;
        }

        // Calculate effective stats
        var effectiveStats = ComputedSkillStats.Calculate(definition, skillModifiers, globalCdr);

        // Execute based on delivery type
        switch (definition.DeliveryType)
        {
            case SkillDeliveryType.Projectile:
                SpawnProjectiles(evt, definition, effectiveStats, casterFaction);
                break;
            
            case SkillDeliveryType.AreaOfEffect:
                SpawnAoEEffect(evt, definition, effectiveStats, casterFaction);
                break;
            
            case SkillDeliveryType.Melee:
                SpawnMeleeHitbox(evt, definition, effectiveStats, casterFaction);
                break;
            
            default:
                // Beam and other types not implemented yet
                break;
        }

        // Publish VFX/SFX events
        PublishSkillEffects(evt, definition);
    }

    private SkillModifiers GetCombinedSkillModifiers(Entity caster, SkillDefinition definition)
    {
        var modifiers = SkillModifiers.Zero;

        if (_world.TryGetComponent(caster, out PlayerSkillModifiers playerMods))
        {
            modifiers = playerMods.GetModifiersForSkill(definition.Id, definition.Element);
        }

        if (_world.TryGetComponent(caster, out LevelUpSkillModifiers levelUpMods) &&
            levelUpMods.SkillSpecificModifiers != null &&
            levelUpMods.SkillSpecificModifiers.TryGetValue(definition.Id, out var runMods))
        {
            modifiers = SkillModifiers.Combine(modifiers, runMods);
        }

        return modifiers;
    }

    private void SpawnProjectiles(
        SkillCastCompletedEvent evt,
        SkillDefinition definition,
        ComputedSkillStats stats,
        Faction casterFaction)
    {
        // Calculate base damage from caster's power stat
        var baseDamage = CalculateBaseDamage(evt.Caster, stats.EffectiveDamageMultiplier);

        // Determine direction
        var direction = evt.Direction;
        if (direction.LengthSquared() < 0.0001f)
        {
            direction = new Vector2(1f, 0f);
        }
        else
        {
            direction = Vector2.Normalize(direction);
        }

        // Spawn multiple projectiles if specified
        var count = stats.EffectiveProjectileCount;
        var spreadAngle = count > 1 ? MathHelper.ToRadians(30f) : 0f; // 30 degree spread for multi-shot
        var angleStep = count > 1 ? spreadAngle / (count - 1) : 0f;
        var startAngle = -spreadAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            var angle = startAngle + (angleStep * i);
            var rotatedDir = RotateVector(direction, angle);
            var statusEffect = TryRollStatus(definition);
            
            SpawnSingleProjectile(
                evt.CasterPosition,
                rotatedDir,
                baseDamage,
                stats,
                definition,
                evt.Caster,
                casterFaction,
                statusEffect);
        }
    }

    private void SpawnSingleProjectile(
        Vector2 sourcePosition,
        Vector2 direction,
        float damage,
        ComputedSkillStats stats,
        SkillDefinition definition,
        Entity source,
        Faction sourceFaction,
        StatusEffectData? statusEffect)
    {
        var projectileEntity = _world.CreateEntity();

        // Position with slight offset to avoid self-collision
        var spawnOffset = direction * 12f;
        _world.SetComponent(projectileEntity, new Position(sourcePosition + spawnOffset));

        // Velocity
        var velocity = direction * stats.EffectiveProjectileSpeed;
        _world.SetComponent(projectileEntity, new Velocity(velocity));

        // Projectile component with pierce support
        var lifetime = stats.EffectiveRange / stats.EffectiveProjectileSpeed;
        var projectile = new Projectile(
            source, 
            damage, 
            sourceFaction, 
            lifetime,
            statusEffect);
        // Note: Pierce support requires extending Projectile component (future enhancement)
        _world.SetComponent(projectileEntity, projectile);

        // Visual based on element
        var color = GetElementColor(definition.Element);
        var size = definition.Id == SkillId.Fireball ? 8f : 6f; // Larger for Fireball
        _world.SetComponent(projectileEntity, new ProjectileVisual(color, size));

        // Collision
        var projectileLayer = sourceFaction == Faction.Player ? CollisionLayer.Projectile : CollisionLayer.Enemy;
        var targetLayer = sourceFaction == Faction.Player ? CollisionLayer.Enemy : CollisionLayer.Player;
        
        _world.SetComponent(projectileEntity, Collider.CreateCircle(
            radius: size,
            layer: projectileLayer,
            mask: targetLayer | CollisionLayer.WorldStatic,
            isTrigger: true));

        // Mark as skill projectile with AoE info if applicable
        if (stats.EffectiveAoeRadius > 0f)
        {
            _world.SetComponent(projectileEntity, new ProjectileAoE(
                stats.EffectiveAoeRadius,
                damage * 0.7f)); // AoE does reduced damage
        }
    }

    private void SpawnAoEEffect(
        SkillCastCompletedEvent evt,
        SkillDefinition definition,
        ComputedSkillStats stats,
        Faction casterFaction)
    {
        var baseDamage = CalculateBaseDamage(evt.Caster, stats.EffectiveDamageMultiplier);
        var statusEffect = TryRollStatus(definition);
        
        // Determine AoE center
        var center = definition.TargetType == SkillTargetType.Self 
            ? evt.CasterPosition 
            : evt.TargetPosition;

        // Create AoE damage entity
        var aoeEntity = _world.CreateEntity();
        _world.SetComponent(aoeEntity, new Position(center));
        
        // Create a large trigger collider for the AoE
        var hitboxLayer = casterFaction == Faction.Player ? CollisionLayer.Projectile : CollisionLayer.Enemy;
        var targetLayer = casterFaction == Faction.Player ? CollisionLayer.Enemy : CollisionLayer.Player;
        
        _world.SetComponent(aoeEntity, Collider.CreateCircle(
            radius: stats.EffectiveAoeRadius,
            layer: hitboxLayer,
            mask: targetLayer,
            isTrigger: true));

        // Use attack hitbox component for damage
        _world.SetComponent(aoeEntity, new AttackHitbox(
            evt.Caster,
            baseDamage,
            casterFaction,
            lifetimeSeconds: 0.1f,
            statusEffect: statusEffect)); // Very short duration, just enough to hit

        // Add visual telegraph (will fade quickly)
        _world.EventBus.Publish(new VfxSpawnEvent(
            $"aoe_{definition.Element}",
            center,
            VfxType.Impact,
            GetElementColor(definition.Element)));

        // Play sound
        _world.EventBus.Publish(new SfxPlayEvent(
            $"skill_{definition.Element}_aoe",
            SfxCategory.Ability,
            center,
            volume: 0.8f));
    }

    private void SpawnMeleeHitbox(
        SkillCastCompletedEvent evt,
        SkillDefinition definition,
        ComputedSkillStats stats,
        Faction casterFaction)
    {
        var baseDamage = CalculateBaseDamage(evt.Caster, stats.EffectiveDamageMultiplier);
        var statusEffect = TryRollStatus(definition);
        
        // Get direction
        var direction = evt.Direction;
        if (direction.LengthSquared() < 0.0001f)
        {
            direction = new Vector2(1f, 0f);
        }
        else
        {
            direction = Vector2.Normalize(direction);
        }

        // Position hitbox in front of caster
        var hitboxOffset = direction * stats.EffectiveRange;
        var hitboxPosition = evt.CasterPosition + hitboxOffset;

        var hitboxEntity = _world.CreateEntity();
        _world.SetComponent(hitboxEntity, new Position(hitboxPosition));

        // Attack hitbox component
        _world.SetComponent(hitboxEntity, new AttackHitbox(
            evt.Caster,
            baseDamage,
            casterFaction,
            lifetimeSeconds: 0.15f,
            statusEffect: statusEffect));

        // Collision
        var hitboxLayer = casterFaction == Faction.Player ? CollisionLayer.Projectile : CollisionLayer.Enemy;
        var targetLayer = casterFaction == Faction.Player ? CollisionLayer.Enemy : CollisionLayer.Player;
        
        _world.SetComponent(hitboxEntity, Collider.CreateCircle(
            radius: stats.EffectiveAoeRadius > 0f ? stats.EffectiveAoeRadius : 30f,
            layer: hitboxLayer,
            mask: targetLayer,
            isTrigger: true));
    }

    private float CalculateBaseDamage(Entity caster, float damageMultiplier)
    {
        // Base damage starts from caster's power stat
        var power = 1.0f;
        if (_world.TryGetComponent(caster, out ComputedStats stats))
        {
            power = stats.EffectivePower;
        }

        // Apply skill's damage multiplier
        return power * damageMultiplier * 10f; // Scale factor for meaningful damage numbers
    }

    private static Color GetElementColor(SkillElement element)
    {
        return element switch
        {
            SkillElement.Fire => new Color(255, 100, 50),
            SkillElement.Arcane => new Color(150, 100, 255),
            SkillElement.Frost => new Color(100, 200, 255),
            _ => Color.White
        };
    }

    private static Vector2 RotateVector(Vector2 vector, float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        return new Vector2(
            vector.X * cos - vector.Y * sin,
            vector.X * sin + vector.Y * cos);
    }

    private void PublishSkillEffects(SkillCastCompletedEvent evt, SkillDefinition definition)
    {
        // VFX
        _world.EventBus.Publish(new VfxSpawnEvent(
            $"skill_{definition.Element}_{definition.DeliveryType}",
            evt.CasterPosition,
            VfxType.Impact,
            GetElementColor(definition.Element)));

        // SFX
        _world.EventBus.Publish(new SfxPlayEvent(
            $"skill_{definition.Id}",
            SfxCategory.Ability,
            evt.CasterPosition,
            volume: 0.7f));
    }

    private StatusEffectData? TryRollStatus(SkillDefinition definition)
    {
        if (definition.OnHitStatusEffect is null)
        {
            return null;
        }

        var chance = Math.Clamp(definition.StatusEffectApplicationChance, 0f, 1f);
        return _rng.NextFloat() <= chance ? definition.OnHitStatusEffect : null;
    }
}

/// <summary>
/// Component marking a projectile that should explode on impact.
/// </summary>
internal struct ProjectileAoE
{
    public float ExplosionRadius { get; set; }
    public float ExplosionDamage { get; set; }

    public ProjectileAoE(float radius, float damage)
    {
        ExplosionRadius = radius;
        ExplosionDamage = damage;
    }
}
