using TheLastMageStanding.Game.Core.Events;
using TheLastMageStanding.Game.Core.Diagnostics;

namespace TheLastMageStanding.Game.Core.MetaProgression;

/// <summary>
/// Manages meta progression state during gameplay.
/// Coordinates profile loading/saving, run tracking, and XP calculation.
/// </summary>
public sealed class MetaProgressionManager : IDisposable
{
    private const string LogCategory = "MetaProgression";
    private readonly EventSubscriptionScope _subscriptions = new();
    private readonly PlayerProfileService _profileService;
    private readonly RunHistoryService _historyService;
    private readonly IEventBus _eventBus;
    private readonly string _slotId;

    private PlayerProfile _currentProfile;
    private RunSession? _currentRun;
    private bool _disposed;

    public PlayerProfile CurrentProfile => _currentProfile;
    public RunSession? CurrentRun => _currentRun;
    public string SlotId => _slotId;
    public RunHistoryService HistoryService => _historyService;

    internal MetaProgressionManager(IEventBus eventBus, SlotPersistenceScope persistence)
    {
        _slotId = persistence.SlotId;
        _profileService = persistence.PlayerProfile;
        _historyService = persistence.RunHistory;
        _eventBus = eventBus;

        // Load profile
        _currentProfile = _profileService.LoadProfile();

        // Subscribe to events
        _subscriptions.Subscribe<RunStartedEvent>(eventBus, OnRunStarted);
        _subscriptions.Subscribe<RunEndedEvent>(eventBus, OnRunEnded);
        _subscriptions.Subscribe<GoldCollectedEvent>(eventBus, OnGoldCollected);
        _subscriptions.Subscribe<EquipmentCollectedEvent>(eventBus, OnEquipmentCollected);
        _subscriptions.Subscribe<SessionRestartedEvent>(eventBus, OnSessionRestarted);
        _subscriptions.Subscribe<PlayerDiedEvent>(eventBus, OnPlayerDied);
        _subscriptions.Subscribe<WaveCompletedEvent>(eventBus, OnWaveCompleted);
        _subscriptions.Subscribe<EntityDamagedEvent>(eventBus, OnEntityDamaged);
        _subscriptions.Subscribe<StageRunStartedEvent>(eventBus, OnStageRunStarted);
        _subscriptions.Subscribe<StageRunCompletedEvent>(eventBus, OnStageRunCompleted);
        _subscriptions.Subscribe<RunMetaXpBonusEvent>(eventBus, OnRunMetaXpBonus);
    }

    /// <summary>
    /// Saves the current profile to disk.
    /// </summary>
    public void SaveProfile()
    {
        _profileService.SaveProfile(_currentProfile);
    }

    /// <summary>
    /// Gets equipped weapon from inventory.
    /// </summary>
    public EquipmentItem? GetEquippedWeapon()
    {
        if (string.IsNullOrEmpty(_currentProfile.EquippedWeaponId))
            return null;

        return _currentProfile.EquipmentInventory
            .FirstOrDefault(e => e.Id == _currentProfile.EquippedWeaponId);
    }

    /// <summary>
    /// Gets equipped armor from inventory.
    /// </summary>
    public EquipmentItem? GetEquippedArmor()
    {
        if (string.IsNullOrEmpty(_currentProfile.EquippedArmorId))
            return null;

        return _currentProfile.EquipmentInventory
            .FirstOrDefault(e => e.Id == _currentProfile.EquippedArmorId);
    }

    /// <summary>
    /// Gets equipped accessories from inventory.
    /// </summary>
    public List<EquipmentItem> GetEquippedAccessories()
    {
        return _currentProfile.EquipmentInventory
            .Where(e => _currentProfile.EquippedAccessoryIds.Contains(e.Id))
            .ToList();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _subscriptions.Dispose();

        _currentRun = null;
    }

    private void OnRunStarted(RunStartedEvent evt)
    {
        _currentRun = new RunSession
        {
            StartTime = DateTime.UtcNow,
            RunId = Guid.NewGuid().ToString(),
            StageId = null,
            StageCompleted = false,
            BossKilled = false,
            BonusMetaXp = 0
        };

        RuntimeLog.Info(LogCategory, $"Run started: {_currentRun.RunId}");
    }

    private void OnRunEnded(RunEndedEvent evt)
    {
        if (_currentRun == null)
            return;

        FinalizeRun();
    }

    private void OnSessionRestarted(SessionRestartedEvent evt)
    {
        // Session restart acts like run start
        _currentRun = new RunSession
        {
            StartTime = DateTime.UtcNow,
            RunId = Guid.NewGuid().ToString(),
            StageId = null,
            StageCompleted = false,
            BossKilled = false,
            BonusMetaXp = 0
        };

        RuntimeLog.Info(LogCategory, $"Run restarted: {_currentRun.RunId}");
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        // Player death triggers run end
        if (_currentRun != null)
        {
            _currentRun.CauseOfDeath = "Defeated";
            FinalizeRun();
        }
    }

    private void OnGoldCollected(GoldCollectedEvent evt)
    {
        if (_currentRun != null)
        {
            _currentRun.GoldCollected += evt.Amount;
        }
    }

