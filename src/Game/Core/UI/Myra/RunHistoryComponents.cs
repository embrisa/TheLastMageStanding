using System.Globalization;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using TheLastMageStanding.Game.Core.MetaProgression;

namespace TheLastMageStanding.Game.Core.UI.Myra;

/// <summary>
/// Represents a single row in the run history list.
/// </summary>
internal sealed class RunHistoryItem : Button
{
    public RunSession Run { get; }

    public RunHistoryItem(RunSession run, bool isSelected = false)
    {
        Run = run;
        
        // Basic styling
        Background = new SolidBrush(isSelected ? UiTheme.ButtonHover : UiTheme.CardBackground);
        Border = new SolidBrush(UiTheme.CardBorder);
        BorderThickness = new Thickness(1);
        Padding = new Thickness(UiTheme.Spacing);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Height = 60;

        var grid = new Grid
        {
            ColumnSpacing = UiTheme.Spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Columns: Date, Wave, Duration, Outcome
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 2)); // Date
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1)); // Wave
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1)); // Duration
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 1)); // Outcome

        // Date
        var dateLabel = UiStyles.BodyText(run.StartTime.ToString("g", CultureInfo.InvariantCulture), UiTheme.MutedText);
        grid.Widgets.Add(dateLabel);

        // Wave
        var waveLabel = UiStyles.BodyText($"Wave {run.WaveReached}", UiTheme.PrimaryText);
        Grid.SetColumn(waveLabel, 1);
        grid.Widgets.Add(waveLabel);

        // Duration
        var durationLabel = UiStyles.BodyText(run.Duration.ToString(@"mm\:ss", CultureInfo.InvariantCulture), UiTheme.MutedText);
        Grid.SetColumn(durationLabel, 2);
        grid.Widgets.Add(durationLabel);

        // Outcome
        bool isVictory = run.CauseOfDeath == "Victory"; // Assuming "Victory" is the string for winning
        var outcomeText = isVictory ? "Victory" : "Defeat";
        var outcomeColor = isVictory ? UiTheme.SuccessText : UiTheme.ErrorText;
        var outcomeLabel = UiStyles.BodyText(outcomeText, outcomeColor);
        outcomeLabel.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(outcomeLabel, 3);
        grid.Widgets.Add(outcomeLabel);

        Content = grid;
    }
}

/// <summary>
/// A row displaying a stat name and value.
/// </summary>
internal sealed class StatRow : Grid
{
    public StatRow(string name, string value, Color? valueColor = null)
    {
        ColumnSpacing = UiTheme.Spacing;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        
        ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var nameLabel = UiStyles.BodyText(name, UiTheme.MutedText);
        Widgets.Add(nameLabel);

        var valueLabel = UiStyles.BodyText(value, valueColor ?? UiTheme.AccentText);
        valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(valueLabel, 1);
        Widgets.Add(valueLabel);
    }
}

/// <summary>
/// A section header for stats.
/// </summary>
internal sealed class StatSectionHeader : Label
{
    public StatSectionHeader(string title)
    {
        Text = title;
        TextColor = UiTheme.PrimaryText;
        Font = UiFonts.Heading; // Assuming HeadingFont is available or use UiStyles
        // UiStyles.Heading returns a Label, but we are inheriting. 
        // Let's use UiStyles helper instead of inheritance if possible, or just set properties.
        // Actually, let's just use UiStyles.SectionTitle in the parent container.
        // But if we want a component:
        UiFonts.ApplyHeading(this, 0.9f);
        Margin = new Thickness(0, UiTheme.Spacing, 0, UiTheme.Spacing / 2);
    }
}

/// <summary>
/// Badge for personal records.
/// </summary>
internal sealed class PersonalRecordBadge : Label
{
    public PersonalRecordBadge(string text)
    {
        Text = text;
        TextColor = UiTheme.AccentText;
        UiFonts.ApplyBody(this, 0.8f);
        Background = new SolidBrush(new Color(UiTheme.AccentText.R, UiTheme.AccentText.G, UiTheme.AccentText.B, (byte)40));
        Padding = new Thickness(4, 2);
        Border = new SolidBrush(UiTheme.AccentText);
        BorderThickness = new Thickness(1);
    }
}
