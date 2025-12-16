using System;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using TheLastMageStanding.Game.Core.Skills;

namespace TheLastMageStanding.Game.Core.UI.Myra;

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
    SkillDefinition? DetailSkill);

internal sealed class MyraSkillSelectionScreen : MyraMenuScreenBase
{
    private static readonly SkillId[,] SkillGrid =
    {
        { SkillId.Firebolt, SkillId.ArcaneMissile, SkillId.FrostBolt },
        { SkillId.Fireball, SkillId.ArcaneBurst, SkillId.FrostNova },
        { SkillId.FlameWave, SkillId.ArcaneBarrage, SkillId.Blizzard }
    };

    private readonly Button[,] _skillButtons = new Button[3, 3];
    private readonly Label[,] _skillBadgeLabels = new Label[3, 3];
    private readonly Button[] _slotButtons = new Button[5];
    private readonly Label[] _slotLabels = new Label[5];

    private readonly Label _dirtyLabel;
    private readonly Label _detailTitle;
    private readonly Label _detailMeta;
    private readonly Label _detailDescription;
    private readonly Label _detailStats;
    private readonly Button _confirmButton;
    private readonly Button _cancelButton;
    private readonly Button _clearSlotButton;
    private readonly KeyboardHintBar _hintBar;
    private IUiSoundPlayer? _uiSoundPlayer;

