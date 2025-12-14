using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using TheLastMageStanding.Game.Core.MetaProgression;
using TheLastMageStanding.Game.Core.UI.Myra;

namespace TheLastMageStanding.Game.Core.UI;

/// <summary>
/// Screen for viewing run history and statistics.
/// </summary>
internal sealed class MyraRunHistoryScreen : MyraMenuScreenBase
{
    private readonly RunHistoryService _runHistoryService;
    private readonly Action _onClose;
    
    private Grid _mainGrid = null!;
    private VerticalStackPanel _runListPanel = null!;
    private Label _detailsTitle = null!;
    private ScrollViewer _detailsScroll = null!;
    private VerticalStackPanel _detailsContent = null!;

    public MyraRunHistoryScreen(RunHistoryService runHistoryService, Action onClose)
    {
        _runHistoryService = runHistoryService;
        _onClose = onClose;

        BuildUI();
        LoadData();
    }

    private void BuildUI()
    {
        var root = new Grid
        {
            RowSpacing = UiTheme.Spacing,
            ColumnSpacing = UiTheme.Spacing,
            Padding = new Thickness(UiTheme.LargePadding),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        // Header Row (Title + Back Button)
        root.RowsProportions.Add(new Proportion(ProportionType.Auto));
        // Content Row
        root.RowsProportions.Add(new Proportion(ProportionType.Fill));

        // Header
        var headerGrid = new Grid
        {
            ColumnSpacing = UiTheme.Spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var title = UiStyles.Heading("Run History", 1.5f);
        headerGrid.Widgets.Add(title);

        var backButton = new MenuButton("Back", false, 120, 40);
        backButton.Click += (s, e) => _onClose();
        Grid.SetColumn(backButton, 1);
        headerGrid.Widgets.Add(backButton);

        root.Widgets.Add(headerGrid);

        // Content Grid (Split View)
        _mainGrid = new Grid
        {
            ColumnSpacing = UiTheme.LargeSpacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _mainGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 40)); // List
        _mainGrid.ColumnsProportions.Add(new Proportion(ProportionType.Part, 60)); // Details

        Grid.SetRow(_mainGrid, 1);
        root.Widgets.Add(_mainGrid);

        // Left Side: Run List
        var listContainer = new Panel
        {
            Background = new SolidBrush(UiTheme.PanelBackground),
            Border = new SolidBrush(UiTheme.PanelBorder),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(UiTheme.Spacing)
        };
        
        var listScroll = new ScrollViewer
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _runListPanel = new VerticalStackPanel
        {
            Spacing = UiTheme.Spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        listScroll.Content = _runListPanel;
        listContainer.Widgets.Add(listScroll);
        _mainGrid.Widgets.Add(listContainer);

        // Right Side: Details / Stats
        var detailsContainer = new Panel
        {
            Background = new SolidBrush(UiTheme.PanelBackground),
            Border = new SolidBrush(UiTheme.PanelBorder),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(UiTheme.LargePadding)
        };
        Grid.SetColumn(detailsContainer, 1);

        var detailsLayout = new Grid
        {
            RowSpacing = UiTheme.Spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        detailsLayout.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Title
        detailsLayout.RowsProportions.Add(new Proportion(ProportionType.Fill)); // Content

        _detailsTitle = UiStyles.SectionTitle("Overview", 1.2f);
        detailsLayout.Widgets.Add(_detailsTitle);

        _detailsScroll = new ScrollViewer
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetRow(_detailsScroll, 1);

        _detailsContent = new VerticalStackPanel
        {
            Spacing = UiTheme.Spacing,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _detailsScroll.Content = _detailsContent;
        
        detailsLayout.Widgets.Add(_detailsScroll);
        detailsContainer.Widgets.Add(detailsLayout);
        _mainGrid.Widgets.Add(detailsContainer);

        Desktop.Root = root;
    }

    private void LoadData()
    {
        // Load runs
        var runs = _runHistoryService.GetRecentRuns(50);
        
        _runListPanel.Widgets.Clear();

        if (runs.Count == 0)
        {
            _runListPanel.Widgets.Add(UiStyles.BodyText("No runs recorded yet.", UiTheme.MutedText));
            ShowAggregateStats(); // Show empty stats
            return;
        }

        foreach (var run in runs)
        {
            var item = new RunHistoryItem(run);
            item.Click += (s, e) => ShowRunDetails(run);
            _runListPanel.Widgets.Add(item);
        }

        // Default to showing aggregate stats
        ShowAggregateStats();
    }

    private void ShowRunDetails(RunSession run)
    {
        _detailsTitle.Text = $"Run Details - {run.StartTime.ToString("g", CultureInfo.InvariantCulture)}";
        _detailsContent.Widgets.Clear();

        // Check for records
        var bestWaveRun = _runHistoryService.GetBestRunByWave();
        var bestKillsRun = _runHistoryService.GetBestRunByKills();
        var bestGoldRun = _runHistoryService.GetBestRunByGold();

        bool isBestWave = bestWaveRun != null && bestWaveRun.RunId == run.RunId;
        bool isBestKills = bestKillsRun != null && bestKillsRun.RunId == run.RunId;
        bool isBestGold = bestGoldRun != null && bestGoldRun.RunId == run.RunId;

        if (isBestWave || isBestKills || isBestGold)
        {
             _detailsContent.Widgets.Add(new StatSectionHeader("Records Set!"));
             if (isBestWave) _detailsContent.Widgets.Add(UiStyles.BodyText("★ New Best Wave!", UiTheme.AccentText));
             if (isBestKills) _detailsContent.Widgets.Add(UiStyles.BodyText("★ New Kill Record!", UiTheme.AccentText));
             if (isBestGold) _detailsContent.Widgets.Add(UiStyles.BodyText("★ New Gold Record!", UiTheme.AccentText));
        }

        // Summary Section
        _detailsContent.Widgets.Add(new StatSectionHeader("Summary"));
        _detailsContent.Widgets.Add(new StatRow("Wave Reached", run.WaveReached.ToString(CultureInfo.InvariantCulture)));
        _detailsContent.Widgets.Add(new StatRow("Duration", run.Duration.ToString(@"mm\:ss", CultureInfo.InvariantCulture)));
        _detailsContent.Widgets.Add(new StatRow("Outcome", run.CauseOfDeath == "Victory" ? "Victory" : "Defeat", 
            run.CauseOfDeath == "Victory" ? UiTheme.SuccessText : UiTheme.ErrorText));
        _detailsContent.Widgets.Add(new StatRow("Cause of Death", string.IsNullOrEmpty(run.CauseOfDeath) ? "-" : run.CauseOfDeath));

        // Combat Stats
        _detailsContent.Widgets.Add(new StatSectionHeader("Combat"));
        _detailsContent.Widgets.Add(new StatRow("Total Kills", run.TotalKills.ToString(CultureInfo.InvariantCulture)));
        _detailsContent.Widgets.Add(new StatRow("Damage Dealt", run.TotalDamageDealt.ToString("N0", CultureInfo.InvariantCulture)));
        _detailsContent.Widgets.Add(new StatRow("Damage Taken", run.TotalDamageTaken.ToString("N0", CultureInfo.InvariantCulture)));

        // Economy & Meta
        _detailsContent.Widgets.Add(new StatSectionHeader("Economy & Progression"));
        _detailsContent.Widgets.Add(new StatRow("Gold Collected", run.GoldCollected.ToString(CultureInfo.InvariantCulture), UiTheme.AccentText));
        _detailsContent.Widgets.Add(new StatRow("Meta XP Earned", run.MetaXpEarned.ToString(CultureInfo.InvariantCulture), UiTheme.InfoText));
        _detailsContent.Widgets.Add(new StatRow("Final Level", run.FinalLevel.ToString(CultureInfo.InvariantCulture)));

        // Equipment (if any)
        if (run.EquipmentFound.Count > 0)
        {
            _detailsContent.Widgets.Add(new StatSectionHeader("Equipment Found"));
            foreach (var item in run.EquipmentFound)
            {
                _detailsContent.Widgets.Add(UiStyles.BodyText($"• {item.Name}", UiTheme.MutedText));
            }
        }
    }

    private void ShowAggregateStats()
    {
        _detailsTitle.Text = "Career Overview";
        _detailsContent.Widgets.Clear();

        var allRuns = _runHistoryService.GetAllRuns();
        if (allRuns.Count == 0)
        {
            _detailsContent.Widgets.Add(UiStyles.BodyText("No runs recorded yet. Go play!", UiTheme.MutedText));
            return;
        }

        // Calculate aggregates
        int totalRuns = allRuns.Count;
        int totalKills = allRuns.Sum(r => r.TotalKills);
        long totalGold = allRuns.Sum(r => (long)r.GoldCollected);
        TimeSpan totalTime = TimeSpan.FromTicks(allRuns.Sum(r => r.Duration.Ticks));
        double avgWave = allRuns.Average(r => r.WaveReached);
        
        // Personal Records
        var bestWaveRun = _runHistoryService.GetBestRunByWave();
        var bestKillsRun = _runHistoryService.GetBestRunByKills();
        var bestGoldRun = _runHistoryService.GetBestRunByGold();

        // Display Personal Records
        _detailsContent.Widgets.Add(new StatSectionHeader("Personal Records"));
        
        if (bestWaveRun != null)
        {
            var row = new StatRow("Best Wave", bestWaveRun.WaveReached.ToString(CultureInfo.InvariantCulture), UiTheme.AccentText);
            // Add a small date label or button to jump to run? For now just text.
            _detailsContent.Widgets.Add(row);
        }

        if (bestKillsRun != null)
        {
            _detailsContent.Widgets.Add(new StatRow("Most Kills", bestKillsRun.TotalKills.ToString(CultureInfo.InvariantCulture), UiTheme.AccentText));
        }

        if (bestGoldRun != null)
        {
            _detailsContent.Widgets.Add(new StatRow("Most Gold", bestGoldRun.GoldCollected.ToString(CultureInfo.InvariantCulture), UiTheme.AccentText));
        }

        // Display Aggregate Stats
        _detailsContent.Widgets.Add(new StatSectionHeader("Lifetime Stats"));
        _detailsContent.Widgets.Add(new StatRow("Total Runs", totalRuns.ToString(CultureInfo.InvariantCulture)));
        _detailsContent.Widgets.Add(new StatRow("Total Kills", totalKills.ToString("N0", CultureInfo.InvariantCulture)));
        _detailsContent.Widgets.Add(new StatRow("Total Gold", totalGold.ToString("N0", CultureInfo.InvariantCulture)));
        _detailsContent.Widgets.Add(new StatRow("Total Playtime", $"{(int)totalTime.TotalHours}h {totalTime.Minutes}m"));
        _detailsContent.Widgets.Add(new StatRow("Average Wave", avgWave.ToString("F1", CultureInfo.InvariantCulture)));
    }
}
