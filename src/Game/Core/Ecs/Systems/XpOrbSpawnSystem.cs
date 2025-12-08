using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Spawns XP orbs at enemy death positions.
/// </summary>
internal sealed class XpOrbSpawnSystem : IUpdateSystem
{
    private readonly ProgressionConfig _config;
    private EcsWorld? _world;

    public XpOrbSpawnSystem(ProgressionConfig config)
    {
        _config = config;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Event-driven system, no per-frame update needed
    }

    private void OnEnemyDied(EnemyDiedEvent evt)
    {
        if (_world == null)
            return;

        // Spawn XP orb at enemy position
        var orbEntity = _world.CreateEntity();
        _world.SetComponent(orbEntity, new Position(evt.Position));
        _world.SetComponent(orbEntity, new Velocity(Vector2.Zero));
        _world.SetComponent(orbEntity, new XpOrb(_config.XpPerEnemy));
        _world.SetComponent(orbEntity, new Lifetime(_config.OrbLifetimeSeconds));
    }
}
