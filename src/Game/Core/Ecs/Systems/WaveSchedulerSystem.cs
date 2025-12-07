using Microsoft.Xna.Framework;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

internal sealed class WaveSchedulerSystem : IUpdateSystem
{
    private readonly EnemyWaveConfig _config;
    private readonly Random _random = new();

    private float _waveTimer;
    private int _waveIndex;

    public WaveSchedulerSystem(EnemyWaveConfig config)
    {
        _config = config;
    }

    public void Initialize(EcsWorld world)
    {
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        _waveTimer += context.DeltaSeconds;
        if (_waveTimer < _config.WaveIntervalSeconds)
        {
            return;
        }

        if (_waveIndex > 0)
        {
            world.EventBus.Publish(new WaveCompletedEvent(_waveIndex));
        }

        _waveTimer = 0f;
        _waveIndex++;

        world.EventBus.Publish(new WaveStartedEvent(_waveIndex));

        if (!TryGetPlayerPosition(world, out var playerPosition))
        {
            return;
        }

        var activeEnemies = CountAliveEnemies(world);
        if (activeEnemies >= _config.MaxActiveEnemies)
        {
            return;
        }

        var requestedCount = _config.BaseEnemiesPerWave + (_waveIndex - 1) * _config.EnemiesPerWaveGrowth;
        var spawnCount = Math.Min(requestedCount, _config.MaxActiveEnemies - activeEnemies);
        for (var i = 0; i < spawnCount; i++)
        {
            var offset = GetSpawnOffset(_config.SpawnRadiusMin, _config.SpawnRadiusMax);
            var spawnPosition = playerPosition + offset;

            var archetype = _config.ChooseArchetype(_waveIndex, _random);
            var request = world.CreateEntity();
            world.SetComponent(request, new EnemySpawnRequest(spawnPosition, archetype));
        }
    }

    private Vector2 GetSpawnOffset(float minRadius, float maxRadius)
    {
        var angle = _random.NextSingle() * MathF.Tau;
        var distance = MathHelper.Lerp(minRadius, maxRadius, _random.NextSingle());
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }

    private static bool TryGetPlayerPosition(EcsWorld world, out Vector2 position)
    {
        var found = false;
        var captured = Vector2.Zero;

        world.ForEach<PlayerTag, Position>(
            (Entity _, ref PlayerTag _, ref Position pos) =>
            {
                captured = pos.Value;
                found = true;
            });

        position = captured;
        return found;
    }

    private static int CountAliveEnemies(EcsWorld world)
    {
        var count = 0;
        world.ForEach<Health, Faction>(
            (Entity _, ref Health health, ref Faction faction) =>
            {
                if (faction == Faction.Enemy && !health.IsDead)
                {
                    count++;
                }
            });

        return count;
    }
}

