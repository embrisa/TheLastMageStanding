namespace TheLastMageStanding.Game.Core.UI;

internal enum MainMenuAction
{
    None,
    StartSlot,
    CreateNewSlot,
    Settings,
    Quit
}

internal sealed record MainMenuResult(MainMenuAction Action, string? SlotId = null);
