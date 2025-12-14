using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using TheLastMageStanding.Game.Core.Campaign;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.UI;

/// <summary>
/// Myra-based stage selection overlay used inside the hub.
/// Provides act navigation, stage list, details, and start/back actions.
/// </summary>
internal sealed class MyraStageSelectionScreen : IDisposable
{
    private readonly StageRegistry _stageRegistry;
    private readonly CampaignProgressionService _campaignProgressionService;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private IUiSoundPlayer? _uiSoundPlayer;

    private Desktop _desktop = null!;
    private RenderTarget2D? _renderTarget;
    private SpriteBatch? _spriteBatch;
    
    // UI Widgets
    private Label _actLabel = null!;
    private Label _metaLabel = null!;
    private Grid _stageListGrid = null!;
    
    // Details Panel Widgets
    private Panel _detailsPanel = null!;
    private Label _detailsTitle = null!;
    private Label _detailsDescription = null!;
    private Label _detailsStatus = null!;
    private Button _startButton = null!;
    
    private readonly List<Button> _stageButtons = new();
    private IReadOnlyList<StageDefinition> _currentStages = Array.Empty<StageDefinition>();
    private PlayerProfile _profile = PlayerProfile.CreateDefault();
    private int _selectedActIndex;
    private int _selectedStageIndex;

    public event Action<string>? StartRequested;
    public event Action? BackRequested;

    public MyraStageSelectionScreen(
        StageRegistry stageRegistry,
        CampaignProgressionService campaignProgressionService,
        int virtualWidth = 960,
        int virtualHeight = 540,
        IUiSoundPlayer? uiSoundPlayer = null)
    {
        _stageRegistry = stageRegistry;
        _campaignProgressionService = campaignProgressionService;
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
        _uiSoundPlayer = uiSoundPlayer;

        BuildLayout();
    }

    public void SetSoundPlayer(IUiSoundPlayer player)
    {
        _uiSoundPlayer = player;
    }

    public void Dispose()
    {
        _desktop?.Dispose();
        _renderTarget?.Dispose();
        _spriteBatch?.Dispose();
    }

    public void Update(GameTime gameTime)
    {
        if (MyraEnvironment.Game != null)
        {
            var bounds = MyraEnvironment.Game.Window.ClientBounds;
            var device = MyraEnvironment.Game.GraphicsDevice;

            // Ensure RenderTarget matches Window size (HighDPI support)
            if (_renderTarget == null || 
                _renderTarget.Width != bounds.Width || 
                _renderTarget.Height != bounds.Height)
            {
                _renderTarget?.Dispose();
                _renderTarget = new RenderTarget2D(device, bounds.Width, bounds.Height);
                _spriteBatch ??= new SpriteBatch(device);
            }

            // Scale UI to match the Window size (so mouse coordinates align)
            _desktop.Scale = new Vector2(
                (float)bounds.Width / _virtualWidth,
                (float)bounds.Height / _virtualHeight);
        }
    }

    public void Render()
    {
        if (_renderTarget == null || _spriteBatch == null || MyraEnvironment.Game == null)
        {
            return;
        }

        var device = MyraEnvironment.Game.GraphicsDevice;
        var oldTargets = device.GetRenderTargets();

        // 1. Render UI to RenderTarget (at Window resolution)
        device.SetRenderTarget(_renderTarget);
        device.Clear(Color.Transparent);
        _desktop.Render();

        // 2. Draw RenderTarget to Screen (scaled to fit Backbuffer)
        device.SetRenderTargets(oldTargets);
        
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        _spriteBatch.Draw(_renderTarget, new Rectangle(0, 0, device.Viewport.Width, device.Viewport.Height), Color.White);
        _spriteBatch.End();
    }

    public void Show(int actIndex, int stageIndex)
    {
        _desktop.Root.Visible = true;
        _selectedActIndex = Math.Clamp(actIndex, 0, GetMaxActIndex());
        _selectedStageIndex = stageIndex;
        RefreshView();
    }

    public void Hide()
    {
        _desktop.Root.Visible = false;
    }

    public int SelectedActIndex => _selectedActIndex;
    public int SelectedStageIndex => _selectedStageIndex;
    public bool IsVisible => _desktop.Root.Visible;

    public void ChangeAct(int delta)
    {
        var maxAct = GetMaxActIndex();
        var next = Math.Clamp(_selectedActIndex + delta, 0, maxAct);
        if (next == _selectedActIndex) return;

        _selectedActIndex = next;
        _selectedStageIndex = 0;
        RefreshView();
    }

