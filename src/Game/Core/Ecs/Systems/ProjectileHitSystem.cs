using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles projectile collision events and applies damage to targets.
/// Projectiles are destroyed on hit and publish EntityDamagedEvent.
/// </summary>
internal sealed class ProjectileHitSystem : IUpdateSystem
{
    private EcsWorld _world = null!;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<CollisionEnterEvent>(OnCollisionEnter);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // No per-frame logic needed - all work is done in collision event handler
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

        // Check if target can take damage (has health and hurtbox)
        if (!_world.TryGetComponent(targetEntity, out Health health) || health.IsDead)
        {
            return false;
        }

        if (!_world.TryGetComponent(targetEntity, out Hurtbox hurtbox))
        {
            return false;
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

        // Apply damage via event
        _world.EventBus.Publish(new EntityDamagedEvent(
            targetEntity,
            projectile.Damage,
            projectilePosition.Value,
            projectile.SourceFaction));

        // Mark projectile as having hit something
        projectile.HasHit = true;
        _world.SetComponent(projectileEntity, projectile);

        return true;
    }
}
