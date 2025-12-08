using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Combat;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Contact damage system that handles damage from entity collisions.
/// Uses collision events and per-entity cooldowns to prevent rapid repeated hits.
/// </summary>
internal sealed class ContactDamageSystem : IUpdateSystem
{
    private float _gameTime;
    private DamageApplicationService? _damageService;

    public void Initialize(EcsWorld world)
    {
        // Subscribe to collision events
        world.EventBus.Subscribe<CollisionEnterEvent>(evt => OnCollision(world, evt.EntityA, evt.EntityB, evt.Normal));
        world.EventBus.Subscribe<CollisionStayEvent>(evt => OnCollision(world, evt.EntityA, evt.EntityB, evt.Normal));
        world.EventBus.Subscribe<SessionRestartedEvent>(_ => _gameTime = 0f);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        _gameTime += context.DeltaSeconds;
        
        // Lazily initialize damage service
        _damageService ??= new DamageApplicationService(
            world,
            new DamageCalculator(new CombatRng()));
    }

    private void OnCollision(EcsWorld world, Entity entityA, Entity entityB, Vector2 normal)
    {
        // Try applying damage in both directions
        TryApplyContactDamage(world, entityA, entityB, normal);
        TryApplyContactDamage(world, entityB, entityA, -normal);
    }

    private void TryApplyContactDamage(EcsWorld world, Entity attacker, Entity target, Vector2 normal)
    {
        // Attacker must have faction and attack stats
        if (!world.TryGetComponent(attacker, out Faction attackerFaction))
            return;
        if (!world.TryGetComponent(attacker, out AttackStats attackStats))
            return;

        // Target must have health and different faction
        if (!world.TryGetComponent(target, out Health health))
            return;
        if (!world.TryGetComponent(target, out Faction targetFaction))
            return;
        if (attackerFaction == targetFaction)
            return;

        // Skip if target is dead
        if (health.IsDead)
            return;

        // Check contact damage cooldown
        var cooldownPool = world.GetPool<ContactDamageCooldown>();
        if (!cooldownPool.TryGet(target, out var cooldown))
        {
            // Initialize cooldown component if not present
            cooldown = new ContactDamageCooldown(0.5f);
            world.SetComponent(target, cooldown);
        }

        // Check if enough time has passed
        if (!cooldown.CanTakeDamageFrom(attacker.Id, _gameTime))
            return;

        // Apply damage using unified damage calculator
        if (_damageService == null)
            return;

        var damageInfo = new DamageInfo(
            baseDamage: attackStats.Damage,
            damageType: DamageType.Physical,
            flags: DamageFlags.CanCrit);

        // Get attacker position for event
        var attackerPos = world.TryGetComponent(attacker, out Position pos) ? pos.Value : Vector2.Zero;

        // Apply damage
        _damageService.ApplyDamage(attacker, target, damageInfo, attackerPos);

        // Apply knockback if target can be knocked back
        if (world.TryGetComponent(target, out Velocity _))
        {
            // Calculate knockback direction and strength
            var knockbackDirection = -normal; // Push away from attacker
            if (knockbackDirection.LengthSquared() < 0.001f)
            {
                // Fallback: use direction from attacker to target
                if (world.TryGetComponent(target, out Position targetPos))
                {
                    knockbackDirection = targetPos.Value - attackerPos;
                    if (knockbackDirection.LengthSquared() > 0.001f)
                        knockbackDirection.Normalize();
                    else
                        knockbackDirection = new Vector2(1, 0); // Arbitrary fallback
                }
            }
            else
            {
                knockbackDirection.Normalize();
            }

            var knockbackStrength = 200f; // Base knockback speed
            var knockbackVelocity = knockbackDirection * knockbackStrength;
            KnockbackSystem.ApplyKnockback(world, target, knockbackVelocity, 0.15f);
        }

        // Record damage in cooldown
        cooldown.RecordDamage(attacker.Id, _gameTime);
        world.SetComponent(target, cooldown);
    }
}
