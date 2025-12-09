using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.MetaProgression;

/// <summary>
/// Manages meta progression state during gameplay.
/// Coordinates profile loading/saving, run tracking, and XP calculation.
/// </summary>
public sealed class MetaProgressionManager
{
    private readonly PlayerProfileService _profileService;
    private readonly RunHistoryService _historyService;
    private readonly IEventBus _eventBus;
    private readonly SaveSlotService _saveSlotService;
    private readonly string _slotId;

    private PlayerProfile _currentProfile;
    private RunSession? _currentRun;

    public PlayerProfile CurrentProfile => _currentProfile;
    public RunSession? CurrentRun => _currentRun;
    public string SlotId => _slotId;

    public MetaProgressionManager(IEventBus eventBus, SaveSlotService saveSlotService, string slotId)
    {
        _saveSlotService = saveSlotService;
        _slotId = slotId;
        var fileSystem = new DefaultFileSystem();
        var slotPath = _saveSlotService.GetSlotPath(slotId);
        _profileService = new PlayerProfileService(fileSystem, slotPath);
        _historyService = new RunHistoryService(fileSystem, slotPath);
        _eventBus = eventBus;

        // Load profile
        _currentProfile = _profileService.LoadProfile();

        // Subscribe to events
        _eventBus.Subscribe<RunStartedEvent>(OnRunStarted);
        _eventBus.Subscribe<RunEndedEvent>(OnRunEnded);
        _eventBus.Subscribe<GoldCollectedEvent>(OnGoldCollected);
        _eventBus.Subscribe<EquipmentCollectedEvent>(OnEquipmentCollected);
        _eventBus.Subscribe<SessionRestartedEvent>(OnSessionRestarted);
        _eventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        _eventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        _eventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
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

    private void OnRunStarted(RunStartedEvent evt)
    {
        _currentRun = new RunSession
        {
            StartTime = DateTime.UtcNow,
            RunId = Guid.NewGuid().ToString()
        };

        Console.WriteLine($"[MetaProgression] Run started: {_currentRun.RunId}");
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
            RunId = Guid.NewGuid().ToString()
        };

        Console.WriteLine($"[MetaProgression] Run restarted: {_currentRun.RunId}");
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

        Console.WriteLine($"[MetaProgression] Run finalized:");
        Console.WriteLine($"  Wave: {_currentRun.WaveReached}");
        Console.WriteLine($"  Kills: {_currentRun.TotalKills}");
        Console.WriteLine($"  Gold: {_currentRun.GoldCollected}");
        Console.WriteLine($"  Meta XP: {metaXp}");
        Console.WriteLine($"  Total Meta XP: {_currentProfile.TotalMetaXp}");
        Console.WriteLine($"  Meta Level: {_currentProfile.MetaLevel}");

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
