namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Component tracking perk tree UI state.
/// </summary>
internal struct PerkTreeUI
{
    public bool IsOpen { get; set; }
    public int SelectedPerkIndex { get; set; }
    public string? HoveredPerkId { get; set; }
    public string? MessageText { get; set; }
    public float MessageTimer { get; set; }

    public PerkTreeUI()
    {
        IsOpen = false;
        SelectedPerkIndex = 0;
        HoveredPerkId = null;
        MessageText = null;
        MessageTimer = 0f;
    }
}
