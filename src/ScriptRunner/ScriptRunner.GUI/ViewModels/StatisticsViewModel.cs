using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;

namespace ScriptRunner.GUI.ViewModels;

public class StatisticsViewModel : ReactiveObject
{
    private readonly ObservableCollection<ExecutionLogAction> _executionLog;

    public StatisticsViewModel(ObservableCollection<ExecutionLogAction> executionLog)
    {
        _executionLog = executionLog;
        RefreshStatistics();
    }

    private List<HeatmapDay> _heatmapDays = new();
    public List<HeatmapDay> HeatmapDays
    {
        get => _heatmapDays;
        set => this.RaiseAndSetIfChanged(ref _heatmapDays, value);
    }

    private List<TopActionItem> _topActions = new();
    public List<TopActionItem> TopActions
    {
        get => _topActions;
        set => this.RaiseAndSetIfChanged(ref _topActions, value);
    }

    private List<string> _monthLabels = new();
    public List<string> MonthLabels
    {
        get => _monthLabels;
        set => this.RaiseAndSetIfChanged(ref _monthLabels, value);
    }

    private int _minWeek = 0;
    public int MinWeek
    {
        get => _minWeek;
        set => this.RaiseAndSetIfChanged(ref _minWeek, value);
    }

    private int _maxWeek = 0;
    public int MaxWeek
    {
        get => _maxWeek;
        set => this.RaiseAndSetIfChanged(ref _maxWeek, value);
    }

    public void RefreshStatistics()
    {
        var now = DateTime.Now;
        var yearAgo = now.Date.AddYears(-1);
        
        // Filter execution log for the last year
        var yearData = _executionLog
            .Where(x => x.Timestamp >= yearAgo)
            .ToList();

        // Generate heatmap data
        GenerateHeatmapData(yearData, yearAgo, now);
        
        // Generate top 10 actions
        GenerateTopActions(yearData);
    }

    private void GenerateHeatmapData(List<ExecutionLogAction> yearData, DateTime startDate, DateTime endDate)
    {
        // Group by date and count executions
        var executionsByDate = yearData
            .GroupBy(x => x.Timestamp.Date)
            .ToDictionary(x => x.Key, x => x.Count());

        var heatmapDays = new List<HeatmapDay>();
        var monthLabels = new List<string>();
        
        // Start from the first Sunday before or on the start date
        var currentDate = startDate;
        while (currentDate.DayOfWeek != DayOfWeek.Sunday)
        {
            currentDate = currentDate.AddDays(-1);
        }

        var startSunday = currentDate;
        string currentMonth = "";
        int currentMonthStartWeek = 0;
        
        while (currentDate <= endDate)
        {
            var count = executionsByDate.TryGetValue(currentDate, out var c) ? c : 0;
            var intensity = GetIntensityLevel(count);
            
            // Calculate week index based on days from start Sunday
            int daysSinceStart = (int)(currentDate - startSunday).TotalDays;
            int weekIndex = daysSinceStart / 7;
            
            heatmapDays.Add(new HeatmapDay
            {
                Date = currentDate,
                Count = count,
                Intensity = intensity,
                DayOfWeek = (int)currentDate.DayOfWeek,
                WeekIndex = weekIndex,
                ToolTip = $"{currentDate:MMM dd, yyyy}: {count} execution{(count != 1 ? "s" : "")} [Week {weekIndex}, Day {(int)currentDate.DayOfWeek}]"
            });

            // Track month changes for labels
            var monthName = currentDate.ToString("MMM");
            if (monthName != currentMonth)
            {
                // Only add the previous month if we had one
                if (!string.IsNullOrEmpty(currentMonth))
                {
                    int weeksInMonth = weekIndex - currentMonthStartWeek;
                    monthLabels.Add($"{currentMonth}|{weeksInMonth}");
                }
                currentMonth = monthName;
                currentMonthStartWeek = weekIndex;
            }
            
            currentDate = currentDate.AddDays(1);
        }
        
        // Add the final month
        if (!string.IsNullOrEmpty(currentMonth))
        {
            int daysSinceStart = (int)(endDate - startSunday).TotalDays;
            int finalWeekIndex = daysSinceStart / 7;
            int weeksInMonth = finalWeekIndex - currentMonthStartWeek + 1;
            monthLabels.Add($"{currentMonth}|{weeksInMonth}");
        }

        HeatmapDays = heatmapDays;
        MonthLabels = monthLabels;
        
        // Update min/max week for debugging
        if (heatmapDays.Count > 0)
        {
            MinWeek = heatmapDays.Min(x => x.WeekIndex);
            MaxWeek = heatmapDays.Max(x => x.WeekIndex);
        }
        else
        {
            MinWeek = 0;
            MaxWeek = 0;
        }
    }

    private int GetIntensityLevel(int count)
    {
        if (count == 0) return 0;
        if (count <= 2) return 1;
        if (count <= 5) return 2;
        if (count <= 10) return 3;
        return 4;
    }

    private void GenerateTopActions(List<ExecutionLogAction> yearData)
    {
        var topActions = yearData
            .GroupBy(x => new { x.Source, x.Name })
            .Select(g => new TopActionItem
            {
                ActionName = g.Key.Name,
                Source = g.Key.Source,
                ExecutionCount = g.Count()
            })
            .OrderByDescending(x => x.ExecutionCount)
            .Take(10)
            .Select((item, index) => 
            {
                item.Rank = (index + 1).ToString();
                return item;
            })
            .ToList();

        TopActions = topActions;
    }
}

public class HeatmapDay
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public int Intensity { get; set; }
    public int DayOfWeek { get; set; }
    public int WeekIndex { get; set; }
    public string ToolTip { get; set; } = "";
}

public class TopActionItem
{
    public string Rank { get; set; } = "";
    public string ActionName { get; set; } = "";
    public string Source { get; set; } = "";
    public int ExecutionCount { get; set; }
}
