using System;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Core.UI;

internal enum SkillSelectionFocusArea
{
    SkillGrid = 0,
    Hotbar = 1
}

internal readonly record struct SkillSelectionScreenState(
    bool IsOpen,
    int CursorRow,
    int CursorColumn,
    SkillId? SelectedSkill,
    SkillSelectionFocusArea FocusArea,
    int FocusedSlot,
    SkillLoadout Loadout,
    bool HasChanges,
    SkillId DetailSkillId);

internal readonly record struct SkillSelectionControllerInput(
    bool ToggleFocusRequested,
    bool DeleteRequested,
    bool MenuBackPressed,
    bool MenuLeftPressed,
    bool MenuRightPressed,
    bool MenuUpPressed,
    bool MenuDownPressed,
    bool MenuConfirmPressed);

internal readonly record struct SkillSelectionControllerResult(
    bool ConfirmRequested,
    bool CancelRequested,
    SkillSelectionScreenState ViewState);

internal sealed class SkillSelectionController
{
    private static readonly SkillId[,] SkillGrid =
    {
        { SkillId.Firebolt, SkillId.ArcaneMissile, SkillId.FrostBolt },
        { SkillId.Fireball, SkillId.ArcaneBurst, SkillId.FrostNova },
        { SkillId.FlameWave, SkillId.ArcaneBarrage, SkillId.Blizzard }
    };

    private SkillLoadout _snapshot;
    private SkillLoadout _pending;
    private SkillId? _selectedSkill;
    private int _cursorRow;
    private int _cursorCol;
    private SkillSelectionFocusArea _focusArea = SkillSelectionFocusArea.SkillGrid;
    private int _focusedSlot = 1;
    private bool _isOpen;
    private bool _queuedConfirm;
    private bool _queuedCancel;

    public void Open(SkillLoadout loadout)
    {
        _isOpen = true;
        _snapshot = loadout;
        _pending = loadout;
        _selectedSkill = null;
        _cursorRow = 0;
        _cursorCol = 0;
        _focusArea = SkillSelectionFocusArea.SkillGrid;
        _focusedSlot = 1;
        _queuedConfirm = false;
        _queuedCancel = false;
    }

    public void Close()
    {
        _isOpen = false;
        _snapshot = default;
        _pending = default;
        _selectedSkill = null;
        _cursorRow = 0;
        _cursorCol = 0;
        _focusArea = SkillSelectionFocusArea.SkillGrid;
        _focusedSlot = 1;
        _queuedConfirm = false;
        _queuedCancel = false;
    }

    public SkillSelectionControllerResult Update(in SkillSelectionControllerInput input)
    {
        if (!_isOpen)
        {
            return new SkillSelectionControllerResult(false, false, default);
        }

        if (input.ToggleFocusRequested)
        {
            _focusArea = _focusArea == SkillSelectionFocusArea.SkillGrid
                ? SkillSelectionFocusArea.Hotbar
                : SkillSelectionFocusArea.SkillGrid;
        }

        if (input.MenuBackPressed)
        {
            _queuedCancel = true;
        }

        HandleKeyboardNavigation(input);

        var result = new SkillSelectionControllerResult(
            ConfirmRequested: _queuedConfirm,
            CancelRequested: _queuedCancel,
            ViewState: BuildState());

        _queuedConfirm = false;
        _queuedCancel = false;
        return result;
    }

    public void SelectSkill(SkillId skillId)
    {
        _selectedSkill = skillId;
        _focusArea = SkillSelectionFocusArea.SkillGrid;
        TrySetCursorToSkill(skillId);
    }

    public void SelectSlot(int slotIndex)
    {
        _focusArea = SkillSelectionFocusArea.Hotbar;
        _focusedSlot = Math.Clamp(slotIndex, 0, 4);

        if (_selectedSkill.HasValue)
        {
            EquipSelectedSkillToSlot(_focusedSlot);
        }
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex is < 1 or > 4)
        {
            return;
        }

        _pending = _pending.SetSlot(slotIndex, SkillId.None);
    }

    public void RequestConfirm() => _queuedConfirm = true;

    public void RequestCancel() => _queuedCancel = true;

    public SkillLoadout PendingLoadout => _pending;

    private SkillSelectionScreenState BuildState() =>
        new(
            IsOpen: _isOpen,
            CursorRow: _cursorRow,
            CursorColumn: _cursorCol,
            SelectedSkill: _selectedSkill,
            FocusArea: _focusArea,
            FocusedSlot: _focusedSlot,
            Loadout: _pending,
            HasChanges: _pending != _snapshot,
            DetailSkillId: _selectedSkill ?? SkillGrid[_cursorRow, _cursorCol]);

    private void HandleKeyboardNavigation(in SkillSelectionControllerInput input)
    {
        if (_focusArea == SkillSelectionFocusArea.SkillGrid)
        {
            if (input.MenuLeftPressed) _cursorCol = Math.Max(0, _cursorCol - 1);
            if (input.MenuRightPressed) _cursorCol = Math.Min(2, _cursorCol + 1);
            if (input.MenuUpPressed) _cursorRow = Math.Max(0, _cursorRow - 1);
            if (input.MenuDownPressed) _cursorRow = Math.Min(2, _cursorRow + 1);

            if (input.MenuConfirmPressed)
            {
                _selectedSkill = SkillGrid[_cursorRow, _cursorCol];
            }

            return;
        }

        if (input.MenuLeftPressed) _focusedSlot = Math.Max(0, _focusedSlot - 1);
        if (input.MenuRightPressed) _focusedSlot = Math.Min(4, _focusedSlot + 1);

        if (input.DeleteRequested && _focusedSlot is >= 1 and <= 4)
        {
            _pending = _pending.SetSlot(_focusedSlot, SkillId.None);
        }

        if (!input.MenuConfirmPressed)
        {
            return;
        }

        if (_selectedSkill.HasValue)
        {
            EquipSelectedSkillToSlot(_focusedSlot);
        }
        else
        {
            var slotSkill = _pending.GetSlot(_focusedSlot);
            if (slotSkill != SkillId.None)
            {
                _selectedSkill = slotSkill;
            }
        }
    }

    private void EquipSelectedSkillToSlot(int slotIndex)
    {
        if (!_selectedSkill.HasValue)
        {
            return;
        }

        var selected = _selectedSkill.Value;
        if (selected == SkillId.None)
        {
            return;
        }

        var existingSlot = FindSlotContaining(_pending, selected);
        if (existingSlot >= 0 && existingSlot != slotIndex)
        {
            var targetSkill = _pending.GetSlot(slotIndex);
            _pending = _pending.SetSlot(existingSlot, targetSkill);
            _pending = _pending.SetSlot(slotIndex, selected);
            return;
        }

        _pending = _pending.SetSlot(slotIndex, selected);
    }

    private static int FindSlotContaining(SkillLoadout loadout, SkillId skillId)
    {
        if (loadout.Primary == skillId) return 0;
        if (loadout.Hotkey1 == skillId) return 1;
        if (loadout.Hotkey2 == skillId) return 2;
        if (loadout.Hotkey3 == skillId) return 3;
        if (loadout.Hotkey4 == skillId) return 4;
        return -1;
    }

    private void TrySetCursorToSkill(SkillId skillId)
    {
        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                if (SkillGrid[row, col] == skillId)
                {
                    _cursorRow = row;
                    _cursorCol = col;
                    return;
                }
            }
        }
    }
}
