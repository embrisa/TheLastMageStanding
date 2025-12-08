using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Player;

namespace TheLastMageStanding.Game.Core.Ecs.Systems;

/// <summary>
/// System that auto-saves perks periodically and on perk changes during a run.
/// </summary>
internal sealed class PerkAutoSaveSystem : IUpdateSystem
{
    private readonly PerkPersistenceService _persistenceService;
    private float _timeSinceLastSave;
    private bool _needsSave;
    private const float SaveInterval = 30f; // Save every 30 seconds

    public PerkAutoSaveSystem()
    {
        _persistenceService = new PerkPersistenceService();
    }

    public void Initialize(EcsWorld world)
    {
        // Load saved perks on initialization
        LoadPerksForPlayer(world);

        // Subscribe to perk events to trigger saves
        world.EventBus.Subscribe<PerkAllocatedEvent>(OnPerkChanged);
        world.EventBus.Subscribe<PerksRespecedEvent>(OnPerkChanged);
        world.EventBus.Subscribe<SessionRestartedEvent>(OnSessionRestarted);
    }

    public void Update(EcsWorld world, in EcsUpdateContext context)
    {
        _timeSinceLastSave += context.DeltaSeconds;

        // Auto-save periodically or when changes are pending
        if (_needsSave || _timeSinceLastSave >= SaveInterval)
        {
            SavePerksForPlayer(world);
            _timeSinceLastSave = 0f;
            _needsSave = false;
        }
    }

    /// <summary>
    /// Manually trigger a save (e.g., on map transition or important events).
    /// </summary>
    public void SaveNow(EcsWorld world)
    {
        SavePerksForPlayer(world);
        _timeSinceLastSave = 0f;
        _needsSave = false;
    }

    private void OnPerkChanged(PerkAllocatedEvent evt)
    {
        _needsSave = true;
    }

    private void OnPerkChanged(PerksRespecedEvent evt)
    {
        _needsSave = true;
    }

    private void OnSessionRestarted(SessionRestartedEvent evt)
    {
        // Clear perks on restart
        _persistenceService.ClearSave();
    }

    private void SavePerksForPlayer(EcsWorld world)
    {
        // Find player entity
        Entity? playerEntity = null;
        world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) => playerEntity = entity);

        if (!playerEntity.HasValue)
            return;

        var player = playerEntity.Value;

        // Get perk components
        if (!world.TryGetComponent<PerkPoints>(player, out var perkPoints))
            return;

        if (!world.TryGetComponent<PlayerPerks>(player, out var playerPerks))
            return;

        // Create snapshot
        var snapshot = new PerkSnapshot
        {
            AvailablePoints = perkPoints.AvailablePoints,
            TotalPointsEarned = perkPoints.TotalPointsEarned,
            AllocatedRanks = new Dictionary<string, int>(playerPerks.AllocatedRanks)
        };

        _persistenceService.SavePerks(snapshot);
    }

    private void LoadPerksForPlayer(EcsWorld world)
    {
        if (!_persistenceService.HasSave())
            return;

        var snapshot = _persistenceService.LoadPerks();
        if (snapshot == null)
            return;

        // Find player entity
        Entity? playerEntity = null;
        world.ForEach<PlayerTag>((Entity entity, ref PlayerTag _) => playerEntity = entity);

        if (!playerEntity.HasValue)
            return;

        var player = playerEntity.Value;

        // Restore perk points
        var perkPoints = new PerkPoints(snapshot.AvailablePoints, snapshot.TotalPointsEarned);
        world.SetComponent(player, perkPoints);

        // Restore allocated perks
        var playerPerks = new PlayerPerks
        {
            AllocatedRanks = new Dictionary<string, int>(snapshot.AllocatedRanks)
        };
        world.SetComponent(player, playerPerks);

        // Trigger perk effect application
        world.EventBus.Publish(new PerksRespecedEvent(player));
    }

    /// <summary>
    /// Clear the save file (e.g., on run end or new game).
    /// </summary>
    public void ClearSave()
    {
        _persistenceService.ClearSave();
    }
}
