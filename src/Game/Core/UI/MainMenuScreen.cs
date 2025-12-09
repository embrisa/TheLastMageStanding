using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.MetaProgression;

namespace TheLastMageStanding.Game.Core.UI;

internal enum MainMenuAction
{
    None,
    StartSlot,
    CreateNewSlot,
    Quit
}

internal sealed record MainMenuResult(MainMenuAction Action, string? SlotId = null);

/// <summary>
/// Simple main menu with keyboard + mouse navigation and save slot selection.
/// </summary>
internal sealed class MainMenuScreen
{
    private enum MenuMode
    {
        Main,
        LoadSlots
    }

    private readonly SaveSlotService _saveSlotService;

    private SpriteFont _titleFont = null!;
    private SpriteFont _bodyFont = null!;
    private Texture2D _pixel = null!;
    private GraphicsDevice _graphicsDevice = null!;

    private MenuMode _mode = MenuMode.Main;
    private int _selectedMainIndex;
    private int _selectedLoadIndex;
    private List<SaveSlotInfo> _slots = new();
    private double _slotRefreshTimer;
    private string _statusMessage = string.Empty;

    private const int ButtonWidth = 420;
    private const int ButtonHeight = 52;
    private const int ButtonSpacing = 12;
    private const int StartY = 220;

    public MainMenuScreen(SaveSlotService saveSlotService)
    {
        _saveSlotService = saveSlotService;
        RefreshSlots();
    }

    public void LoadContent(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _titleFont = content.Load<SpriteFont>("Fonts/FontStylizedTitle");
        _bodyFont = content.Load<SpriteFont>("Fonts/FontRegularText");
        _pixel = CreatePixel(graphicsDevice);
    }

    public MainMenuResult Update(GameTime gameTime, InputState input)
    {
        _slotRefreshTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (_slotRefreshTimer >= 2.0)
        {
            RefreshSlots();
            _slotRefreshTimer = 0;
        }

        return _mode == MenuMode.Main
            ? UpdateMainMenu(input)
            : UpdateLoadMenu(input);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;

        // Background
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(15, 12, 24));

        // Title
        var title = "The Last Mage Standing";
        var titleSize = _titleFont.MeasureString(title);
        var titlePosition = new Vector2(
            (viewport.Width - titleSize.X) * 0.5f,
            80f);
        spriteBatch.DrawString(_titleFont, title, titlePosition, Color.Gold);

        if (_mode == MenuMode.Main)
        {
            DrawMainMenu(spriteBatch);
        }
        else
        {
            DrawLoadMenu(spriteBatch);
        }