    private void OnEquipmentCollected(EquipmentCollectedEvent evt)
    {
        if (_currentRun != null)
        {
            // Track in run session (actual equipment item would be resolved from game data)
            // For now, just track the ID
        }
    }

    private void OnWaveCompleted(WaveCompletedEvent evt)
    {
        if (_currentRun != null)
        {
            _currentRun.WaveReached = Math.Max(_currentRun.WaveReached, evt.WaveIndex);
        }
    }

    private void OnEntityDamaged(EntityDamagedEvent evt)
    {
        if (_currentRun == null)
            return;

        // Track damage (Amount is the damage dealt)
        // This is simplified - in a complete implementation you'd check if Source is player
        _currentRun.TotalDamageDealt += evt.Amount;
    }

    private void OnStageRunStarted(StageRunStartedEvent evt)
    {
        if (_currentRun == null)
        {
            return;
        }

        _currentRun.StageId = evt.StageId;
        _currentRun.StageCompleted = false;
        _currentRun.BossKilled = false;
    }

    private void OnStageRunCompleted(StageRunCompletedEvent evt)
    {
        if (_currentRun == null)
        {
            return;
        }

        _currentRun.StageId ??= evt.StageId;
        _currentRun.StageCompleted = evt.IsVictory;
        _currentRun.BossKilled = evt.BossKilled;
        if (evt.IsVictory)
        {
            _currentRun.CauseOfDeath = "Stage Completed";
        }

        if (evt.IsVictory &&
            !string.IsNullOrWhiteSpace(evt.StageId) &&
            !_currentProfile.CompletedStages.Contains(evt.StageId))
        {
            _currentProfile.CompletedStages.Add(evt.StageId);
        }
    }

    private void OnRunMetaXpBonus(RunMetaXpBonusEvent evt)
    {
        if (_currentRun == null)
        {
            return;
        }

        _currentRun.BonusMetaXp += Math.Max(0, evt.Amount);
    }

    private void FinalizeRun()
    {
        if (_currentRun == null)
            return;

        _currentRun.EndTime = DateTime.UtcNow;

        // Calculate meta XP and gold rewards
        var metaXp = MetaProgressionCalculator.CalculateMetaXP(_currentRun);
        var goldReward = MetaProgressionCalculator.CalculateGoldReward(_currentRun);

        // Update run session
        _currentRun.MetaXpEarned = metaXp;
        _currentRun.GoldCollected += goldReward; // Add calculated bonus to collected gold

        // Update profile
        var oldLevel = _currentProfile.MetaLevel;
        _currentProfile.TotalMetaXp += metaXp;
        _currentProfile.TotalGold += _currentRun.GoldCollected;
        _currentProfile.TotalRuns++;
        _currentProfile.BestWave = Math.Max(_currentProfile.BestWave, _currentRun.WaveReached);
        _currentProfile.TotalKills += _currentRun.TotalKills;
        _currentProfile.TotalDamageDealt += _currentRun.TotalDamageDealt;
        _currentProfile.TotalPlaytime += _currentRun.Duration;

        // Calculate new level
        _currentProfile.MetaLevel = MetaProgressionCalculator.GetLevelFromXP(_currentProfile.TotalMetaXp);

        // Add equipment found during run to inventory
        foreach (var equipment in _currentRun.EquipmentFound)
        {
            if (!_currentProfile.EquipmentInventory.Any(e => e.Id == equipment.Id))
            {
                _currentProfile.EquipmentInventory.Add(equipment);
            }
        }

        // Save profile and run history
        _profileService.SaveProfile(_currentProfile);
        _historyService.SaveRun(_currentRun);

        // Publish meta progression events
        _eventBus.Publish(new MetaXpGainedEvent(metaXp, _currentProfile.TotalMetaXp, _currentProfile.MetaLevel));
        
        if (_currentProfile.MetaLevel > oldLevel)
        {
            _eventBus.Publish(new MetaLevelUpEvent(_currentProfile.MetaLevel));
        }

        RuntimeLog.Info(
            LogCategory,
            $"Run finalized: wave={_currentRun.WaveReached}, kills={_currentRun.TotalKills}, gold={_currentRun.GoldCollected}, " +
            $"metaXp={metaXp}, totalMetaXp={_currentProfile.TotalMetaXp}, metaLevel={_currentProfile.MetaLevel}.");

        _currentRun = null;
    }

    /// <summary>
    /// Manually track an enemy kill (called from combat systems).
    /// </summary>
    public void TrackKill()
    {
        if (_currentRun != null)
        {
            _currentRun.TotalKills++;
        }
    }

    /// <summary>
    /// Manually track damage dealt (alternative to event-based tracking).
    /// </summary>
    public void TrackDamageDealt(float damage)
    {
        if (_currentRun != null)
        {
            _currentRun.TotalDamageDealt += damage;
        }
    }

    /// <summary>
    /// Manually track damage taken (alternative to event-based tracking).
    /// </summary>
    public void TrackDamageTaken(float damage)
    {
        if (_currentRun != null)
        {
            _currentRun.TotalDamageTaken += damage;
        }
    }
}
