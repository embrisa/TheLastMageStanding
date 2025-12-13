using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D.UI;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.UI;

/// <summary>
/// Myra-based main menu with standardized styling and reusable menu components.
/// </summary>
internal sealed class MyraMainMenuScreen : MyraMenuScreenBase
{
    private enum MenuMode
    {
        Main,
        LoadSlots
    }

    private readonly SaveSlotService _saveSlotService;
    private IUiSoundPlayer? _uiSoundPlayer;
    private MenuMode _mode = MenuMode.Main;
    private List<SaveSlotInfo> _slots = new();
    private double _slotRefreshTimer;
    private string _statusMessage = string.Empty;
    private MainMenuResult _pendingResult = new(MainMenuAction.None);
    private readonly KeyboardHintBar _hintBar = new();
    private readonly List<(MenuButton Button, Action Activate)> _buttons = new();
    private int _selectedIndex;
    private string _slotsSignature = string.Empty;

    public MyraMainMenuScreen(SaveSlotService saveSlotService)
        : base(UiTheme.VirtualWidth, UiTheme.VirtualHeight)
    {
        _saveSlotService = saveSlotService;
        RefreshSlots();
    }

    public void SetSoundPlayer(IUiSoundPlayer player)
    {
        _uiSoundPlayer = player;
    }

    public void Initialize(Microsoft.Xna.Framework.Game game)
    {
        MyraEnvironment.Game ??= game;
        BuildMainMenu();
    }

    public override void Dispose()
    {
        base.Dispose();
    }

    public MainMenuResult Update(GameTime gameTime, InputState input)
    {
        base.Update(gameTime);

        _slotRefreshTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (_slotRefreshTimer >= 2.0)
        {
            var slotsChanged = RefreshSlots();
            _slotRefreshTimer = 0;

            if (slotsChanged)
            {
                if (_mode == MenuMode.Main)
                {
                    BuildMainMenu();
                }
                else
                {
                    BuildLoadMenu();
                }
            }
        }

        if (_mode == MenuMode.LoadSlots && input.MenuBackPressed)
        {
            UiSoundBinder.PlayKeyboardCancel(_uiSoundPlayer);
            _mode = MenuMode.Main;
            _statusMessage = string.Empty;
            BuildMainMenu();
        }

        HandleKeyboard(input);

        var result = _pendingResult;
        _pendingResult = new MainMenuResult(MainMenuAction.None);
        return result;
    }

    public void Draw()
    {
        Render();
    }

    private void BuildMainMenu()
    {
        _mode = MenuMode.Main;

        var root = UiStyles.ScreenOverlay();
        root.Width = VirtualWidth;
        root.Height = VirtualHeight;

        var layout = new MenuColumn(UiTheme.LargeSpacing)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        layout.AddRow(UiStyles.Heading("The Last Mage Standing", 1.8f));

        var card = new MenuCard(UiTheme.LargePadding);
        var buttons = new MenuColumn(UiTheme.Spacing);
        var buttonLabels = BuildMainMenuLabels();
        _buttons.Clear();
        var previousSelected = _selectedIndex;

        for (int i = 0; i < buttonLabels.Count; i++)
        {
            var button = new MenuButton(buttonLabels[i].Label, accent: i == 0);
            var id = buttonLabels[i].Id;
            button.Click += (_, _) => OnMainMenuButtonClick(id);
            UiSoundBinder.BindHoverAndClick(button, _uiSoundPlayer);
            buttons.AddRow(button);
            _buttons.Add((button, () => OnMainMenuButtonClick(id)));
        }

        _selectedIndex = _buttons.Count == 0 ? 0 : Math.Clamp(previousSelected, 0, _buttons.Count - 1);

        if (!string.IsNullOrWhiteSpace(_statusMessage))
        {
            buttons.AddRow(UiStyles.BodyText(_statusMessage, UiTheme.MutedText));
        }

        card.Widgets.Add(buttons);
        layout.AddRow(card);

        _hintBar.SetHints(("↑/↓", "Navigate"), ("Enter", "Select"), ("Esc", "Quit/Back"));
        layout.AddRow(_hintBar);

        root.Widgets.Add(layout);
        Desktop.Root = root;

        HighlightSelection();
    }

    private void BuildLoadMenu()
    {
        _mode = MenuMode.LoadSlots;

        var root = UiStyles.ScreenOverlay();
        root.Width = VirtualWidth;
        root.Height = VirtualHeight;

        var layout = new MenuColumn(UiTheme.LargeSpacing)
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        layout.AddRow(UiStyles.Heading("Select Save Slot", 1.6f, UiTheme.PrimaryText));

        var card = new MenuCard(UiTheme.LargePadding);
        var listColumn = new MenuColumn(UiTheme.Spacing)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top
        };
        _buttons.Clear();
        _selectedIndex = 0;

