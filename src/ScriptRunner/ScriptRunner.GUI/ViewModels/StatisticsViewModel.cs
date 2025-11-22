using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;

namespace ScriptRunner.GUI.ViewModels;

public class StatisticsViewModel : ReactiveObject
{
    private readonly ObservableCollection<ExecutionLogAction> _executionLog;
    private List<TopActionItem> _allActions = new();
    private const int PageSize = 10;

    public StatisticsViewModel(ObservableCollection<ExecutionLogAction> executionLog)
    {
        _executionLog = executionLog;
        
        // Initialize commands
        NextPageCommand = ReactiveCommand.Create(NextPage, this.WhenAnyValue(x => x.CanGoNext));
        PreviousPageCommand = ReactiveCommand.Create(PreviousPage, this.WhenAnyValue(x => x.CanGoPrevious));
        GoToPageCommand = ReactiveCommand.Create<int>(GoToPage);
        
        // Initialize available years
        InitializeAvailableYears();
        
        // Listen to changes in the execution log and refresh year options
        _executionLog.CollectionChanged += (sender, args) =>
        {
            InitializeAvailableYears();
        };
        
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

    private List<int> _availableYears = new();
    public List<int> AvailableYears
    {
        get => _availableYears;
        set => this.RaiseAndSetIfChanged(ref _availableYears, value);
    }

    private List<YearOption> _yearOptions = new();
    public List<YearOption> YearOptions
    {
        get => _yearOptions;
        set => this.RaiseAndSetIfChanged(ref _yearOptions, value);
    }

    private YearOption? _selectedYearOption;
    public YearOption? SelectedYearOption
    {
        get => _selectedYearOption;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedYearOption, value);
            if (value != null)
            {
                RefreshHeatmapForSelectedOption();
            }
        }
    }

    private int _selectedYear;
    public int SelectedYear
    {
        get => _selectedYear;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedYear, value);
        }
    }

    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            this.RaiseAndSetIfChanged(ref _currentPage, value);
            this.RaisePropertyChanged(nameof(CanGoNext));
            this.RaisePropertyChanged(nameof(CanGoPrevious));
            this.RaisePropertyChanged(nameof(PageInfo));
            UpdatePagedActions();
        }
    }

    private int _totalPages = 1;
    public int TotalPages
    {
        get => _totalPages;
        set
        {
            this.RaiseAndSetIfChanged(ref _totalPages, value);
            this.RaisePropertyChanged(nameof(CanGoNext));
            this.RaisePropertyChanged(nameof(PageInfo));
        }
    }

    private int _totalActions = 0;
    public int TotalActions
    {
        get => _totalActions;
        set
        {
            this.RaiseAndSetIfChanged(ref _totalActions, value);
            this.RaisePropertyChanged(nameof(PageInfo));
        }
    }

    public bool CanGoNext => CurrentPage < TotalPages;
    public bool CanGoPrevious => CurrentPage > 1;
    
    public string PageInfo => TotalActions > 0 
        ? $"Page {CurrentPage} of {TotalPages} ({TotalActions} total actions)"
        : "No actions found";

    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand GoToPageCommand { get; }

    private void NextPage()
    {
        if (CanGoNext)
        {
            CurrentPage++;
        }
    }

    private void PreviousPage()
    {
        if (CanGoPrevious)
        {
            CurrentPage--;
        }
    }

    private void GoToPage(int pageNumber)
    {
        if (pageNumber >= 1 && pageNumber <= TotalPages)
        {
            CurrentPage = pageNumber;
        }
    }

    private void InitializeAvailableYears()
    {
        var yearOptions = new List<YearOption>();
        
        // Add "Last Year" option first
        yearOptions.Add(new YearOption { DisplayName = "Last Year", IsLastYear = true, Year = null });

        System.Diagnostics.Debug.WriteLine($"=== InitializeAvailableYears called ===");
        System.Diagnostics.Debug.WriteLine($"Execution log count: {_executionLog.Count}");
        
        if (_executionLog.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("Execution log is empty!");
            _selectedYearOption = yearOptions[0];
            YearOptions = yearOptions;
            return;
        }

        // Print first and last few entries for debugging
        var sortedLog = _executionLog.OrderBy(x => x.Timestamp).ToList();
        System.Diagnostics.Debug.WriteLine($"First execution: {sortedLog.First().Timestamp:yyyy-MM-dd}");
        System.Diagnostics.Debug.WriteLine($"Last execution: {sortedLog.Last().Timestamp:yyyy-MM-dd}");

        // Get all years from execution log
        var years = _executionLog
            .Select(x => x.Timestamp.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();

        // Debug: Print all years found
        System.Diagnostics.Debug.WriteLine($"Found {years.Count} unique years in execution log: {string.Join(", ", years)}");
        
        // Add individual years (exclude current year since "Last Year" covers it)
        var currentYear = DateTime.Now.Year;
        System.Diagnostics.Debug.WriteLine($"Current year: {currentYear}");
        
        foreach (var year in years)
        {
            // Only add years that are NOT the current year
            if (year < currentYear)
            {
                System.Diagnostics.Debug.WriteLine($"Adding year option: {year}");
                yearOptions.Add(new YearOption { DisplayName = year.ToString(), IsLastYear = false, Year = year });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Skipping year: {year} (current or future year)");
            }
        }

        System.Diagnostics.Debug.WriteLine($"Total year options: {yearOptions.Count}");
        System.Diagnostics.Debug.WriteLine($"=== End InitializeAvailableYears ===");
        
        YearOptions = yearOptions;
        
        // Only set selected year option if it's not already set
        if (_selectedYearOption == null || !yearOptions.Contains(_selectedYearOption))
        {
            _selectedYearOption = yearOptions[0]; // Select "Last Year" by default
        }
    }

    public void RefreshStatistics()
    {
        var now = DateTime.Now;
        var yearAgo = now.Date.AddYears(-1);
        
        // Filter execution log for the last year
        var yearData = _executionLog
            .Where(x => x.Timestamp >= yearAgo)
            .ToList();

        // Generate heatmap data for selected year
        RefreshHeatmapForSelectedOption();
        
        // Generate top actions
        GenerateTopActions(yearData);
    }

    private void RefreshHeatmapForSelectedOption()
    {
        if (_selectedYearOption == null)
            return;

        DateTime startDate;
        DateTime endDate;

        if (_selectedYearOption.IsLastYear)
        {
            // Last year: from today going back 365 days
            endDate = DateTime.Now.Date;
            startDate = endDate.AddYears(-1).AddDays(1); // Start from 364 days ago
        }
        else if (_selectedYearOption.Year.HasValue)
        {
            // Specific year: January 1 to December 31
            startDate = new DateTime(_selectedYearOption.Year.Value, 1, 1);
            endDate = new DateTime(_selectedYearOption.Year.Value, 12, 31);
        }
        else
        {
            return;
        }
        
        var yearData = _executionLog
            .Where(x => x.Timestamp >= startDate && x.Timestamp <= endDate)
            .ToList();

        GenerateHeatmapData(yearData, startDate, endDate, _selectedYearOption.IsLastYear);
    }

    private void RefreshHeatmapForYear()
    {
        // Get data for the selected year
        var startDate = new DateTime(SelectedYear, 1, 1);
        var endDate = new DateTime(SelectedYear, 12, 31);
        
        var yearData = _executionLog
            .Where(x => x.Timestamp >= startDate && x.Timestamp <= endDate)
            .ToList();

        GenerateHeatmapData(yearData, startDate, endDate, false);
    }

    private void GenerateHeatmapData(List<ExecutionLogAction> yearData, DateTime startDate, DateTime endDate, bool isLastYear)
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
        
        // Continue until we reach a Saturday after the end date
        var actualEndDate = endDate;
        while (actualEndDate.DayOfWeek != DayOfWeek.Saturday)
        {
            actualEndDate = actualEndDate.AddDays(1);
        }
        
        while (currentDate <= actualEndDate)
        {
            // Only count executions if date is within the actual range
            var count = 0;
            var isInRange = currentDate >= startDate && currentDate <= endDate;
            
            if (isInRange)
            {
                count = executionsByDate.TryGetValue(currentDate, out var c) ? c : 0;
            }
            
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
                ToolTip = $"{currentDate:MMM dd, yyyy}: {count} execution{(count != 1 ? "s" : "")}",
                IsOutOfRange = !isInRange
            });

            // Track month changes for labels (only for months in the actual range)
            if (isInRange)
            {
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
            }
            
            currentDate = currentDate.AddDays(1);
        }
        
        // Add the final month
        if (!string.IsNullOrEmpty(currentMonth))
        {
            int daysSinceStart = (int)(actualEndDate - startSunday).TotalDays;
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
        _allActions = yearData
            .GroupBy(x => new { x.Source, x.Name })
            .Select(g => new TopActionItem
            {
                ActionName = g.Key.Name,
                Source = g.Key.Source,
                ExecutionCount = g.Count(),
                LastUsed = g.Max(x => x.Timestamp)
            })
            .OrderByDescending(x => x.ExecutionCount)
            .Select((item, index) => 
            {
                item.Rank = (index + 1).ToString();
                return item;
            })
            .ToList();

        TotalActions = _allActions.Count;
        TotalPages = TotalActions > 0 ? (int)Math.Ceiling((double)TotalActions / PageSize) : 1;
        CurrentPage = 1;
        UpdatePagedActions();
    }

    private void UpdatePagedActions()
    {
        var skip = (CurrentPage - 1) * PageSize;
        TopActions = _allActions.Skip(skip).Take(PageSize).ToList();
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
    public bool IsOutOfRange { get; set; }
}

public class YearOption
{
    public string DisplayName { get; set; } = "";
    public bool IsLastYear { get; set; }
    public int? Year { get; set; }
}

public class TopActionItem
{
    public string Rank { get; set; } = "";
    public string ActionName { get; set; } = "";
    public string Source { get; set; } = "";
    public int ExecutionCount { get; set; }
    public DateTime LastUsed { get; set; }
    public string LastUsedFormatted => LastUsed.ToString("yyyy-MM-dd");
}