    public MyraSkillSelectionScreen(IUiSoundPlayer? uiSoundPlayer = null)
        : base(useRenderTarget: true)
    {
        _uiSoundPlayer = uiSoundPlayer;

        var root = UiStyles.ScreenOverlay();
        root.Visible = false;
        root.Width = VirtualWidth;
        root.Height = VirtualHeight;

        var modal = new ModalDialog();
        modal.ContentHost.Width = 880;
        modal.ContentHost.Height = 500;

        var layout = new Grid
        {
            RowSpacing = UiTheme.Spacing,
            ColumnSpacing = UiTheme.Spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Header
        layout.RowsProportions.Add(new Proportion(ProportionType.Fill)); // Content
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Hotbar
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Actions
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Hints

        var header = new Grid { ColumnSpacing = UiTheme.Spacing };
        header.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        header.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var title = UiStyles.Heading("Skill Selection", 1.5f, UiTheme.PrimaryText);
        title.HorizontalAlignment = HorizontalAlignment.Left;
        Grid.SetColumn(title, 0);
        header.Widgets.Add(title);

        _dirtyLabel = UiStyles.BodyText(string.Empty, UiTheme.MutedText, wrap: false, scale: 0.9f);
        _dirtyLabel.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(_dirtyLabel, 1);
        header.Widgets.Add(_dirtyLabel);

        Grid.SetRow(header, 0);
        layout.Widgets.Add(header);

        var content = new Grid { ColumnSpacing = UiTheme.LargeSpacing };
        content.ColumnsProportions.Add(new Proportion(ProportionType.Fill)); // Grid
        content.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Details

        var gridPanel = UiStyles.Card(UiTheme.Padding);
        gridPanel.Width = 560;
        gridPanel.Height = 300;
        gridPanel.HorizontalAlignment = HorizontalAlignment.Left;
        gridPanel.VerticalAlignment = VerticalAlignment.Top;
        gridPanel.Widgets.Add(BuildSkillGrid());
        Grid.SetColumn(gridPanel, 0);
        content.Widgets.Add(gridPanel);

        var detailsPanel = UiStyles.Card(UiTheme.Padding);
        detailsPanel.Width = 280;
        detailsPanel.Height = 300;
        detailsPanel.HorizontalAlignment = HorizontalAlignment.Right;
        detailsPanel.VerticalAlignment = VerticalAlignment.Top;

        var detailColumn = new MenuColumn(UiTheme.Spacing);
        _detailTitle = UiStyles.SectionTitle("Select a skill");
        _detailMeta = UiStyles.SectionDescription(string.Empty, 0.95f);
        _detailDescription = UiStyles.BodyText(string.Empty, UiTheme.PrimaryText, wrap: true, scale: 0.95f);
        _detailStats = UiStyles.BodyText(string.Empty, UiTheme.MutedText, wrap: true, scale: 0.9f);
        detailColumn.AddRow(_detailTitle);
        detailColumn.AddRow(_detailMeta);
        detailColumn.AddRow(_detailDescription, ProportionType.Fill);
        detailColumn.AddRow(_detailStats);
        detailsPanel.Widgets.Add(detailColumn);

        Grid.SetColumn(detailsPanel, 1);
        content.Widgets.Add(detailsPanel);

        Grid.SetRow(content, 1);
        layout.Widgets.Add(content);

        var hotbarPanel = UiStyles.Card(UiTheme.Padding);
        hotbarPanel.Width = 880;
        hotbarPanel.Height = 90;
        hotbarPanel.HorizontalAlignment = HorizontalAlignment.Stretch;

        var hotbarColumn = new MenuColumn(UiTheme.Spacing);
        hotbarColumn.AddRow(UiStyles.SectionTitle("Hotbar", 1.05f));

        var slots = new HorizontalStackPanel
        {
            Spacing = UiTheme.Spacing,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        for (var slotIndex = 0; slotIndex < _slotButtons.Length; slotIndex++)
        {
            var button = BuildSlotButton(slotIndex);
            _slotButtons[slotIndex] = button;
            slots.Widgets.Add(button);
        }

        hotbarColumn.AddRow(slots);
        hotbarPanel.Widgets.Add(hotbarColumn);

        Grid.SetRow(hotbarPanel, 2);
        layout.Widgets.Add(hotbarPanel);

        var actions = new Grid { ColumnSpacing = UiTheme.Spacing };
        actions.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        actions.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        actions.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        actions.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        _clearSlotButton = new MenuButton("Clear Slot", width: 160, height: 44);
        UiSoundBinder.BindHoverAndClick(_clearSlotButton, _uiSoundPlayer);
        _clearSlotButton.Click += (_, _) => ClearSlotRequested?.Invoke(_lastState.FocusedSlot);
        Grid.SetColumn(_clearSlotButton, 0);
        actions.Widgets.Add(_clearSlotButton);

        _cancelButton = new MenuButton("Cancel", width: 160, height: 44);
        UiSoundBinder.BindHoverAndClick(_cancelButton, _uiSoundPlayer);
        _cancelButton.Click += (_, _) => CancelRequested?.Invoke();
        Grid.SetColumn(_cancelButton, 2);
        actions.Widgets.Add(_cancelButton);

        _confirmButton = new MenuButton("Confirm", accent: true, width: 180, height: 44);
        UiSoundBinder.BindHoverAndClick(_confirmButton, _uiSoundPlayer);
        _confirmButton.Click += (_, _) => ConfirmRequested?.Invoke();
        Grid.SetColumn(_confirmButton, 3);
        actions.Widgets.Add(_confirmButton);

        Grid.SetRow(actions, 3);
        layout.Widgets.Add(actions);

        _hintBar = new KeyboardHintBar();
        _hintBar.SetHints(
            ("Arrows", "Navigate"),
            ("Enter", "Select/Equip"),
            ("Tab", "Switch Focus"),
            ("Esc", "Cancel"));
        Grid.SetRow(_hintBar, 4);
        layout.Widgets.Add(_hintBar);

        modal.ContentHost.Widgets.Add(layout);
        root.Widgets.Add(modal);
        Desktop.Root = root;
    }

    public event Action<SkillId>? SkillClicked;
    public event Action<int>? SlotClicked;
    public event Action<int>? ClearSlotRequested;
    public event Action? ConfirmRequested;
    public event Action? CancelRequested;

    public bool IsVisible => Desktop.Root.Visible;

    public void SetSoundPlayer(IUiSoundPlayer player)
    {
        _uiSoundPlayer = player;
    }

    private SkillSelectionScreenState _lastState;

    public void ApplyState(SkillSelectionScreenState state)
    {
        _lastState = state;
        Desktop.Root.Visible = state.IsOpen;
        if (!state.IsOpen)
        {
            return;
        }

        _dirtyLabel.Text = state.HasChanges ? "Unsaved changes" : string.Empty;
        _confirmButton.Enabled = state.HasChanges;

        _clearSlotButton.Enabled = state.FocusedSlot is >= 1 and <= 4 && state.Loadout.GetSlot(state.FocusedSlot) != SkillId.None;

        UpdateSkillButtons(state);
        UpdateHotbarButtons(state);
        UpdateDetails(state);
    }

    private Grid BuildSkillGrid()
    {
        var container = new Grid
        {
            RowSpacing = UiTheme.Spacing,
            ColumnSpacing = UiTheme.Spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        container.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        container.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        container.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));

        container.RowsProportions.Add(new Proportion(ProportionType.Auto));
        container.RowsProportions.Add(new Proportion(ProportionType.Auto));
        container.RowsProportions.Add(new Proportion(ProportionType.Auto));
        container.RowsProportions.Add(new Proportion(ProportionType.Auto));

        AddElementHeader(container, 0, "Fire", new Color(255, 120, 80));
        AddElementHeader(container, 1, "Arcane", new Color(180, 140, 255));
        AddElementHeader(container, 2, "Frost", new Color(120, 220, 255));

        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                var skillId = SkillGrid[row, col];
                var button = BuildSkillButton(skillId);
                _skillButtons[row, col] = button;
                Grid.SetRow(button, row + 1);
                Grid.SetColumn(button, col);
                container.Widgets.Add(button);
            }
        }

