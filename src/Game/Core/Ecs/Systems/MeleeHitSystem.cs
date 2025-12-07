using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Handles damage application from attack hitboxes using collision events.
/// Replaces distance-based hit detection with collider-driven approach.
/// </summary>
internal sealed class MeleeHitSystem : IUpdateSystem
{
    private float _gameTime;

    public void Initialize(EcsWorld world)
    {
        // Subscribe to collision events
        world.EventBus.Subscribe<CollisionEnterEvent>(evt => OnCollisionEnter(world, evt));
        world.EventBus.Subscribe<SessionRestartedEvent>(_ => _gameTime = 0f);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        _gameTime += context.DeltaSeconds;

        // Update hitbox lifetimes and remove expired ones
        UpdateHitboxLifetimes(world, context.DeltaSeconds);

        // Update hurtbox invulnerability
        UpdateInvulnerability(world);
    }

    private void OnCollisionEnter(EcsWorld world, CollisionEnterEvent evt)
    {
        // Try both directions: A hits B, or B hits A
        TryApplyHitboxDamage(world, evt.EntityA, evt.EntityB, evt.ContactPoint);
        TryApplyHitboxDamage(world, evt.EntityB, evt.EntityA, evt.ContactPoint);
    }

    private void TryApplyHitboxDamage(EcsWorld world, Entity hitboxEntity, Entity targetEntity, Vector2 contactPoint)
    {
        // Check if the first entity is an attack hitbox
        if (!world.TryGetComponent(hitboxEntity, out AttackHitbox hitbox))
            return;

        // Prevent self-damage
        if (hitbox.Owner.Id == targetEntity.Id)
            return;

        // Check if already hit this target
        if (hitbox.AlreadyHit.Contains(targetEntity.Id))
            return;

        // Check if target has a hurtbox (can be damaged)
        if (!world.TryGetComponent(targetEntity, out Hurtbox hurtbox))
            return;

        // Check invulnerability
        if (hurtbox.IsInvulnerable && hurtbox.InvulnerabilityEndsAt > _gameTime)
            return;

        // Check target health and faction
        if (!world.TryGetComponent(targetEntity, out Health health))
            return;
        if (health.IsDead)
            return;

        if (!world.TryGetComponent(targetEntity, out Faction targetFaction))
            return;

        // Check faction filtering - don't hit allies
        if (hitbox.OwnerFaction == targetFaction)
            return;

        // Apply damage
        var damageApplied = MathF.Min(health.Current, hitbox.Damage);
        if (damageApplied <= 0f)
            return;

        // Get owner position for event
        var ownerPos = world.TryGetComponent(hitbox.Owner, out Position pos) ? pos.Value : contactPoint;

        // Publish damage event
        world.EventBus.Publish(new EntityDamagedEvent(targetEntity, damageApplied, ownerPos, hitbox.OwnerFaction));

        // Mark this target as already hit
        hitbox.AlreadyHit.Add(targetEntity.Id);
        world.SetComponent(hitboxEntity, hitbox);

        // Apply brief invulnerability to prevent multi-hits from different sources
        hurtbox.IsInvulnerable = true;
        hurtbox.InvulnerabilityEndsAt = _gameTime + 0.05f; // 50ms invulnerability window
        world.SetComponent(targetEntity, hurtbox);
    }

    private static void UpdateHitboxLifetimes(EcsWorld world, float deltaSeconds)
    {
        world.ForEach<AttackHitbox>((Entity entity, ref AttackHitbox hitbox) =>
        {
            hitbox.LifetimeRemaining -= deltaSeconds;
            
            if (hitbox.LifetimeRemaining <= 0f)
            {
                // Hitbox expired - destroy it
                world.DestroyEntity(entity);
            }
        });
    }

    private void UpdateInvulnerability(EcsWorld world)
    {
        world.ForEach<Hurtbox>((Entity _, ref Hurtbox hurtbox) =>
        {
            if (hurtbox.IsInvulnerable && hurtbox.InvulnerabilityEndsAt <= _gameTime)
            {
                hurtbox.IsInvulnerable = false;
            }
        });
    }
}
