using System;
using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class DamageNumberLifecycleSystem : IUpdateSystem
{
    private readonly Random _random = new();
    private EcsWorld _world = null!;

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        var deltaSeconds = context.DeltaSeconds;

        world.ForEach<DamageNumber, Position, Lifetime>(
            (Entity entity, ref DamageNumber number, ref Position position, ref Lifetime lifetime) =>
            {
                var floatDistance = number.FloatSpeed * deltaSeconds;
                position.Value += new Vector2(number.HorizontalJitter * deltaSeconds, -floatDistance);
                world.SetComponent(entity, position);
            });
    }

    private void OnEntityDamaged(EntityDamagedEvent evt)
    {
        if (!_world.TryGetComponent(evt.Target, out Position position))
        {
            return;
        }

        var targetFaction = _world.TryGetComponent(evt.Target, out Faction faction) ? faction : Faction.Neutral;
        SpawnDamageNumber(_world, position.Value, evt.Amount, evt.SourceFaction, targetFaction);
    }

    private void SpawnDamageNumber(EcsWorld world, Vector2 position, float amount, Faction sourceFaction, Faction targetFaction)
    {
        if (amount <= 0f || targetFaction == Faction.Player)
        {
            return;
        }

        var numberEntity = world.CreateEntity();
        var lifetimeSeconds = 0.9f;
        var floatSpeed = 22f;
        var horizontalJitter = (_random.NextSingle() - 0.5f) * 14f;
        var scale = targetFaction == Faction.Player ? 0.55f : 0.6f;
        var color = sourceFaction == Faction.Player ? Color.Gold : Color.Crimson;
        var spawnOffset = new Vector2(horizontalJitter * 0.5f, -18f);

        world.SetComponent(numberEntity, new Position(position + spawnOffset));
        world.SetComponent(numberEntity, new DamageNumber(amount, lifetimeSeconds, floatSpeed, horizontalJitter, scale, color));
        world.SetComponent(numberEntity, new Lifetime(lifetimeSeconds));
    }
}
