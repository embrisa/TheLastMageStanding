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
    private Entity? _sessionEntity;

    public WaveSchedulerSystem(EnemyWaveConfig config)
    {
        _config = config;
    }

    public void Initialize(EcsWorld world)
    {
        world.EventBus.Subscribe<SessionRestartedEvent>(_ => Reset());
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Check session state - halt wave spawning if game over
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

        _waveTimer += context.DeltaSeconds;
        if (_waveTimer < _config.WaveIntervalSeconds)
        {
            return;
        }

        _waveTimer = 0f;
        var hasStage = TryGetStageRunState(world, out var stageState);

        if (_waveIndex > 0)
        {
            world.EventBus.Publish(new WaveCompletedEvent(_waveIndex));

            if (hasStage && !stageState.IsBossStage && stageState.MaxWaves > 0 && _waveIndex >= stageState.MaxWaves)
            {
                world.EventBus.Publish(new StageRunCompletedEvent(stageState.StageId, isVictory: true, bossKilled: false));
                return;
            }
        }

        // If we're in a boss stage and the boss wave has already started, don't schedule additional waves.
        if (hasStage && stageState.IsBossStage && stageState.BossWaveIndex > 0 && _waveIndex >= stageState.BossWaveIndex)
        {
            return;
        }

        // In non-boss stages, stop once the stage wave cap has been reached.
        if (hasStage && !stageState.IsBossStage && stageState.MaxWaves > 0 && _waveIndex >= stageState.MaxWaves)
        {
            return;
        }

        _waveIndex++;

        world.EventBus.Publish(new WaveStartedEvent(_waveIndex));

        if (!TryGetPlayerPosition(world, out var playerPosition))
        {
            return;
        }

        if (hasStage && stageState.IsBossStage && stageState.BossWaveIndex > 0 && _waveIndex == stageState.BossWaveIndex)
        {
            var bossArchetypeId = stageState.BossArchetypeId;
            if (!string.IsNullOrWhiteSpace(bossArchetypeId))
            {
                var spawnPosition = playerPosition + GetSpawnOffset(300f, 420f);
                var request = world.CreateEntity();
                world.SetComponent(request, new EnemySpawnRequest(spawnPosition, EnemyWaveConfig.CreateBossArchetype(bossArchetypeId)));
            }

            return;
        }

        var activeEnemies = CountAliveEnemies(world);
        if (activeEnemies >= _config.MaxActiveEnemies)
        {
            return;
        }

        // Count active elites and bosses to cap their spawns
        var (activeElites, _) = CountElitesAndBosses(world);
        var (activeChargers, activeProtectors, activeBuffers) = CountActiveRoles(world);
        const int maxElites = 3; // At most 3 elites at once
        const int maxChargers = 2;
        const int maxProtectors = 1;
        const int maxBuffers = 1;

        var requestedCount = _config.BaseEnemiesPerWave + (_waveIndex - 1) * _config.EnemiesPerWaveGrowth;
        var spawnCount = Math.Min(requestedCount, _config.MaxActiveEnemies - activeEnemies);
        for (var i = 0; i < spawnCount; i++)
        {
            var archetype = _config.ChooseArchetype(_waveIndex, _random);
            IReadOnlyList<EliteModifierType>? modifiers = null;

            // Enforce elite/boss caps - reroll if we hit the limit
            var rerollAttempts = 0;
            while (rerollAttempts < 5)
            {
                // Bosses are stage-driven; prevent random boss spawns.
                if (archetype.Tier == EnemyTier.Boss)
                {
                    archetype = _config.ChooseArchetype(_waveIndex, _random);
                    rerollAttempts++;
                    continue;
                }

                if (archetype.Tier == EnemyTier.Elite && activeElites >= maxElites)
                {
                    archetype = _config.ChooseArchetype(_waveIndex, _random);
                    rerollAttempts++;
                    continue;
                }

                if (archetype.RoleConfig?.Role == EnemyRole.Charger && activeChargers >= maxChargers)
                {
                    archetype = _config.ChooseArchetype(_waveIndex, _random);
                    rerollAttempts++;
                    continue;
                }

                if (archetype.RoleConfig?.Role == EnemyRole.Protector && activeProtectors >= maxProtectors)
                {
                    archetype = _config.ChooseArchetype(_waveIndex, _random);
                    rerollAttempts++;
                    continue;
                }

                if (archetype.RoleConfig?.Role == EnemyRole.Buffer && activeBuffers >= maxBuffers)
                {
                    archetype = _config.ChooseArchetype(_waveIndex, _random);
                    rerollAttempts++;
                    continue;
                }

                break;
            }

            // Track spawned elites/bosses for this wave
            if (archetype.Tier == EnemyTier.Elite)
            {
                activeElites++;
                modifiers = _config.ModifierConfig.RollModifiers(_waveIndex, _random);
            }

            // Track role caps for this wave
            if (archetype.RoleConfig?.Role == EnemyRole.Charger)
            {
                activeChargers++;
            }
            else if (archetype.RoleConfig?.Role == EnemyRole.Protector)
            {
                activeProtectors++;
            }
            else if (archetype.RoleConfig?.Role == EnemyRole.Buffer)
            {
                activeBuffers++;
            }

            var offset = GetSpawnOffsetForRole(archetype.RoleConfig);
            var spawnPosition = playerPosition + offset;

            var request = world.CreateEntity();
            world.SetComponent(request, new EnemySpawnRequest(spawnPosition, archetype, modifiers));
        }
    }

    private Vector2 GetSpawnOffset(float minRadius, float maxRadius)
    {
        var angle = _random.NextSingle() * MathF.Tau;
        var distance = MathHelper.Lerp(minRadius, maxRadius, _random.NextSingle());
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }

    private Vector2 GetSpawnOffsetForRole(AiRoleConfig? roleConfig)
    {
        var minRadius = _config.SpawnRadiusMin;
        var maxRadius = _config.SpawnRadiusMax;

        if (roleConfig.HasValue)
        {
            switch (roleConfig.Value.Role)
            {
                case EnemyRole.Protector:
                    minRadius = 250f;
                    maxRadius = 380f;
                    break;
                case EnemyRole.Buffer:
                    minRadius = 240f;
                    maxRadius = 360f;
                    break;
                case EnemyRole.Charger:
                    minRadius = _config.SpawnRadiusMin;
                    maxRadius = _config.SpawnRadiusMax;
                    break;
            }
        }

        return GetSpawnOffset(minRadius, maxRadius);
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

    private static (int elites, int bosses) CountElitesAndBosses(EcsWorld world)
    {
        var eliteCount = 0;
        var bossCount = 0;

        world.ForEach<Health, EliteTag>(
            (Entity _, ref Health health, ref EliteTag _) =>
            {
                if (!health.IsDead)
                {
                    eliteCount++;
                }
            });

        world.ForEach<Health, BossTag>(
            (Entity _, ref Health health, ref BossTag _) =>
            {
                if (!health.IsDead)
                {
                    bossCount++;
                }
            });

        return (eliteCount, bossCount);
    }

    private static (int chargers, int protectors, int buffers) CountActiveRoles(EcsWorld world)
    {
        var chargers = 0;
        var protectors = 0;
        var buffers = 0;

        world.ForEach<AiRoleConfig, Health>(
            (Entity _, ref AiRoleConfig role, ref Health health) =>
            {
                if (health.IsDead)
                {
                    return;
                }

                switch (role.Role)
                {
                    case EnemyRole.Charger:
                        chargers++;
                        break;
                    case EnemyRole.Protector:
                        protectors++;
                        break;
                    case EnemyRole.Buffer:
                        buffers++;
                        break;
                }
            });

        return (chargers, protectors, buffers);
    }

    private void Reset()
    {
        _waveTimer = 0f;
        _waveIndex = 0;
    }

    private bool TryGetStageRunState(EcsWorld world, out StageRunState stageState)
    {
        if (_sessionEntity.HasValue && world.IsAlive(_sessionEntity.Value) && world.TryGetComponent(_sessionEntity.Value, out stageState))
        {
            return true;
        }

        _sessionEntity = null;
        var found = false;
        var captured = new StageRunState();
        world.ForEach<GameSession, StageRunState>((Entity entity, ref GameSession _, ref StageRunState state) =>
        {
            _sessionEntity = entity;
            captured = state;
            found = true;
        });

        stageState = captured;
        return found;
    }
}
