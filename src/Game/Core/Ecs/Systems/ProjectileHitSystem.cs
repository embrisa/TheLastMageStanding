using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Combat;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles projectile collision events and applies damage to targets.
/// Projectiles are destroyed on hit and publish EntityDamagedEvent.
/// </summary>
internal sealed class ProjectileHitSystem : IUpdateSystem
{
    private EcsWorld _world = null!;
    private DamageApplicationService? _damageService;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<CollisionEnterEvent>(OnCollisionEnter);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Lazily initialize damage service
        _damageService ??= new DamageApplicationService(
            world,
            new DamageCalculator(new CombatRng()));
    }

    private void OnCollisionEnter(CollisionEnterEvent evt)
    {
        // Check both orderings since collision events don't guarantee which entity is which
        if (TryHandleProjectileHit(evt.EntityA, evt.EntityB, evt.ContactPoint))
        {
            return;
        }

        TryHandleProjectileHit(evt.EntityB, evt.EntityA, evt.ContactPoint);
    }

    private bool TryHandleProjectileHit(Entity projectileEntity, Entity targetEntity, Microsoft.Xna.Framework.Vector2 contactPoint)
    {
        // Check if first entity is a projectile
        if (!_world.TryGetComponent(projectileEntity, out Projectile projectile))
        {
            return false;
        }

        // Projectile already hit something, skip
        if (projectile.HasHit)
        {
            return false;
        }

        // Don't hit the source entity
        if (targetEntity.Id == projectile.Source.Id)
        {
            return false;
        }

        // Check if target has a faction
        if (!_world.TryGetComponent(targetEntity, out Faction targetFaction))
        {
            return false;
        }

        // No friendly fire - projectile should only hit opposing faction
        if (projectile.SourceFaction == targetFaction)
        {
            return false;
        }

        // Dash/temporary invulnerability
        if (_world.TryGetComponent(targetEntity, out Invulnerable _))
        {
            return false;
        }

        // Check if target can take damage (has health and hurtbox)
        if (!_world.TryGetComponent(targetEntity, out Health health) || health.IsDead)
        {
            return false;
        }

        if (!_world.TryGetComponent(targetEntity, out Hurtbox hurtbox))
        {
            return false;
        }

        // Check projectile shields (protector support)
        if (_world.TryGetComponent(targetEntity, out ShieldActive shield) && shield.IsActive && shield.BlocksRemaining > 0)
        {
            projectile.HasHit = true;
            _world.SetComponent(projectileEntity, projectile);

            shield.BlocksRemaining -= 1;
            if (shield.BlocksRemaining <= 0 || shield.RemainingDuration <= 0f)
            {
                _world.RemoveComponent<ShieldActive>(targetEntity);
            }
            else
            {
                _world.SetComponent(targetEntity, shield);
            }

            _world.EventBus.Publish(new VfxSpawnEvent("projectile_block", contactPoint, VfxType.Impact, new Color(80, 140, 255)));
            _world.EventBus.Publish(new SfxPlayEvent("shield_block", SfxCategory.Impact, contactPoint));
            return true;
        }

        // Check invulnerability
        if (hurtbox.IsInvulnerable && _world.TryGetComponent(targetEntity, out Position _))
        {
            var currentTime = _world.TryGetComponent(targetEntity, out Lifetime _) ? 0f : 0f;
            // Note: We don't have easy access to game time here, so we skip the time check
            // The hurtbox invulnerability is managed by MeleeHitSystem
            if (hurtbox.IsInvulnerable)
            {
                return false;
            }
        }

        // Get projectile position for damage event
        if (!_world.TryGetComponent(projectileEntity, out Position projectilePosition))
        {
            projectilePosition = new Position(contactPoint);
        }

        // Apply damage using unified damage calculator
        if (_damageService == null)
            return false;

        var damageInfo = new DamageInfo(
            baseDamage: projectile.Damage,
            damageType: DamageType.Arcane, // Projectiles are arcane damage
            flags: DamageFlags.CanCrit,
            source: DamageSource.Projectile,
            statusEffect: projectile.StatusEffect);

        _damageService.ApplyDamage(projectile.Source, targetEntity, damageInfo, projectilePosition.Value);

        // Mark projectile as having hit something
        projectile.HasHit = true;
        _world.SetComponent(projectileEntity, projectile);

        return true;
    }
}
