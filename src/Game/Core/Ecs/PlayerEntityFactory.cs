using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;

namespace TheLastMageStanding.Game.Core.Ecs;

internal sealed class PlayerEntityFactory
{
    private readonly EcsWorld _world;

    public PlayerEntityFactory(EcsWorld world)
    {
        _world = world;
    }

    public Entity CreatePlayer(Vector2 spawnPosition)
    {
        var entity = _world.CreateEntity();

        _world.SetComponent(entity, new PlayerTag());
        _world.SetComponent(entity, new CameraTarget());
        _world.SetComponent(entity, Faction.Player);
        _world.SetComponent(entity, new Position(spawnPosition));
        _world.SetComponent(entity, new Velocity(Vector2.Zero));
        _world.SetComponent(entity, new MoveSpeed(220f));
        _world.SetComponent(entity, new InputIntent());
        _world.SetComponent(entity, new AttackStats(damage: 20f, cooldownSeconds: 0.35f, range: 42f));
        _world.SetComponent(entity, new Health(current: 100f, max: 100f));
        _world.SetComponent(entity, new Hitbox(radius: 6f));

        return entity;
    }
}