        return container;
    }

    private void AddElementHeader(Grid grid, int column, string text, Color color)
    {
        var label = UiStyles.SectionTitle(text, 1.05f);
        label.TextColor = color;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        Grid.SetRow(label, 0);
        Grid.SetColumn(label, column);
        grid.Widgets.Add(label);
    }

    private Button BuildSkillButton(SkillId skillId)
    {
        var label = UiStyles.BodyText(FormatSkillLabel(skillId), UiTheme.PrimaryText, wrap: true, scale: 0.95f);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;

        var badge = UiStyles.BodyText(string.Empty, UiTheme.SuccessText, wrap: false, scale: 0.85f);
        badge.HorizontalAlignment = HorizontalAlignment.Right;
        badge.VerticalAlignment = VerticalAlignment.Bottom;

        var content = new Grid();
        content.RowsProportions.Add(new Proportion(ProportionType.Fill));
        content.RowsProportions.Add(new Proportion(ProportionType.Auto));
        Grid.SetRow(label, 0);
        Grid.SetRow(badge, 1);
        content.Widgets.Add(label);
        content.Widgets.Add(badge);

        var button = new Button
        {
            Width = 170,
            Height = 72,
            Content = content
        };

        UiStyles.StyleMenuButton(button);
        UiSoundBinder.BindHoverAndClick(button, _uiSoundPlayer);
        button.Click += (_, _) => SkillClicked?.Invoke(skillId);

        CacheBadgeLabel(skillId, badge);
        return button;
    }

    private void CacheBadgeLabel(SkillId skillId, Label badge)
    {
        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                if (SkillGrid[row, col] == skillId)
                {
                    _skillBadgeLabels[row, col] = badge;
                    return;
                }
            }
        }
    }

    private Button BuildSlotButton(int slotIndex)
    {
        var slotName = slotIndex switch
        {
            0 => "Primary",
            _ => $"Slot {slotIndex}"
        };

        var header = UiStyles.BodyText(slotName, UiTheme.MutedText, wrap: false, scale: 0.85f);
        header.HorizontalAlignment = HorizontalAlignment.Center;

        var skillLabel = UiStyles.BodyText("Empty", UiTheme.PrimaryText, wrap: true, scale: 0.95f);
        skillLabel.HorizontalAlignment = HorizontalAlignment.Center;

        var content = new MenuColumn(2);
        content.AddRow(header);
        content.AddRow(skillLabel);

        var button = new Button
        {
            Width = 160,
            Height = 54,
            Content = content
        };

        UiStyles.StyleMenuButton(button);
        UiSoundBinder.BindHoverAndClick(button, _uiSoundPlayer);
        button.Click += (_, _) => SlotClicked?.Invoke(slotIndex);

        _slotLabels[slotIndex] = skillLabel;
        return button;
    }

    private void UpdateSkillButtons(SkillSelectionScreenState state)
    {
        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                var skillId = SkillGrid[row, col];
                var button = _skillButtons[row, col];
                var badge = _skillBadgeLabels[row, col];

                badge.Text = GetEquippedBadge(skillId, state.Loadout);

                var isCursor = state.FocusArea == SkillSelectionFocusArea.SkillGrid &&
                               row == state.CursorRow &&
                               col == state.CursorColumn;
                var isSelected = state.SelectedSkill.HasValue && state.SelectedSkill.Value == skillId;

                if (isSelected)
                {
                    button.Border = new SolidBrush(UiTheme.AccentText);
                    button.BorderThickness = new Thickness(2);
                }
                else if (isCursor)
                {
                    button.Border = new SolidBrush(UiTheme.TabActiveBorder);
                    button.BorderThickness = new Thickness(2);
                }
                else
                {
                    button.Border = new SolidBrush(UiTheme.ButtonBorder);
                    button.BorderThickness = new Thickness(1);
                }
            }
        }
    }

    private void UpdateHotbarButtons(SkillSelectionScreenState state)
    {
        for (var slot = 0; slot < 5; slot++)
        {
            var skillId = state.Loadout.GetSlot(slot);
            _slotLabels[slot].Text = skillId == SkillId.None ? "Empty" : FormatSkillLabel(skillId);

            var focused = state.FocusArea == SkillSelectionFocusArea.Hotbar && state.FocusedSlot == slot;
            if (focused)
            {
                _slotButtons[slot].Border = new SolidBrush(UiTheme.TabActiveBorder);
                _slotButtons[slot].BorderThickness = new Thickness(2);
            }
            else
            {
                _slotButtons[slot].Border = new SolidBrush(UiTheme.ButtonBorder);
                _slotButtons[slot].BorderThickness = new Thickness(1);
            }
        }
    }

    private void UpdateDetails(SkillSelectionScreenState state)
    {
        var definition = state.DetailSkill;
        if (definition == null)
        {
            _detailTitle.Text = "Select a skill";
            _detailMeta.Text = string.Empty;
            _detailDescription.Text = "Pick a skill from the grid to view details, then assign it to a hotbar slot.";
            _detailStats.Text = string.Empty;
            return;
        }

        _detailTitle.Text = definition.Name;
        _detailMeta.Text = $"{definition.Element} • {definition.DeliveryType} • {definition.TargetType}";
        _detailDescription.Text = definition.Description;

        var castTimeText = definition.CastTime <= 0 ? "Instant" : $"{definition.CastTime:0.0}s";
        var stats =
            $"Damage: {definition.BaseDamageMultiplier:0.0}× power\n" +
            $"Cooldown: {definition.BaseCooldown:0.0}s\n" +
            $"Cast Time: {castTimeText}\n" +
            $"Range: {definition.Range:0}\n";

        if (definition.ProjectileSpeed > 0)
        {
            stats += $"Projectile Speed: {definition.ProjectileSpeed:0}\n";
        }

        if (definition.AoeRadius > 0)
        {
            stats += $"AoE Radius: {definition.AoeRadius:0}\n";
        }

        var equippedBadge = GetEquippedBadge(definition.Id, state.Loadout);
        if (!string.IsNullOrWhiteSpace(equippedBadge))
        {
            stats += $"\nEquipped: {equippedBadge}";
        }

        _detailStats.Text = stats.TrimEnd();
    }

    private static string GetEquippedBadge(SkillId skillId, SkillLoadout loadout)
    {
        if (loadout.Primary == skillId)
        {
            return "Primary";
        }

        if (loadout.Hotkey1 == skillId) return "Slot 1";
        if (loadout.Hotkey2 == skillId) return "Slot 2";
        if (loadout.Hotkey3 == skillId) return "Slot 3";
        if (loadout.Hotkey4 == skillId) return "Slot 4";

        return string.Empty;
    }

    private static string FormatSkillLabel(SkillId skillId)
    {
        var raw = skillId.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return raw;
        }

        // Insert spaces in PascalCase for readability (ArcaneMissile -> Arcane Missile).
        Span<char> buffer = stackalloc char[raw.Length * 2];
        var index = 0;
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(raw[i - 1]))
            {
                buffer[index++] = ' ';
            }

            buffer[index++] = c;
        }

        return new string(buffer[..index]);
    }
}
