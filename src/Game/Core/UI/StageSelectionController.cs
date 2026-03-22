using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLastMageStanding.Game.Core.UI;

internal readonly record struct StageSelectionActViewModel(
    int ActNumber,
    string Title);

internal readonly record struct StageSelectionListItemViewModel(
    string StageId,
    string Label,
    bool IsUnlocked,
    bool IsCompleted,
    bool IsSelected);

internal readonly record struct StageSelectionDetailsViewModel(
    string Title,
    string Description,
    string StatusText,
    string StartButtonText,
    bool CanStart);

internal readonly record struct StageSelectionScreenState(
    bool IsOpen,
    string MetaText,
    string HelpText,
    int SelectedActIndex,
    int SelectedStageIndex,
    IReadOnlyList<StageSelectionActViewModel> Acts,
    IReadOnlyList<StageSelectionListItemViewModel> Stages,
    StageSelectionDetailsViewModel Details);

internal readonly record struct StageSelectionStageOption(
    string StageId,
    string DisplayName,
    string Description,
    bool IsUnlocked,
    bool IsCompleted,
    string LockedReason);

internal readonly record struct StageSelectionActOption(
    int ActNumber,
    string Title,
    IReadOnlyList<StageSelectionStageOption> Stages);

internal readonly record struct StageSelectionCatalog(
    string MetaText,
    IReadOnlyList<StageSelectionActOption> Acts)
{
    public static StageSelectionCatalog Empty { get; } = new(string.Empty, Array.Empty<StageSelectionActOption>());
}

internal readonly record struct StageSelectionControllerInput(
    bool MoveLeft,
    bool MoveRight,
    bool MoveUp,
    bool MoveDown,
    bool Confirm,
    bool Back);

internal readonly record struct StageSelectionControllerResult(
    bool CloseRequested,
    string? StartStageId,
    StageSelectionScreenState ViewState);

internal sealed class StageSelectionController
{
    private const string HelpText = "[WASD] Navigate  [ENTER] Select  [ESC] Back";

    private bool _isOpen;
    private bool _queuedClose;
    private bool _queuedStart;
    private int _selectedActIndex;
    private int _selectedStageIndex;

    public void Open(int actIndex, int stageIndex, StageSelectionCatalog catalog)
    {
        _isOpen = true;
        _queuedClose = false;
        _queuedStart = false;
        _selectedActIndex = actIndex;
        _selectedStageIndex = stageIndex;
        ClampToCatalog(catalog);
    }

    public void Close()
    {
        _isOpen = false;
        _queuedClose = false;
        _queuedStart = false;
        _selectedActIndex = 0;
        _selectedStageIndex = 0;
    }

    public StageSelectionControllerResult Update(StageSelectionCatalog catalog, in StageSelectionControllerInput input)
    {
        ClampToCatalog(catalog);

        string? startStageId = null;
        var closeRequested = false;

        if (_isOpen)
        {
            if (input.MoveLeft)
            {
                ChangeAct(-1, catalog);
            }
            else if (input.MoveRight)
            {
                ChangeAct(1, catalog);
            }

            if (input.MoveDown)
            {
                MoveSelection(1, catalog);
            }
            else if (input.MoveUp)
            {
                MoveSelection(-1, catalog);
            }

            if (input.Confirm || _queuedStart)
            {
                startStageId = TryGetStartStageId(catalog);
            }

            if (input.Back || _queuedClose)
            {
                closeRequested = true;
            }
        }

        _queuedClose = false;
        _queuedStart = false;

        return new StageSelectionControllerResult(closeRequested, startStageId, BuildState(catalog));
    }

    public void ChangeAct(int delta, StageSelectionCatalog catalog)
    {
        if (catalog.Acts.Count == 0)
        {
            _selectedActIndex = 0;
            _selectedStageIndex = 0;
            return;
        }

        var next = Math.Clamp(_selectedActIndex + delta, 0, catalog.Acts.Count - 1);
        if (next == _selectedActIndex)
        {
            return;
        }

        _selectedActIndex = next;
        _selectedStageIndex = 0;
        ClampToCatalog(catalog);
    }

    public void SelectStage(int stageIndex, StageSelectionCatalog catalog)
    {
        ClampToCatalog(catalog);
        var stages = GetSelectedAct(catalog)?.Stages ?? Array.Empty<StageSelectionStageOption>();
        if (stages.Count == 0)
        {
            _selectedStageIndex = 0;
            return;
        }

        _selectedStageIndex = Math.Clamp(stageIndex, 0, stages.Count - 1);
    }

