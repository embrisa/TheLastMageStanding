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
        var deltaSeconds = context.DeltaSeconds;
        world.ForEach<AttackStats>(
            (Entity _, ref AttackStats attack) =>
            {
                attack.CooldownTimer = MathF.Max(0f, attack.CooldownTimer - deltaSeconds);
            });

        HandleEnemyContact(world);
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
            !_world.TryGetComponent(entity, out Hitbox hitbox) ||
            !_world.TryGetComponent(entity, out Faction faction))
        {
            return;
        }

        ApplyDamageInRange(_world, position.Value, hitbox.Radius, attack.Damage, attack.Range, faction);
    }

    private static void HandleEnemyContact(EcsWorld world)
    {
        world.ForEach<AttackStats, Faction, Position>(
            (Entity entity, ref AttackStats attack, ref Faction faction, ref Position position) =>
            {
                if (faction != Faction.Enemy || attack.CooldownTimer > 0f)
                {
                    return;
                }

                if (!world.TryGetComponent(entity, out Hitbox hitbox))
                {
                    return;
                }

                var hit = ApplyDamageInRange(world, position.Value, hitbox.Radius, attack.Damage, attack.Range, faction);
                if (hit)
                {
                    attack.CooldownTimer = attack.CooldownSeconds;
                }
            });
    }

    private static bool ApplyDamageInRange(
        EcsWorld world,
        Vector2 origin,
        float attackerRadius,
        float damage,
        float range,
        Faction attackerFaction)
    {
        var hit = false;
        var reachBase = attackerRadius + range;

        world.ForEach<Position, Hitbox, Faction>(
            (Entity target, ref Position targetPosition, ref Hitbox targetHitbox, ref Faction targetFaction) =>
            {
                if (targetFaction == attackerFaction)
                {
                    return;
                }

                var reach = reachBase + targetHitbox.Radius;
                if (Vector2.DistanceSquared(origin, targetPosition.Value) > reach * reach)
                {
                    return;
                }

                if (!world.TryGetComponent(target, out Health health))
                {
                    return;
                }

                if (health.IsDead)
                {
                    return;
                }

                var damageApplied = MathF.Min(health.Current, damage);
                if (damageApplied <= 0f)
                {
                    return;
                }

                world.EventBus.Publish(new EntityDamagedEvent(target, damageApplied, origin, attackerFaction));

                hit = true;
            });

        return hit;
    }
}