        if (_slots.Count == 0)
        {
            listColumn.AddRow(UiStyles.BodyText("No saves found.", UiTheme.MutedText));
            _statusMessage = "No saves found. Create a new game to start.";
        }
        else
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                var slotLabel = BuildSlotLabel(slot);
                var button = new MenuButton(slotLabel, width: 520, height: 56);
                var slotId = slot.SlotId;
                button.Click += (_, _) => OnLoadSlotClick(slotId);
                UiSoundBinder.BindHoverAndClick(button, _uiSoundPlayer);
                listColumn.AddRow(button);
                _buttons.Add((button, () => OnLoadSlotClick(slotId)));
            }
        }

        if (!string.IsNullOrWhiteSpace(_statusMessage))
        {
            listColumn.AddRow(UiStyles.BodyText(_statusMessage, UiTheme.MutedText));
        }

        var listScrollViewer = new ScrollViewer
        {
            Content = listColumn,
            Height = Math.Max(200, VirtualHeight - 220),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            ClipToBounds = true
        };

        card.Widgets.Add(listScrollViewer);
        layout.AddRow(card);

        _hintBar.SetHints(("Esc", "Back"));
        layout.AddRow(_hintBar);

        root.Widgets.Add(layout);
        Desktop.Root = root;

        HighlightSelection();
    }

    private void OnMainMenuButtonClick(string id)
    {
        switch (id)
        {
            case "continue":
                var mostRecent = _saveSlotService.GetMostRecentSlot();
                if (mostRecent != null)
                {
                    _pendingResult = new MainMenuResult(MainMenuAction.StartSlot, mostRecent.SlotId);
                }
                else
                {
                    _pendingResult = new MainMenuResult(MainMenuAction.CreateNewSlot);
                }
                break;
            case "new":
                _pendingResult = new MainMenuResult(MainMenuAction.CreateNewSlot);
                break;
            case "load":
                _statusMessage = string.Empty;
                BuildLoadMenu();
                break;
            case "settings":
                _pendingResult = new MainMenuResult(MainMenuAction.Settings);
                break;
            case "quit":
                _pendingResult = new MainMenuResult(MainMenuAction.Quit);
                break;
        }
    }

    private void OnLoadSlotClick(string slotId)
    {
        _pendingResult = new MainMenuResult(MainMenuAction.StartSlot, slotId);
    }

    private List<(string Id, string Label)> BuildMainMenuLabels()
    {
        return new List<(string Id, string Label)>
        {
            ("continue", _slots.Any(s => s.HasProfileData) ? "Continue" : "Start"),
            ("new", "New Game"),
            ("load", "Load Game"),
            ("settings", "Settings"),
            ("quit", "Quit")
        };
    }

    private bool RefreshSlots()
    {
        var newSlots = _saveSlotService.GetSlots().ToList();
        var newSignature = string.Join("|", newSlots.Select(s =>
        {
            var last = s.LastPlayedAt?.Ticks ?? 0;
            var created = s.CreatedAt?.Ticks ?? 0;
            return $"{s.SlotId}:{s.HasProfileData}:{last}:{created}";
        }));
        var changed = !string.Equals(newSignature, _slotsSignature, StringComparison.Ordinal);

        _slots = newSlots;
        _slotsSignature = newSignature;

        return changed;
    }

    private static string BuildSlotLabel(SaveSlotInfo slot)
    {
        var lastPlayed = slot.LastPlayedAt.HasValue
            ? slot.LastPlayedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : "Never played";
        var created = slot.CreatedAt.HasValue
            ? slot.CreatedAt.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : "Unknown";

        return $"{slot.SlotId.ToUpperInvariant()} — Last Played: {lastPlayed} | Created: {created}";
    }

    private void HandleKeyboard(InputState input)
    {
        if (_buttons.Count == 0)
        {
            return;
        }

        var changed = false;
        if (input.MenuDownPressed)
        {
            _selectedIndex = (_selectedIndex + 1) % _buttons.Count;
            changed = true;
        }
        else if (input.MenuUpPressed)
        {
            _selectedIndex = (_selectedIndex - 1 + _buttons.Count) % _buttons.Count;
            changed = true;
        }

        if (changed)
        {
            UiSoundBinder.PlayKeyboardHover(_buttons[_selectedIndex].Button, _uiSoundPlayer);
            HighlightSelection();
        }

        if (input.MenuConfirmPressed)
        {
            UiSoundBinder.PlayKeyboardActivate(_uiSoundPlayer);
            _buttons[_selectedIndex].Activate.Invoke();
        }
    }

    private void HighlightSelection()
    {
        for (int i = 0; i < _buttons.Count; i++)
        {
            UiStyles.HighlightSelection(_buttons[i].Button, i == _selectedIndex);
        }
    }

    private static Label? FindLabel(Widget? widget)
    {
        if (widget is Label label)
        {
            return label;
        }

        switch (widget)
        {
            case Button button:
                return FindLabel(button.Content);
            case Grid grid:
                foreach (var child in grid.Widgets)
                {
                    var found = FindLabel(child);
                    if (found != null)
                    {
                        return found;
                    }
                }
                break;
            case Panel panel:
                foreach (var child in panel.Widgets)
                {
                    var found = FindLabel(child);
                    if (found != null)
                    {
                        return found;
                    }
                }
                break;
            case ScrollViewer scrollViewer:
                return FindLabel(scrollViewer.Content);
            case HorizontalStackPanel hsp:
                foreach (var child in hsp.Widgets)
                {
                    var found = FindLabel(child);
                    if (found != null)
                    {
                        return found;
                    }
                }
                break;
            case VerticalStackPanel vsp:
                foreach (var child in vsp.Widgets)
                {
                    var found = FindLabel(child);
                    if (found != null)
                    {
                        return found;
                    }
                }
                break;
        }

        return null;
    }
}
