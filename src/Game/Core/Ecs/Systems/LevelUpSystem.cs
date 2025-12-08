using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Ecs.Config;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Applies stat bonuses when the player levels up and creates notifications.
/// </summary>
internal sealed class LevelUpSystem : IUpdateSystem
{
    private readonly ProgressionConfig _config;
    private EcsWorld? _world;

    public LevelUpSystem(ProgressionConfig config)
    {
        _config = config;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        world.EventBus.Subscribe<PlayerLeveledUpEvent>(OnPlayerLeveledUp);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Event-driven system, no per-frame update needed
    }

    private void OnPlayerLeveledUp(PlayerLeveledUpEvent evt)
    {
        if (_world == null)
            return;

        var player = evt.Player;

        // Apply damage bonus
        if (_world.TryGetComponent<AttackStats>(player, out var attackStats))
        {
            attackStats.Damage += _config.DamageBonusPerLevel;
            _world.SetComponent(player, attackStats);
        }

        // Apply move speed bonus
        if (_world.TryGetComponent<MoveSpeed>(player, out var moveSpeed))
        {
            moveSpeed.Value += _config.MoveSpeedBonusPerLevel;
            _world.SetComponent(player, moveSpeed);

            if (_world.TryGetComponent(player, out BaseMoveSpeed baseMove))
            {
                baseMove.Value += _config.MoveSpeedBonusPerLevel;
                _world.SetComponent(player, baseMove);
            }
        }

        // Apply health bonus
        if (_world.TryGetComponent<Health>(player, out var health))
        {
            var ratio = health.Ratio;
            health.Max += _config.HealthBonusPerLevel;
            // Maintain health ratio when increasing max
            health.Current = health.Max * ratio;
            _world.SetComponent(player, health);
        }

        if (_world.TryGetComponent(player, out ComputedStats computed))
        {
            computed.IsDirty = true;
            _world.SetComponent(player, computed);
        }

        // Create level-up notification
        var notificationEntity = _world.CreateEntity();
        _world.SetComponent(notificationEntity, new WaveNotification(
            $"LEVEL {evt.NewLevel}!",
            duration: 2.0f));
        _world.SetComponent(notificationEntity, new Lifetime(2.0f));
    }
}
