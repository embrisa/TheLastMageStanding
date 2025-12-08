using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// Grants perk points when the player gains a meta level (hub-only progression).
/// In-run level-ups no longer grant perk points.
/// </summary>
internal sealed class PerkPointGrantSystem : IUpdateSystem
{
    private readonly PerkTreeConfig _config;
    private EcsWorld? _world;

    public PerkPointGrantSystem(PerkTreeConfig config)
    {
        _config = config;
    }

    public void Initialize(EcsWorld world)
    {
        _world = world;
        // Subscribe to meta level-up events instead of in-run level-ups
        world.EventBus.Subscribe<MetaLevelUpEvent>(OnMetaLevelUp);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        // Event-driven system, no per-frame update needed
    }

    private void OnMetaLevelUp(MetaLevelUpEvent evt)
    {
        if (_world == null)
            return;

        // Find player entity
        Entity? playerEntity = null;
        _world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) => playerEntity = entity);

        if (!playerEntity.HasValue)
            return;

        var player = playerEntity.Value;

        // Ensure player has PerkPoints component
        if (!_world.TryGetComponent<PerkPoints>(player, out var perkPoints))
        {
            perkPoints = new PerkPoints(0, 0);
        }

        // Grant points
        perkPoints.AvailablePoints += _config.PointsPerLevel;
        perkPoints.TotalPointsEarned += _config.PointsPerLevel;

        _world.SetComponent(player, perkPoints);

        // Publish event for UI notification
        _world.EventBus.Publish(new PerkPointsGrantedEvent(player, _config.PointsPerLevel, perkPoints.AvailablePoints));
    }
}
