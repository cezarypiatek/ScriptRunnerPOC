using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI.ViewModels
{
    public class SearchBoxViewModel : ReactiveObject
    {
        private readonly List<ScriptConfigWithArgumentSet> _allActions;
        private readonly IReadOnlyList<RecentAction> _recentActions;
        private readonly IReadOnlyList<ExecutionLogAction> _executionLog;
        private readonly List<CategoryFilterOption> _availableCategories;
        private readonly int _actionCount;
        private readonly int _presetCount;

        public string SearchFilter
        {
            get => _searchFilter;
            set => this.RaiseAndSetIfChanged(ref _searchFilter, value);
        }

        private string _searchFilter;

        public string SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                if (_selectedCategoryFilter == value)
                {
                    return;
                }

                this.RaiseAndSetIfChanged(ref _selectedCategoryFilter, value);
                UpdateCategoryFilterStates();
                this.RaisePropertyChanged(nameof(IsAllFilterSelected));
                this.RaisePropertyChanged(nameof(IsFavoritesFilterSelected));
                this.RaisePropertyChanged(nameof(IsRecentFilterSelected));
                this.RaisePropertyChanged(nameof(IsCategoryFilterSelected));
                this.RaisePropertyChanged(nameof(CategoryFilterButtonText));
            }
        }

        private string _selectedCategoryFilter = MainWindowViewModel.AllCategoryFilter;

        public bool IsAllFilterSelected =>
            SelectedCategoryFilter == MainWindowViewModel.AllCategoryFilter;

        public bool IsFavoritesFilterSelected =>
            SelectedCategoryFilter == MainWindowViewModel.FavoritesCategoryFilter;

        public bool IsRecentFilterSelected =>
            SelectedCategoryFilter == MainWindowViewModel.RecentCategoryFilter;

        public bool IsCategoryFilterSelected =>
            SelectedCategoryFilter != MainWindowViewModel.AllCategoryFilter &&
            SelectedCategoryFilter != MainWindowViewModel.FavoritesCategoryFilter &&
            SelectedCategoryFilter != MainWindowViewModel.RecentCategoryFilter;

        public string CategoryFilterButtonText =>
            IsCategoryFilterSelected ? SelectedCategoryFilter : "Filters";

        public IEnumerable<CategoryFilterOption> AvailableCategories => _availableCategories;

        private readonly ObservableAsPropertyHelper<IEnumerable<ScriptConfigWithArgumentSet>> _filteredActionList;
        public IEnumerable<ScriptConfigWithArgumentSet> FilteredActionList => _filteredActionList.Value;

        private readonly ObservableAsPropertyHelper<IEnumerable<ExecutionLogAction>> _filteredExecutionLog;
        public IEnumerable<ExecutionLogAction> FilteredExecutionLog => _filteredExecutionLog.Value;

        private readonly ObservableAsPropertyHelper<string> _actionCountText;
        public string ActionCountText => _actionCountText.Value;

        private readonly ObservableAsPropertyHelper<string> _presetCountText;
        public string PresetCountText => _presetCountText.Value;


        public SearchBoxViewModel(
            IReadOnlyList<ScriptConfig> allActions,
            IReadOnlyList<RecentAction> recent,
            IReadOnlyCollection<string>? favoriteActionKeys = null,
            IEnumerable<ExecutionLogAction>? executionLog = null)
        {
            _recentActions = recent.OrderByDescending(action => action.Timestamp).ToArray();
            _executionLog = executionLog?.ToArray() ?? Array.Empty<ExecutionLogAction>();
            _actionCount = allActions.Count;
            _presetCount = allActions.Sum(action =>
                action.PredefinedArgumentSets?.Count(argumentSet => argumentSet.Description != "<default>") ?? 0);
            _allActions = allActions.SelectMany(x=> x.PredefinedArgumentSets.Select(p=> new ScriptConfigWithArgumentSet()
            {
                Config = x,
                ArgumentSet = p,
                FullName = p.Description == "<default>"? x.FullName : $"{x.FullName} - {p.Description}",
                ActionName =  p.Description == "<default>"? x.Name : $"{x.Name} - {p.Description}",
                SourceName = x.SourceName,
                IconColor = p.Description == "<default>" ? "#3baced" : "#ff8c00",
                IsFavorite = favoriteActionKeys?.Contains(x.FullName) == true
            })).ToList();

            _availableCategories = allActions
                .SelectMany(action => action.Categories ?? Enumerable.Empty<string>())
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Distinct()
                .OrderBy(category => category)
                .Select(category => new CategoryFilterOption(category, category, false))
                .ToList();
            if (allActions.Any(action => action.Categories == null || action.Categories.Count == 0))
            {
                _availableCategories.Add(new CategoryFilterOption("(No Category)", "(No Category)", false));
            }

            UpdateCategoryFilterStates();

            this.WhenAnyValue(x => x.SearchFilter, x => x.SelectedCategoryFilter)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .DistinctUntilChanged()
                .Select(tuple =>
                {
                    var (text, categoryFilter) = tuple;
                    text = text?.Trim() ?? string.Empty;

                    var filteredActions = ApplyCategoryFilter(categoryFilter);
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return filteredActions;
                    }

                    var tokens = text.ToUpperInvariant()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    return filteredActions.Select(action =>
                        {
                            var fullName = action.FullName.ToUpperInvariant();
                            var score = tokens.Sum(token => fullName.Contains(token) ? token.Length : -1000);
                            return (score, action);
                        })
                        .Where(result => result.score > 0)
                        .OrderByDescending(result => result.score)
                        .Select(result => result.action);
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.FilteredActionList, out _filteredActionList);

            this.WhenAnyValue(x => x.SearchFilter, x => x.SelectedCategoryFilter)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .DistinctUntilChanged()
                .Select(tuple =>
                {
                    var (text, categoryFilter) = tuple;
                    if (categoryFilter != MainWindowViewModel.RecentCategoryFilter)
                    {
                        return Enumerable.Empty<ExecutionLogAction>();
                    }

                    var tokens = (text ?? string.Empty)
                        .Trim()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (tokens.Length == 0)
                    {
                        return _executionLog;
                    }

                    return _executionLog.Where(entry =>
                    {
                        var searchableText = string.Join(' ',
                            entry.Source,
                            entry.Name,
                            string.Join(' ', entry.Parameters.Select(parameter =>
                                $"{parameter.Key} {parameter.Value}")));
                        return tokens.All(token =>
                            searchableText.Contains(token, StringComparison.InvariantCultureIgnoreCase));
                    });
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.FilteredExecutionLog, out _filteredExecutionLog);

            this.WhenAnyValue(x => x.FilteredActionList, x => x.SearchFilter, x => x.SelectedCategoryFilter)
                .Select(tuple =>
                {
                    var (filteredActions, textFilter, categoryFilter) = tuple;
                    var filteredCount = filteredActions?
                        .Select(action => action.Config)
                        .Distinct()
                        .Count() ?? 0;
                    return IsFiltering(textFilter, categoryFilter)
                        ? $"Actions: {filteredCount}/{_actionCount}"
                        : $"Actions: {_actionCount}";
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.ActionCountText, out _actionCountText);

            this.WhenAnyValue(x => x.FilteredActionList, x => x.SearchFilter, x => x.SelectedCategoryFilter)
                .Select(tuple =>
                {
                    var (filteredActions, textFilter, categoryFilter) = tuple;
                    var filteredCount = filteredActions?
                        .Select(action => action.Config)
                        .Distinct()
                        .Sum(action => action.PredefinedArgumentSets?
                            .Count(argumentSet => argumentSet.Description != "<default>") ?? 0) ?? 0;
                    return IsFiltering(textFilter, categoryFilter)
                        ? $"Presets: {filteredCount}/{_presetCount}"
                        : $"Presets: {_presetCount}";
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.PresetCountText, out _presetCountText);
        }

        private static bool IsFiltering(string? textFilter, string categoryFilter) =>
            !string.IsNullOrWhiteSpace(textFilter) ||
            categoryFilter != MainWindowViewModel.AllCategoryFilter;

        private IEnumerable<ScriptConfigWithArgumentSet> ApplyCategoryFilter(string categoryFilter)
        {
            if (categoryFilter == MainWindowViewModel.FavoritesCategoryFilter)
            {
                return _allActions.Where(action => action.IsFavorite);
            }

            if (categoryFilter == MainWindowViewModel.RecentCategoryFilter)
            {
                return GetRecentActions(_allActions, _recentActions);
            }

            if (categoryFilter == "(No Category)")
            {
                return _allActions.Where(action => action.Config.Categories is not { Count: > 0 });
            }

            if (categoryFilter != MainWindowViewModel.AllCategoryFilter)
            {
                return _allActions.Where(action => action.Config.Categories?.Contains(categoryFilter) == true);
            }

            return _allActions;
        }

        private static IEnumerable<ScriptConfigWithArgumentSet> GetRecentActions(
            IReadOnlyList<ScriptConfigWithArgumentSet> allActions,
            IEnumerable<RecentAction> recent)
        {
            foreach (var recentAction in recent)
            {
                foreach (var sc in allActions)
                {
                    if (sc.Config.SourceName == recentAction.ActionId.SourceName &&
                        sc.Config.Name == recentAction.ActionId.ActionName &&
                        sc.ArgumentSet.Description == recentAction.ActionId.ParameterSet)
                    {
                        yield return sc;
                    }
                }
            }
        }

        private void UpdateCategoryFilterStates()
        {
            foreach (var category in _availableCategories)
            {
                category.UpdateState(SelectedCategoryFilter);
            }
        }
    }

    public class ScriptConfigWithArgumentSet
    {
        public ScriptConfig Config { get; set; }
        public ArgumentSet ArgumentSet{ get; set; }
        public string FullName { get; set; }
        public string ActionName { get; set; }
        public string SourceName { get; set; }
        public string IconColor { get; set; }
        public string IconName => ArgumentSet?.Description == "<default>" ? "fa-scroll" : "fa-list-ul";
        public bool HasCategories => Config.Categories is { Count: > 0 };
        public bool IsFavorite { get; set; }
    }
}
