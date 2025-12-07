using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class CombatSystem : IUpdateSystem
{
    private EcsWorld _world = null!;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<PlayerAttackIntentEvent>(OnPlayerAttackIntent);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Check session state - halt combat if game over
        var sessionActive = false;
        world.ForEach<GameSession>((Entity _, ref GameSession session) =>
        {
            if (session.State == GameState.Playing)
            {
                sessionActive = true;
            }
        });
        if (!sessionActive)
        {
            return;
        }

        var deltaSeconds = context.DeltaSeconds;
        world.ForEach<AttackStats>(
            (Entity _, ref AttackStats attack) =>
            {
                attack.CooldownTimer = MathF.Max(0f, attack.CooldownTimer - deltaSeconds);
            });
    }

    private void OnPlayerAttackIntent(PlayerAttackIntentEvent evt)
    {
        var entity = evt.Player;
        if (!_world.TryGetComponent(entity, out AttackStats attack))
        {
            return;
        }

        if (attack.CooldownTimer > 0f)
        {
            return;
        }

        attack.CooldownTimer = attack.CooldownSeconds;
        _world.SetComponent(entity, attack);

        if (!_world.TryGetComponent(entity, out Position position) ||
            !_world.TryGetComponent(entity, out Faction faction))
        {
            return;
        }

        // Get melee config or use defaults
        var meleeConfig = _world.TryGetComponent(entity, out MeleeAttackConfig config)
            ? config
            : new MeleeAttackConfig(attack.Range, Vector2.Zero, 0.15f);

        // Spawn hitbox entity
        SpawnAttackHitbox(_world, entity, position.Value, meleeConfig, attack.Damage, faction);
    }

    /// <summary>
    /// Creates a transient hitbox entity for melee attacks.
    /// </summary>
    private static void SpawnAttackHitbox(
        EcsWorld world,
        Entity owner,
        Vector2 ownerPosition,
        MeleeAttackConfig config,
        float damage,
        Faction ownerFaction)
    {
        var hitboxEntity = world.CreateEntity();
        
        // Position the hitbox at offset from owner
        var hitboxPosition = ownerPosition + config.HitboxOffset;
        world.SetComponent(hitboxEntity, new Position(hitboxPosition));

        // Create attack hitbox component
        world.SetComponent(hitboxEntity, new AttackHitbox(owner, damage, ownerFaction, config.Duration));

        // Determine collision layer based on faction
        var hitboxLayer = ownerFaction == Faction.Player ? CollisionLayer.Projectile : CollisionLayer.Enemy;
        var targetLayer = ownerFaction == Faction.Player ? CollisionLayer.Enemy : CollisionLayer.Player;

        // Create trigger collider for the hitbox
        world.SetComponent(
            hitboxEntity,
            Collider.CreateCircle(
                config.HitboxRadius,
                hitboxLayer,
                targetLayer,
                isTrigger: true
            )
        );
    }
}
