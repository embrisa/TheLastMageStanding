using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.UI;

/// <summary>
/// Myra-based stage selection overlay used inside the hub.
/// The screen owns layout/rendering only and applies externally prepared view state.
/// </summary>
internal sealed class MyraStageSelectionScreen : IDisposable
{
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private IUiSoundPlayer? _uiSoundPlayer;

    private Desktop _desktop = null!;
    private RenderTarget2D? _renderTarget;
    private SpriteBatch? _spriteBatch;

    private Label _actLabel = null!;
    private Label _metaLabel = null!;
    private Label _helpText = null!;
    private Grid _stageListGrid = null!;
    private Label _detailsTitle = null!;
    private Label _detailsDescription = null!;
    private Label _detailsStatus = null!;
    private Button _startButton = null!;
    private readonly List<Button> _stageButtons = new();
    private StageSelectionScreenState _lastState;

    public event Action<int>? ActChangeRequested;
    public event Action<int>? StageSelected;
    public event Action? StartRequested;
    public event Action? BackRequested;

    public MyraStageSelectionScreen(
        int virtualWidth = 960,
        int virtualHeight = 540,
        IUiSoundPlayer? uiSoundPlayer = null)
    {
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
        _uiSoundPlayer = uiSoundPlayer;

        BuildLayout();
    }

    public bool IsVisible => _desktop.Root.Visible;

    public void Hide()
    {
        _desktop.Root.Visible = false;
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
        if (MyraEnvironment.Game == null)
        {
            return;
        }

        var bounds = MyraEnvironment.Game.Window.ClientBounds;
        var device = MyraEnvironment.Game.GraphicsDevice;
        if (_renderTarget == null ||
            _renderTarget.Width != bounds.Width ||
            _renderTarget.Height != bounds.Height)
        {
            _renderTarget?.Dispose();
            _renderTarget = new RenderTarget2D(device, bounds.Width, bounds.Height);
            _spriteBatch ??= new SpriteBatch(device);
        }

        _desktop.Scale = new Vector2(
            (float)bounds.Width / _virtualWidth,
            (float)bounds.Height / _virtualHeight);
    }

    public void Render()
    {
        if (_renderTarget == null || _spriteBatch == null || MyraEnvironment.Game == null)
        {
            return;
        }

        var device = MyraEnvironment.Game.GraphicsDevice;
        var oldTargets = device.GetRenderTargets();

        device.SetRenderTarget(_renderTarget);
        device.Clear(Color.Transparent);
        _desktop.Render();

        device.SetRenderTargets(oldTargets);
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        _spriteBatch.Draw(_renderTarget, new Rectangle(0, 0, device.Viewport.Width, device.Viewport.Height), Color.White);
        _spriteBatch.End();
    }

    public void ApplyState(StageSelectionScreenState state)
    {
        _lastState = state;
        _desktop.Root.Visible = state.IsOpen;
        if (!state.IsOpen)
        {
            return;
        }

        _metaLabel.Text = state.MetaText;
        _helpText.Text = state.HelpText;

        if (state.Acts.Count > 0 && state.SelectedActIndex >= 0 && state.SelectedActIndex < state.Acts.Count)
        {
            var act = state.Acts[state.SelectedActIndex];
            _actLabel.Text = $"ACT {act.ActNumber} - {act.Title}";
        }
        else
        {
            _actLabel.Text = "ACT";
        }

        RebuildStageList(state.Stages);
        UpdateDetails(state.Details);
    }

