using System;
using System.Collections.Generic;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using TheLastMageStanding.Game.Core.Ecs.Components;
using TheLastMageStanding.Game.Core.Events;

namespace TheLastMageStanding.Game.Core.UI.Myra;

/// <summary>
/// Myra-based level-up choice overlay with three clickable cards.
/// </summary>
internal sealed class MyraLevelUpChoiceScreen : MyraMenuScreenBase
{
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly KeyboardHintBar _hintBar;
    private readonly List<Button> _choiceButtons = new();
    private readonly List<Label> _choiceTitles = new();
    private readonly List<Label> _choiceDescriptions = new();
    private readonly List<Label> _choiceFooters = new();
    private readonly Dictionary<Button, string> _choiceIds = new();
    private IUiSoundPlayer? _uiSoundPlayer;
    private bool _canSelect;

    public MyraLevelUpChoiceScreen(IUiSoundPlayer? uiSoundPlayer = null) : base(useRenderTarget: true)
    {
        _uiSoundPlayer = uiSoundPlayer;

        var root = UiStyles.ScreenOverlay();
        root.Visible = false;
        root.Width = VirtualWidth;
        root.Height = VirtualHeight;

        var layout = new MenuColumn(UiTheme.LargeSpacing)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(UiTheme.LargePadding)
        };

        _titleLabel = UiStyles.Heading("Level Up!", 1.6f, UiTheme.AccentText);
        layout.AddRow(_titleLabel);

        _subtitleLabel = UiStyles.BodyText("Choose 1 of 3 options", UiTheme.MutedText, wrap: true, scale: 1.0f);
        _subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        layout.AddRow(_subtitleLabel);

        var cardsRow = new HorizontalStackPanel
        {
            Spacing = UiTheme.LargeSpacing,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        for (var i = 0; i < 3; i++)
        {
            cardsRow.Widgets.Add(BuildChoiceCard(i));
        }

        layout.AddRow(cardsRow);

        _hintBar = new KeyboardHintBar();
        _hintBar.SetHints(
            ("A/D", "Navigate"),
            ("Enter", "Select"),
            ("Mouse", "Click card"));
        layout.AddRow(_hintBar);

        root.Widgets.Add(layout);
        Desktop.Root = root;
    }

    public event Action<string>? ChoicePicked;

    public bool IsVisible => Desktop.Root.Visible;

    public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
        base.Update(gameTime);
    }

    public void ApplyViewModel(LevelUpChoiceViewModel viewModel)
    {
        Desktop.Root.Visible = viewModel.IsOpen;
        _canSelect = viewModel.CanSelect;

        var choices = viewModel.Choices ?? Array.Empty<LevelUpChoiceCardViewModel>();
        for (var i = 0; i < _choiceButtons.Count; i++)
        {
            if (i >= choices.Length)
            {
                _choiceButtons[i].Visible = false;
                _choiceIds[_choiceButtons[i]] = string.Empty;
                continue;
            }

            var choice = choices[i];
            _choiceButtons[i].Visible = true;
            _choiceTitles[i].Text = choice.Title ?? string.Empty;
            _choiceDescriptions[i].Text = choice.Description ?? string.Empty;
            _choiceFooters[i].Text = choice.Kind == LevelUpChoiceKind.StatBoost ? "Stat Boost" : "Skill Modifier";
            _choiceIds[_choiceButtons[i]] = choice.Id ?? string.Empty;
            UiStyles.HighlightSelection(_choiceButtons[i], i == viewModel.SelectedIndex);
        }
    }

    public void SetSoundPlayer(IUiSoundPlayer player)
    {
        _uiSoundPlayer = player;
    }

    private Button BuildChoiceCard(int index)
    {
        var button = new Button
        {
            Width = 260,
            Height = 220,
            Background = new SolidBrush(UiTheme.CardBackground),
            OverBackground = new SolidBrush(UiTheme.ButtonHover),
            PressedBackground = new SolidBrush(UiTheme.ButtonPressed),
            Border = new SolidBrush(UiTheme.CardBorder),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(UiTheme.Padding)
        };

        _choiceIds[button] = string.Empty;

        var content = new MenuColumn(UiTheme.Spacing)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var title = UiStyles.SectionTitle(string.Empty, 1.0f);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddRow(title);

        var description = UiStyles.BodyText(string.Empty, UiTheme.MutedText, wrap: true, scale: 0.95f);
        description.HorizontalAlignment = HorizontalAlignment.Stretch;
        content.AddRow(description, ProportionType.Fill);

        var footer = UiStyles.BodyText(string.Empty, UiTheme.PrimaryText, wrap: false, scale: 0.9f);
        footer.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddRow(footer);

        button.Content = content;
        button.Click += (_, _) =>
        {
            if (!_canSelect)
            {
                return;
            }

            if (_choiceIds.TryGetValue(button, out var id) && !string.IsNullOrWhiteSpace(id))
            {
                ChoicePicked?.Invoke(id);
            }
        };

        UiSoundBinder.BindHoverAndClick(button, _uiSoundPlayer);

        _choiceButtons.Add(button);
        _choiceTitles.Add(title);
        _choiceDescriptions.Add(description);
        _choiceFooters.Add(footer);

        return button;
    }
}
