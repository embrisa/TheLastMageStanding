using TheLastMageStanding.Game.Core.MetaProgression;

namespace TheLastMageStanding.Game.Core.SceneState;

/// <summary>
/// Owns active save-slot selection for the runtime.
/// World instances are slot-scoped and must be replaced whenever the active slot changes.
/// </summary>
internal sealed class ActiveSaveSlotController
{
    private readonly SaveSlotService _saveSlotService;
    private string? _activeSlotId;

    public ActiveSaveSlotController(SaveSlotService saveSlotService)
    {
        _saveSlotService = saveSlotService;
    }

    public string CurrentSlotId => _activeSlotId ?? EnsureActiveSlot();

    public bool ActivateSlot(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
        {
            throw new ArgumentException("Slot id is required.", nameof(slotId));
        }

        var changed = !string.Equals(_activeSlotId, slotId, StringComparison.Ordinal);
        _activeSlotId = slotId;
        return changed;
    }

    public string CreateNextSlot()
    {
        var newSlot = _saveSlotService.CreateNextSlot();
        _activeSlotId = newSlot.SlotId;
        return newSlot.SlotId;
    }

    public string EnsureActiveSlot()
    {
        if (!string.IsNullOrEmpty(_activeSlotId))
        {
            return _activeSlotId;
        }

        var existing = _saveSlotService.GetMostRecentSlot();
        _activeSlotId = existing?.SlotId ?? _saveSlotService.CreateNextSlot().SlotId;
        return _activeSlotId;
    }
}