    private void BuildLayout()
    {
        var root = new Panel
        {
            Background = new SolidBrush(new Color(15, 15, 25, 240)),
            Visible = false,
            Width = _virtualWidth,
            Height = _virtualHeight
        };

        var mainGrid = new Grid
        {
            RowSpacing = 12,
            ColumnSpacing = 12,
            Padding = new Thickness(32),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Fill));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var headerGrid = new Grid
        {
            ColumnSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var actSelector = new HorizontalStackPanel { Spacing = 8 };
        var prevActButton = CreateNavButton("<");
        prevActButton.Click += (_, _) => ActChangeRequested?.Invoke(-1);
        var nextActButton = CreateNavButton(">");
        nextActButton.Click += (_, _) => ActChangeRequested?.Invoke(1);

        _actLabel = new Label
        {
            Text = "ACT I",
            TextColor = Color.Gold,
            VerticalAlignment = VerticalAlignment.Center
        };
        UiFonts.ApplyHeading(_actLabel, 1.0f);

        actSelector.Widgets.Add(prevActButton);
        actSelector.Widgets.Add(_actLabel);
        actSelector.Widgets.Add(nextActButton);
        Grid.SetColumn(actSelector, 0);
        headerGrid.Widgets.Add(actSelector);

        _metaLabel = new Label
        {
            Text = string.Empty,
            TextColor = Color.Cyan,
            VerticalAlignment = VerticalAlignment.Center
        };
        UiFonts.ApplyBody(_metaLabel, 0.9f);
        Grid.SetColumn(_metaLabel, 2);
        headerGrid.Widgets.Add(_metaLabel);

        Grid.SetRow(headerGrid, 0);
        mainGrid.Widgets.Add(headerGrid);

        var contentGrid = new Grid { ColumnSpacing = 24 };
        contentGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1));
        contentGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 2));

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

        var detailsPanel = new Panel
        {
            Background = new SolidBrush(new Color(30, 30, 40, 150)),
            Border = new SolidBrush(Color.Gray),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(24),
            ClipToBounds = true
        };

        var detailsLayout = new Grid { RowSpacing = 16 };
        detailsLayout.RowsProportions.Add(new Proportion(ProportionType.Auto));
        detailsLayout.RowsProportions.Add(new Proportion(ProportionType.Auto));
        detailsLayout.RowsProportions.Add(new Proportion(ProportionType.Fill));
        detailsLayout.RowsProportions.Add(new Proportion(ProportionType.Auto));

        _detailsTitle = new Label
        {
            Text = string.Empty,
            TextColor = Color.White,
            Wrap = true
        };
        UiFonts.ApplyHeading(_detailsTitle, 1.2f);
        Grid.SetRow(_detailsTitle, 0);
        detailsLayout.Widgets.Add(_detailsTitle);

        _detailsStatus = new Label
        {
            Text = string.Empty,
            TextColor = Color.Gray
        };
        UiFonts.ApplyBody(_detailsStatus);
        Grid.SetRow(_detailsStatus, 1);
        detailsLayout.Widgets.Add(_detailsStatus);

        _detailsDescription = new Label
        {
            Text = string.Empty,
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
        _startButton.Click += (_, _) => StartRequested?.Invoke();
        UiSoundBinder.BindHoverAndClick(_startButton, _uiSoundPlayer);
        Grid.SetRow(_startButton, 3);
        detailsLayout.Widgets.Add(_startButton);

        detailsPanel.Widgets.Add(detailsLayout);
        Grid.SetColumn(detailsPanel, 1);
        contentGrid.Widgets.Add(detailsPanel);

        Grid.SetRow(contentGrid, 1);
        mainGrid.Widgets.Add(contentGrid);

        var footerGrid = new Grid { ColumnSpacing = 10 };
        footerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        footerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var backButton = new Button
        {
            Content = new Label { Text = "Back" },
            Width = 100
        };
        if (backButton.Content is Label backLabel)
        {
            UiFonts.ApplyBody(backLabel, 0.95f);
        }
        backButton.Click += (_, _) => BackRequested?.Invoke();
        UiSoundBinder.BindHoverAndClick(backButton, _uiSoundPlayer);
        var footerLeft = new HorizontalStackPanel();
        footerLeft.Widgets.Add(backButton);
        Grid.SetColumn(footerLeft, 0);
        footerGrid.Widgets.Add(footerLeft);

        _helpText = new Label
        {
            Text = string.Empty,
            TextColor = Color.Gray,
            VerticalAlignment = VerticalAlignment.Center
        };
        UiFonts.ApplyBody(_helpText);
        Grid.SetColumn(_helpText, 1);
        footerGrid.Widgets.Add(_helpText);

        Grid.SetRow(footerGrid, 2);
        mainGrid.Widgets.Add(footerGrid);

        root.Widgets.Add(mainGrid);
        _desktop = new Desktop { Root = root };
    }

    private void RebuildStageList(IReadOnlyList<StageSelectionListItemViewModel> stages)
    {
        _stageListGrid.Widgets.Clear();
        _stageListGrid.RowsProportions.Clear();
        _stageButtons.Clear();

        if (stages.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "No stages available.",
                TextColor = Color.Gray,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            UiFonts.ApplyBody(emptyLabel);
            _stageListGrid.Widgets.Add(emptyLabel);
            return;
        }

        for (var index = 0; index < stages.Count; index++)
        {
            var stage = stages[index];
            _stageListGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            var button = CreateListItem(stage);
            var capturedIndex = index;
            button.Click += (_, _) => StageSelected?.Invoke(capturedIndex);

            Grid.SetRow(button, index);
            _stageListGrid.Widgets.Add(button);
            _stageButtons.Add(button);
        }
    }

    private Button CreateListItem(StageSelectionListItemViewModel stage)
    {
        var label = new Label
        {
            Text = stage.Label,
            TextColor = stage.IsUnlocked ? Color.White : Color.Gray
        };
        UiFonts.ApplyBody(label, 0.9f);

        var button = new Button
        {
            Content = label,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 40,
            Background = stage.IsSelected ? new SolidBrush(new Color(60, 60, 80, 200)) : null,
            Border = stage.IsSelected ? new SolidBrush(Color.Cyan) : null,
            BorderThickness = stage.IsSelected ? new Thickness(1) : new Thickness(0)
        };
        UiSoundBinder.BindHoverAndClick(button, _uiSoundPlayer);
        return button;
    }

    private void UpdateDetails(StageSelectionDetailsViewModel details)
    {
        _detailsTitle.Text = details.Title;
        _detailsDescription.Text = details.Description;
        _detailsStatus.Text = details.StatusText;

        _detailsStatus.TextColor = details.StatusText switch
        {
            "COMPLETED" => Color.LightGreen,
            "AVAILABLE" => Color.Cyan,
            _ when details.CanStart => Color.Cyan,
            _ => Color.OrangeRed
        };

        _startButton.Enabled = details.CanStart;
        if (_startButton.Content is Label startLabel)
        {
            startLabel.Text = details.StartButtonText;
        }

        _startButton.Background = details.CanStart && details.StartButtonText == "REPLAY STAGE"
            ? new SolidBrush(new Color(0, 80, 0))
            : details.CanStart
                ? new SolidBrush(new Color(0, 100, 0))
                : new SolidBrush(new Color(50, 50, 50));
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
}