    public void MoveSelection(int delta)
    {
        if (_currentStages.Count == 0) return;
        var next = (_selectedStageIndex + delta + _currentStages.Count) % _currentStages.Count;
        SetSelection(next);
    }

    public void StartSelectedStage()
    {
        var selectedStage = GetSelectedStage();
        if (selectedStage == null) return;

        if (!_campaignProgressionService.IsStageUnlocked(selectedStage, _profile)) return;

        UiSoundBinder.PlayKeyboardActivate(_uiSoundPlayer);
        StartRequested?.Invoke(selectedStage.StageId);
    }

    public void Close()
    {
        UiSoundBinder.PlayKeyboardCancel(_uiSoundPlayer);
        BackRequested?.Invoke();
    }

    private void BuildLayout()
    {
        var root = new Panel
        {
            Background = new SolidBrush(new Color(15, 15, 25, 240)), // Darker, more opaque background
            Visible = false,
            Width = _virtualWidth,
            Height = _virtualHeight
        };

        // Main container with padding
        var mainGrid = new Grid
        {
            RowSpacing = 12,
            ColumnSpacing = 12,
            Padding = new Thickness(32),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        // Rows: Header, Content, Footer
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Header
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Fill)); // Content (Split view)
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Footer

        // --- Header ---
        var headerGrid = new Grid
        {
            ColumnSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Act Selector
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill)); // Spacer/Title
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Meta Level

        // Act Selector
        var actSelector = new HorizontalStackPanel { Spacing = 8 };
        var prevActBtn = CreateNavButton("<");
        prevActBtn.Click += (_, _) => ChangeAct(-1);
        
        _actLabel = new Label
        {
            Text = "ACT I",
            TextColor = Color.Gold,
            VerticalAlignment = VerticalAlignment.Center
        };
        UiFonts.ApplyHeading(_actLabel, 1.0f);
        
        var nextActBtn = CreateNavButton(">");
        nextActBtn.Click += (_, _) => ChangeAct(1);

        actSelector.Widgets.Add(prevActBtn);
        actSelector.Widgets.Add(_actLabel);
        actSelector.Widgets.Add(nextActBtn);
        
        Grid.SetColumn(actSelector, 0);
        headerGrid.Widgets.Add(actSelector);

        // Meta Level
        _metaLabel = new Label
        {
            Text = "Meta Level: 1",
            TextColor = Color.Cyan,
            VerticalAlignment = VerticalAlignment.Center
        };
        UiFonts.ApplyBody(_metaLabel, 0.9f);
        Grid.SetColumn(_metaLabel, 2);
        headerGrid.Widgets.Add(_metaLabel);

        Grid.SetRow(headerGrid, 0);
        mainGrid.Widgets.Add(headerGrid);

        // --- Content (Split View) ---
        var contentGrid = new Grid
        {
            ColumnSpacing = 24
        };
        contentGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1)); // List (1/3)
        contentGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 2)); // Details (2/3)

        // Left: Stage List
        var listPanel = new Panel
        {
            Background = new SolidBrush(new Color(30, 30, 40, 150)),
            Border = new SolidBrush(Color.Gray),
            BorderThickness = new Thickness(1),
            ClipToBounds = true
        };
        
        _stageListGrid = new Grid { RowSpacing = 4, Padding = new Thickness(8) };
        var scrollViewer = new ScrollViewer
        {
            Content = _stageListGrid,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        listPanel.Widgets.Add(scrollViewer);
        
        Grid.SetColumn(listPanel, 0);
        contentGrid.Widgets.Add(listPanel);

        // Right: Details
        _detailsPanel = new Panel
        {
            Background = new SolidBrush(new Color(30, 30, 40, 150)),
            Border = new SolidBrush(Color.Gray),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(24),
            ClipToBounds = true
        };

        var detailsLayout = new Grid { RowSpacing = 16 };
        detailsLayout.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Title
        detailsLayout.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Status/Reqs
        detailsLayout.RowsProportions.Add(new Proportion(ProportionType.Fill)); // Description
        detailsLayout.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Action Button

        _detailsTitle = new Label
        {
            Text = "Stage Title",
            TextColor = Color.White,
            Wrap = true
        };
        UiFonts.ApplyHeading(_detailsTitle, 1.2f);
        Grid.SetRow(_detailsTitle, 0);
        detailsLayout.Widgets.Add(_detailsTitle);

        _detailsStatus = new Label
        {
            Text = "Status",
            TextColor = Color.Gray
        };
        UiFonts.ApplyBody(_detailsStatus);
        Grid.SetRow(_detailsStatus, 1);
        detailsLayout.Widgets.Add(_detailsStatus);

        _detailsDescription = new Label
        {
            Text = "Description goes here...",
            TextColor = Color.LightGray,
            Wrap = true
        };
        UiFonts.ApplyBody(_detailsDescription);
        Grid.SetRow(_detailsDescription, 2);
        detailsLayout.Widgets.Add(_detailsDescription);

        _startButton = new Button
        {
            Content = new Label { Text = "ENTER STAGE", HorizontalAlignment = HorizontalAlignment.Center },
            Height = 50,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new SolidBrush(new Color(0, 100, 0)),
            OverBackground = new SolidBrush(new Color(0, 150, 0)),
            DisabledBackground = new SolidBrush(new Color(50, 50, 50))
        };
        if (_startButton.Content is Label startLabel)
        {
            UiFonts.ApplyBody(startLabel, 1.0f);
        }
        _startButton.Click += (_, _) => StartSelectedStage();
        UiSoundBinder.BindHoverAndClick(_startButton, _uiSoundPlayer);
        Grid.SetRow(_startButton, 3);
        detailsLayout.Widgets.Add(_startButton);

        _detailsPanel.Widgets.Add(detailsLayout);
        Grid.SetColumn(_detailsPanel, 1);
        contentGrid.Widgets.Add(_detailsPanel);

        Grid.SetRow(contentGrid, 1);
        mainGrid.Widgets.Add(contentGrid);

        // --- Footer ---
        var footerGrid = new Grid
        {
            ColumnSpacing = 10
        };
        footerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        footerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var backBtn = new Button
        {
            Content = new Label { Text = "Back" },
            Width = 100
        };
        if (backBtn.Content is Label backLabel)
        {
            UiFonts.ApplyBody(backLabel, 0.95f);
        }
        backBtn.Click += (_, _) => Close();
        UiSoundBinder.BindHoverAndClick(backBtn, _uiSoundPlayer);
        Grid.SetColumn(backBtn, 0);
        // Align back button to left, but we put it in a grid to have the help text on right
        var footerLeft = new HorizontalStackPanel();
        footerLeft.Widgets.Add(backBtn);
        Grid.SetColumn(footerLeft, 0);
        footerGrid.Widgets.Add(footerLeft);

        var helpText = new Label
        {
            Text = "[WASD] Navigate  [ENTER] Select  [ESC] Back",
            TextColor = Color.Gray,
            VerticalAlignment = VerticalAlignment.Center
        };
        UiFonts.ApplyBody(helpText);
        Grid.SetColumn(helpText, 1);
        footerGrid.Widgets.Add(helpText);

        Grid.SetRow(footerGrid, 2);
        mainGrid.Widgets.Add(footerGrid);

        root.Widgets.Add(mainGrid);
        _desktop = new Desktop { Root = root };
    }

    private void RefreshView()
    {
        _profile = _campaignProgressionService.LoadProfile();
        _currentStages = _stageRegistry.GetStagesForAct(_selectedActIndex + 1);
        _selectedStageIndex = Math.Clamp(_selectedStageIndex, 0, Math.Max(0, _currentStages.Count - 1));

        var act = _stageRegistry.GetAct(_selectedActIndex + 1);
        _actLabel.Text = act != null
            ? $"ACT {act.ActNumber} - {act.DisplayName}"
            : $"ACT {_selectedActIndex + 1}";
        _metaLabel.Text = $"Meta Level: {_profile.MetaLevel}";

        _stageListGrid.Widgets.Clear();
        _stageListGrid.RowsProportions.Clear();
        _stageButtons.Clear();

        if (_currentStages.Count == 0)
        {
            var emptyLabel = new Label { Text = "No stages available.", TextColor = Color.Gray, HorizontalAlignment = HorizontalAlignment.Center };
            UiFonts.ApplyBody(emptyLabel);
            _stageListGrid.Widgets.Add(emptyLabel);
            UpdateDetails(null);
            return;
        }

        for (int i = 0; i < _currentStages.Count; i++)
        {
            var stage = _currentStages[i];
            var isUnlocked = _campaignProgressionService.IsStageUnlocked(stage, _profile);
            var isCompleted = _profile.CompletedStages.Contains(stage.StageId);
            
            _stageListGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            
            var btn = CreateListItem(stage, isUnlocked, isCompleted);
            int idx = i;
            btn.Click += (_, _) => SetSelection(idx);
            
            Grid.SetRow(btn, i);
            _stageListGrid.Widgets.Add(btn);
            _stageButtons.Add(btn);
        }

        SetSelection(_selectedStageIndex);
    }

    private Button CreateListItem(StageDefinition stage, bool isUnlocked, bool isCompleted)
    {
        var color = isUnlocked ? Color.White : Color.Gray;
        var icon = isCompleted ? "✓ " : (!isUnlocked ? "[L] " : "  ");
        
        var label = new Label
        {
            Text = $"{icon}{stage.DisplayName}",
            TextColor = color
        };
        UiFonts.ApplyBody(label, 0.9f);

        var btn = new Button
        {
            Content = label,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 40,
            Background = null, // Transparent by default
            Border = null
        };
        UiSoundBinder.BindHoverAndClick(btn, _uiSoundPlayer);
        return btn;
    }

    private void SetSelection(int index)
    {
        if (_currentStages.Count == 0)
        {
            UpdateDetails(null);
            return;
        }

        _selectedStageIndex = Math.Clamp(index, 0, _currentStages.Count - 1);
        
        // Update list highlighting
        for (int i = 0; i < _stageButtons.Count; i++)
        {
            var btn = _stageButtons[i];
            if (i == _selectedStageIndex)
            {
                btn.Background = new SolidBrush(new Color(60, 60, 80, 200));
                btn.Border = new SolidBrush(Color.Cyan);
                btn.BorderThickness = new Thickness(1);
            }
            else
            {
                btn.Background = null;
                btn.Border = null;
            }
        }

        UpdateDetails(_currentStages[_selectedStageIndex]);
        PlaySelectionHover();
    }

    private void UpdateDetails(StageDefinition? stage)
    {
        if (stage == null)
        {
            _detailsTitle.Text = "";
            _detailsDescription.Text = "Select a stage.";
            _detailsStatus.Text = "";
            _startButton.Enabled = false;
            _startButton.Content = new Label { Text = "LOCKED", HorizontalAlignment = HorizontalAlignment.Center };
            if (_startButton.Content is Label lockedLabel)
            {
                UiFonts.ApplyBody(lockedLabel, 1.0f);
            }
            return;
        }

        _detailsTitle.Text = stage.DisplayName;
        _detailsDescription.Text = stage.Description;

        var isUnlocked = IsStageUnlocked(stage, _profile);
        var isCompleted = _profile.CompletedStages.Contains(stage.StageId);

        if (isCompleted)
        {
            _detailsStatus.Text = "COMPLETED";
            _detailsStatus.TextColor = Color.LightGreen;
            _startButton.Enabled = true;
            ((Label)_startButton.Content).Text = "REPLAY STAGE";
            _startButton.Background = new SolidBrush(new Color(0, 80, 0));
        }
        else if (isUnlocked)
        {
            _detailsStatus.Text = "AVAILABLE";
            _detailsStatus.TextColor = Color.Cyan;
            _startButton.Enabled = true;
            ((Label)_startButton.Content).Text = "ENTER STAGE";
            _startButton.Background = new SolidBrush(new Color(0, 100, 0));
        }
        else
        {
            _detailsStatus.Text = $"LOCKED - {_campaignProgressionService.GetLockReason(stage, _profile)}";
            _detailsStatus.TextColor = Color.OrangeRed;
            _startButton.Enabled = false;
            ((Label)_startButton.Content).Text = "LOCKED";
            _startButton.Background = new SolidBrush(new Color(50, 50, 50));
        }
    }

    private void PlaySelectionHover()
    {
        var widget = _selectedStageIndex >= 0 && _selectedStageIndex < _stageButtons.Count
            ? _stageButtons[_selectedStageIndex]
            : null;

        UiSoundBinder.PlayKeyboardHover(widget, _uiSoundPlayer);
    }

    private Button CreateNavButton(string text)
    {
        var button = new Button
        {
            Content = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center },
            Width = 40,
            Height = 40
        };
        if (button.Content is Label label)
        {
            UiFonts.ApplyBody(label, 0.9f);
        }
        UiSoundBinder.BindHoverAndClick(button, _uiSoundPlayer);
        return button;
    }

    private StageDefinition? GetSelectedStage()
    {
        if (_currentStages.Count == 0 || _selectedStageIndex < 0 || _selectedStageIndex >= _currentStages.Count)
        {
            return null;
        }

        return _currentStages[_selectedStageIndex];
    }

    private int GetMaxActIndex()
    {
        var maxAct = _stageRegistry.GetAllStages()
            .Select(s => s.ActNumber)
            .DefaultIfEmpty(1)
            .Max();

        return Math.Max(0, maxAct - 1);
    }

    private bool IsStageUnlocked(StageDefinition stage, PlayerProfile profile) =>
        _campaignProgressionService.IsStageUnlocked(stage, profile);
}
