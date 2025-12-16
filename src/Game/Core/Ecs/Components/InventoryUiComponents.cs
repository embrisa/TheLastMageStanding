namespace TheLastMageStanding.Game.Core.Ecs.Components;

/// <summary>
/// Component to track inventory UI state.
/// </summary>
internal struct InventoryUiState
{
    public bool IsOpen { get; set; }
    public int SelectedIndex { get; set; }
    public InventoryUiMode Mode { get; set; }

    public InventoryUiState()
    {
        IsOpen = false;
        SelectedIndex = 0;
        Mode = InventoryUiMode.Inventory;
    }
}

internal enum InventoryUiMode
{
    Inventory,
    Equipment
}