        if (!string.IsNullOrWhiteSpace(_statusMessage))
        {
            var statusSize = _bodyFont.MeasureString(_statusMessage);
            spriteBatch.DrawString(
                _bodyFont,
                _statusMessage,
                new Vector2((viewport.Width - statusSize.X) * 0.5f, viewport.Height - 60),
                Color.LightGray);
        }
    }

    private MainMenuResult UpdateMainMenu(InputState input)
    {
        var entries = BuildMainMenuEntries();
        HandleMouseNavigation(input, entries, ref _selectedMainIndex);

        if (input.MenuDownPressed)
        {
            _selectedMainIndex = (_selectedMainIndex + 1) % entries.Count;
        }
        else if (input.MenuUpPressed)
        {
            _selectedMainIndex = (_selectedMainIndex - 1 + entries.Count) % entries.Count;
        }

        if (input.MenuConfirmPressed || input.PrimaryClickPressed && IsMouseOverSelection(input, entries, _selectedMainIndex))
        {
            var entry = entries[_selectedMainIndex];
            switch (entry.Id)
            {
                case "continue":
                    var mostRecent = _saveSlotService.GetMostRecentSlot();
                    if (mostRecent != null)
                    {
                        return new MainMenuResult(MainMenuAction.StartSlot, mostRecent.SlotId);
                    }
                    return new MainMenuResult(MainMenuAction.CreateNewSlot);
                case "new":
                    return new MainMenuResult(MainMenuAction.CreateNewSlot);
                case "load":
                    _mode = MenuMode.LoadSlots;
                    _selectedLoadIndex = 0;
                    _statusMessage = string.Empty;
                    break;
                case "settings":
                    _statusMessage = "Settings coming soon.";
                    break;
                case "quit":
                    return new MainMenuResult(MainMenuAction.Quit);
            }
        }

        return new MainMenuResult(MainMenuAction.None);
    }

    private MainMenuResult UpdateLoadMenu(InputState input)
    {
        if (_slots.Count == 0 && _statusMessage == string.Empty)
        {
            _statusMessage = "No saves found. Create a new game to start.";
        }

        HandleMouseNavigation(input, _slots.Select((s, i) => BuildLoadEntryRect(i)).ToList(), ref _selectedLoadIndex);

        if (input.MenuDownPressed && _slots.Count > 0)
        {
            _selectedLoadIndex = (_selectedLoadIndex + 1) % _slots.Count;
        }
        else if (input.MenuUpPressed && _slots.Count > 0)
        {
            _selectedLoadIndex = (_selectedLoadIndex - 1 + _slots.Count) % _slots.Count;
        }

        if (input.MenuBackPressed)
        {
            _mode = MenuMode.Main;
            _statusMessage = string.Empty;
            return new MainMenuResult(MainMenuAction.None);
        }

        if ((_slots.Count > 0) &&
            (input.MenuConfirmPressed || (input.PrimaryClickPressed && IsMouseOverSelection(input, _slots.Select((_, i) => BuildLoadEntryRect(i)).ToList(), _selectedLoadIndex))))
        {
            var selected = _slots[_selectedLoadIndex];
            return new MainMenuResult(MainMenuAction.StartSlot, selected.SlotId);
        }

        return new MainMenuResult(MainMenuAction.None);
    }

    private void DrawMainMenu(SpriteBatch spriteBatch)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;
        var entries = BuildMainMenuEntries();

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var isSelected = i == _selectedMainIndex;
            DrawButton(spriteBatch, entry.Bounds, entry.Label, isSelected);
        }
    }

    private void DrawLoadMenu(SpriteBatch spriteBatch)
    {
        var viewport = spriteBatch.GraphicsDevice.Viewport;
        var header = "Select Save Slot";
        var headerSize = _bodyFont.MeasureString(header);
        spriteBatch.DrawString(
            _bodyFont,
            header,
            new Vector2((viewport.Width - headerSize.X) * 0.5f, StartY - 50),
            Color.LightGray);

        if (_slots.Count == 0)
        {
            var noSave = "No saves found.";
            var size = _bodyFont.MeasureString(noSave);
            spriteBatch.DrawString(
                _bodyFont,
                noSave,
                new Vector2((viewport.Width - size.X) * 0.5f, StartY),
                Color.Gray);
            return;
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            var rect = BuildLoadEntryRect(i);
            var isSelected = i == _selectedLoadIndex;
            var label = BuildSlotLabel(slot);
            DrawButton(spriteBatch, rect, label, isSelected);
        }
    }

    private Rectangle BuildLoadEntryRect(int index)
    {
        var viewport = _graphicsDevice.Viewport;
        var x = (viewport.Width - ButtonWidth) / 2;
        var y = StartY + index * (ButtonHeight + ButtonSpacing);
        return new Rectangle(x, y, ButtonWidth, ButtonHeight);
    }

    private List<(string Id, string Label, Rectangle Bounds)> BuildMainMenuEntries()
    {
        var viewport = _graphicsDevice.Viewport;
        var x = (viewport.Width - ButtonWidth) / 2;
        var labels = new List<(string Id, string Label)>
        {
            ("continue", _slots.Any(s => s.HasProfileData) ? "Continue" : "Start"),
            ("new", "New Game"),
            ("load", "Load Game"),
            ("settings", "Settings"),
            ("quit", "Quit")
        };

        var entries = new List<(string Id, string Label, Rectangle Bounds)>();
        for (int i = 0; i < labels.Count; i++)
        {
            var y = StartY + i * (ButtonHeight + ButtonSpacing);
            var rect = new Rectangle(x, y, ButtonWidth, ButtonHeight);
            entries.Add((labels[i].Id, labels[i].Label, rect));
        }

        return entries;
    }

    private void DrawButton(SpriteBatch spriteBatch, Rectangle rect, string text, bool isSelected)
    {
        var bgColor = isSelected ? new Color(60, 60, 100, 220) : new Color(35, 35, 50, 200);
        var borderColor = isSelected ? Color.Gold : new Color(90, 90, 110);

        spriteBatch.Draw(_pixel, rect, bgColor);
        DrawBorder(spriteBatch, rect, borderColor, 2);

        var textSize = _bodyFont.MeasureString(text);
        var textPos = new Vector2(
            rect.X + (rect.Width - textSize.X) * 0.5f,
            rect.Y + (rect.Height - textSize.Y) * 0.5f);
        spriteBatch.DrawString(_bodyFont, text, textPos, Color.White);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    private void RefreshSlots()
    {
        _slots = _saveSlotService.GetSlots().ToList();
        _selectedMainIndex = Math.Clamp(_selectedMainIndex, 0, Math.Max(0, BuildMainMenuEntries().Count - 1));
        _selectedLoadIndex = Math.Clamp(_selectedLoadIndex, 0, Math.Max(0, _slots.Count - 1));
    }

    private void HandleMouseNavigation(InputState input, IList<Rectangle> hitAreas, ref int selectedIndex)
    {
        var mousePoint = new Point((int)input.MouseScreenPosition.X, (int)input.MouseScreenPosition.Y);
        for (int i = 0; i < hitAreas.Count; i++)
        {
            if (hitAreas[i].Contains(mousePoint))
            {
                selectedIndex = i;
                break;
            }
        }
    }

    private void HandleMouseNavigation(InputState input, IList<(string Id, string Label, Rectangle Bounds)> entries, ref int selectedIndex)
    {
        var mousePoint = new Point((int)input.MouseScreenPosition.X, (int)input.MouseScreenPosition.Y);
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Bounds.Contains(mousePoint))
            {
                selectedIndex = i;
                break;
            }
        }
    }

    private bool IsMouseOverSelection(InputState input, IList<Rectangle> entries, int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= entries.Count)
        {
            return false;
        }

        var mousePoint = new Point((int)input.MouseScreenPosition.X, (int)input.MouseScreenPosition.Y);
        return entries[selectedIndex].Contains(mousePoint);
    }

    private bool IsMouseOverSelection(InputState input, IList<(string Id, string Label, Rectangle Bounds)> entries, int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= entries.Count)
        {
            return false;
        }

        var mousePoint = new Point((int)input.MouseScreenPosition.X, (int)input.MouseScreenPosition.Y);
        return entries[selectedIndex].Bounds.Contains(mousePoint);
    }

    private static string BuildSlotLabel(SaveSlotInfo slot)
    {
        var lastPlayed = slot.LastPlayedAt.HasValue
            ? slot.LastPlayedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "Never played";
        var created = slot.CreatedAt.HasValue
            ? slot.CreatedAt.Value.ToLocalTime().ToString("yyyy-MM-dd")
            : "Unknown";

        return $"{slot.SlotId.ToUpperInvariant()} — Last Played: {lastPlayed} | Created: {created}";
    }

    private static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var pixel = new Texture2D(graphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        return pixel;
    }
}

