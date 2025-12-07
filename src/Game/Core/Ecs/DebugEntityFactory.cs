using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs;

internal sealed class DebugEntityFactory
{
    private readonly EcsWorld _world;
    private readonly Random _random = new();

    public DebugEntityFactory(EcsWorld world)
    {
        _world = world;
    }

    public Entity CreateDebugAgent(Vector2 position, Color color, float speed = 65f, float lifetimeSeconds = 12f)
    {
        var entity = _world.CreateEntity();
        _world.SetComponent(entity, new Position(position));
        _world.SetComponent(entity, new Velocity(Vector2.Zero));
        _world.SetComponent(entity, new Health(30f, 30f));
        _world.SetComponent(entity, Faction.Neutral);
        _world.SetComponent(entity, new Hitbox(6f));
        _world.SetComponent(entity, new AttackStats(damage: 5f, cooldownSeconds: 1.5f, range: 30f));
        _world.SetComponent(entity, new RenderDebug(color, size: 8f));
        _world.SetComponent(entity, new Lifetime(lifetimeSeconds));

        var phase = _random.NextSingle() * MathF.Tau;
        var turnRate = MathHelper.Lerp(0.6f, 1.6f, _random.NextSingle());
        var mover = new DebugMover(speed, turnRate, phase);
        _world.SetComponent(entity, mover);

        return entity;
    }

    public void SeedDebugAgents(int count, float radius)
    {
        for (var i = 0; i < count; i++)
        {
            var angle = _random.NextSingle() * MathF.Tau;
            var distance = radius * _random.NextSingle();
            var position = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
            var color = Color.Lerp(Color.Cyan, Color.Yellow, _random.NextSingle());
            var speed = MathHelper.Lerp(50f, 90f, _random.NextSingle());
            var lifetime = MathHelper.Lerp(8f, 16f, _random.NextSingle());
            CreateDebugAgent(position, color, speed, lifetime);
        }
    }
}