    public void RequestStart() => _queuedStart = true;

    public void RequestBack() => _queuedClose = true;

    public int SelectedActIndex => _selectedActIndex;

    public int SelectedStageIndex => _selectedStageIndex;

    private void MoveSelection(int delta, StageSelectionCatalog catalog)
    {
        var stages = GetSelectedAct(catalog)?.Stages ?? Array.Empty<StageSelectionStageOption>();
        if (stages.Count == 0)
        {
            _selectedStageIndex = 0;
            return;
        }

        _selectedStageIndex = (_selectedStageIndex + delta + stages.Count) % stages.Count;
    }

    private string? TryGetStartStageId(StageSelectionCatalog catalog)
    {
        var stage = GetSelectedStage(catalog);
        if (stage is null || !stage.Value.IsUnlocked)
        {
            return null;
        }

        return stage.Value.StageId;
    }

    private void ClampToCatalog(StageSelectionCatalog catalog)
    {
        if (catalog.Acts.Count == 0)
        {
            _selectedActIndex = 0;
            _selectedStageIndex = 0;
            return;
        }

        _selectedActIndex = Math.Clamp(_selectedActIndex, 0, catalog.Acts.Count - 1);
        var stages = catalog.Acts[_selectedActIndex].Stages;
        _selectedStageIndex = stages.Count == 0
            ? 0
            : Math.Clamp(_selectedStageIndex, 0, stages.Count - 1);
    }

    private StageSelectionScreenState BuildState(StageSelectionCatalog catalog)
    {
        var selectedAct = GetSelectedAct(catalog);
        var stages = selectedAct?.Stages ?? Array.Empty<StageSelectionStageOption>();
        var selectedStage = GetSelectedStage(catalog);

        var listItems = stages
            .Select((stage, index) => new StageSelectionListItemViewModel(
                StageId: stage.StageId,
                Label: $"{(stage.IsCompleted ? "✓ " : (!stage.IsUnlocked ? "[L] " : "  "))}{stage.DisplayName}",
                IsUnlocked: stage.IsUnlocked,
                IsCompleted: stage.IsCompleted,
                IsSelected: index == _selectedStageIndex))
            .ToArray();

        var acts = catalog.Acts
            .Select(act => new StageSelectionActViewModel(act.ActNumber, act.Title))
            .ToArray();

        return new StageSelectionScreenState(
            IsOpen: _isOpen,
            MetaText: catalog.MetaText,
            HelpText: HelpText,
            SelectedActIndex: _selectedActIndex,
            SelectedStageIndex: _selectedStageIndex,
            Acts: acts,
            Stages: listItems,
            Details: BuildDetails(selectedStage));
    }

    private static StageSelectionDetailsViewModel BuildDetails(StageSelectionStageOption? stage)
    {
        if (stage is null)
        {
            return new StageSelectionDetailsViewModel(
                Title: string.Empty,
                Description: "Select a stage.",
                StatusText: string.Empty,
                StartButtonText: "LOCKED",
                CanStart: false);
        }

        if (stage.Value.IsCompleted)
        {
            return new StageSelectionDetailsViewModel(
                Title: stage.Value.DisplayName,
                Description: stage.Value.Description,
                StatusText: "COMPLETED",
                StartButtonText: "REPLAY STAGE",
                CanStart: true);
        }

        if (stage.Value.IsUnlocked)
        {
            return new StageSelectionDetailsViewModel(
                Title: stage.Value.DisplayName,
                Description: stage.Value.Description,
                StatusText: "AVAILABLE",
                StartButtonText: "ENTER STAGE",
                CanStart: true);
        }

        return new StageSelectionDetailsViewModel(
            Title: stage.Value.DisplayName,
            Description: stage.Value.Description,
            StatusText: $"LOCKED - {stage.Value.LockedReason}",
            StartButtonText: "LOCKED",
            CanStart: false);
    }

    private StageSelectionActOption? GetSelectedAct(StageSelectionCatalog catalog)
    {
        if (catalog.Acts.Count == 0 || _selectedActIndex < 0 || _selectedActIndex >= catalog.Acts.Count)
        {
            return null;
        }

        return catalog.Acts[_selectedActIndex];
    }

    private StageSelectionStageOption? GetSelectedStage(StageSelectionCatalog catalog)
    {
        var stages = GetSelectedAct(catalog)?.Stages;
        if (stages is null || stages.Count == 0 || _selectedStageIndex < 0 || _selectedStageIndex >= stages.Count)
        {
            return null;
        }

        return stages[_selectedStageIndex];
    }
}
