using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class CombatSystem : IUpdateSystem
{
    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;
        world.ForEach<AttackStats>(
            (Entity _, ref AttackStats attack) =>
            {
                attack.CooldownTimer = MathF.Max(0f, attack.CooldownTimer - deltaSeconds);
            });

        HandlePlayerAttacks(world);
        HandleEnemyContact(world);
    }

    private static void HandlePlayerAttacks(EcsWorld world)
    {
        world.ForEach<PlayerTag, InputIntent, AttackStats>(
            (Entity entity, ref PlayerTag _, ref InputIntent intent, ref AttackStats attack) =>
            {
                if (!intent.Attack || attack.CooldownTimer > 0f)
                {
                    return;
                }

                attack.CooldownTimer = attack.CooldownSeconds;

                if (!world.TryGetComponent(entity, out Position position) ||
                    !world.TryGetComponent(entity, out Hitbox hitbox) ||
                    !world.TryGetComponent(entity, out Faction faction))
                {
                    return;
                }

                ApplyDamageInRange(world, position.Value, hitbox.Radius, attack.Damage, attack.Range, faction);
            });
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

                health.Current = MathF.Max(0f, health.Current - damage);
                world.SetComponent(target, health);

                if (world.TryGetComponent(target, out DamageEvent existing))
                {
                    existing.Amount += damageApplied;
                    existing.SourcePosition = origin;
                    existing.SourceFaction = attackerFaction;
                    world.SetComponent(target, existing);
                }
                else
                {
                    world.SetComponent(target, new DamageEvent(damageApplied, origin, attackerFaction));
                }

                hit = true;
            });

        return hit;
    }
}

