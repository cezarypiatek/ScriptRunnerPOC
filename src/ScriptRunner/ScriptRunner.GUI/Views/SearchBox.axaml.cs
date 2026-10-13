using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ReactiveUI;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views
{
    public partial class SearchBox :  ReactiveUserControl<SearchBoxViewModel>
    {
        public SearchBox():this(
            Array.Empty<ScriptConfig>(),
            Array.Empty<RecentAction>(),
            Array.Empty<string>(),
            Array.Empty<ExecutionLogAction>())
        {
        }

        public SearchBox(
            IReadOnlyList<ScriptConfig> viewModelActions,
            IReadOnlyList<RecentAction> recentActions,
            IReadOnlyCollection<string>? favoriteActionKeys = null,
            IEnumerable<ExecutionLogAction>? executionLog = null)
        {
            InitializeComponent();
            ViewModel = new SearchBoxViewModel(
                viewModelActions,
                recentActions,
                favoriteActionKeys,
                executionLog);

            foreach (var filter in GetFilterControls())
            {
                filter.AddHandler(
                    InputElement.KeyDownEvent,
                    FilterButton_OnKeyDown,
                    RoutingStrategies.Tunnel,
                    handledEventsToo: true);
            }

            RecentExecutionLogList.AddHandler(
                InputElement.KeyDownEvent,
                RecentExecutionLogList_OnKeyDown,
                RoutingStrategies.Tunnel,
                handledEventsToo: true);
            FilteredItems.AddHandler(
                InputElement.KeyDownEvent,
                FilteredItems_OnKeyDown,
                RoutingStrategies.Tunnel,
                handledEventsToo: true);
            SearchBoxInput.AddHandler(
                InputElement.KeyDownEvent,
                SearchBoxInput_OnKeyDown,
                RoutingStrategies.Tunnel,
                handledEventsToo: true);
            RecentExecutionLogList.ItemInvoked += (_, args) => OnRecentResultSelected(args.Action);
            
            this.WhenActivated(disposables => 
            {
                this.SearchBoxInput.Focus();
            });

            this.FilteredItems.KeyUp += (sender, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    var autoLaunch = args.KeyModifiers.HasFlag(KeyModifiers.Control);
                    ChoseSelected(autoLaunch);
                }


            };

            this.ViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ViewModel.FilteredActionList))
                {
                    if (FilteredItems.ItemCount > 0)
                    {
                        FilteredItems.SelectedIndex = 0;
                    }
                }
                else if (args.PropertyName == nameof(ViewModel.FilteredExecutionLog))
                {
                    Dispatcher.UIThread.Post(() => RecentExecutionLogList.MoveSelection(1));
                }
            };
        }

        private void SearchBoxInput_OnKeyDown(object? sender, KeyEventArgs args)
        {
            if (args.Key == Key.Tab)
            {
                if (args.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    FocusResults();
                }
                else
                {
                    AllFilterButton.Focus(NavigationMethod.Tab);
                }

                args.Handled = true;
            }
            else if (args.Key == Key.Up)
            {
                if (ViewModel.IsRecentFilterSelected)
                {
                    RecentExecutionLogList.MoveSelection(-1);
                    return;
                }

                if (FilteredItems.ItemCount > 0)
                {
                    FilteredItems.SelectedIndex = FilteredItems.SelectedIndex <= 0
                        ? FilteredItems.ItemCount - 1
                        : FilteredItems.SelectedIndex - 1;
                }
            }
            else if (args.Key == Key.Down)
            {
                if (ViewModel.IsRecentFilterSelected)
                {
                    RecentExecutionLogList.MoveSelection(1);
                    return;
                }

                if (FilteredItems.ItemCount > 0)
                {
                    FilteredItems.SelectedIndex = FilteredItems.SelectedIndex == FilteredItems.ItemCount - 1
                        ? 0
                        : FilteredItems.SelectedIndex + 1;
                }
            }
            else if (args.Key == Key.Enter)
            {
                var autoLaunch = args.KeyModifiers.HasFlag(KeyModifiers.Control);
                ChoseSelected(autoLaunch);
            }
        }

        private void ChoseSelected(bool autoLaunch = false)
        {
            if (ViewModel?.IsRecentFilterSelected == true)
            {
                RecentExecutionLogList.InvokeSelectedItem();
                return;
            }

            if (FilteredItems.SelectedItem is ScriptConfigWithArgumentSet selectedConfig)
            {
                OnResultSelected(selectedConfig, autoLaunch);
            }
        }

        private void ClickOnItem(object? sender, PointerReleasedEventArgs e)
        {
            if (FilteredItems.SelectedItem is ScriptConfigWithArgumentSet selectedConfig)
            {
                var autoLaunch = e.KeyModifiers.HasFlag(KeyModifiers.Control);
                OnResultSelected(selectedConfig, autoLaunch);
            }
        }

        private void FilteredItems_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    FiltersButton.Focus(NavigationMethod.Tab);
                }
                else
                {
                    SearchBoxInput.Focus(NavigationMethod.Tab);
                }

                e.Handled = true;
            }
        }

        private void RecentExecutionLogList_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    FiltersButton.Focus(NavigationMethod.Tab);
                }
                else
                {
                    SearchBoxInput.Focus(NavigationMethod.Tab);
                }

                e.Handled = true;
            }
        }

        private void FilterButton_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (sender is not Control currentFilter)
            {
                return;
            }

            var filters = GetFilterControls();
            var currentIndex = Array.IndexOf(filters, currentFilter);
            if (e.Key == Key.Tab)
            {
                var targetIndex = currentIndex +
                    (e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1);
                if (targetIndex < 0)
                {
                    SearchBoxInput.Focus(NavigationMethod.Tab);
                }
                else if (targetIndex >= filters.Length)
                {
                    FocusResults();
                }
                else
                {
                    filters[targetIndex].Focus(NavigationMethod.Tab);
                }

                e.Handled = true;
                return;
            }

            if (e.Key is not (Key.Left or Key.Right))
            {
                return;
            }

            var directionalTargetIndex = currentIndex + (e.Key == Key.Right ? 1 : -1);

            if (directionalTargetIndex >= 0 && directionalTargetIndex < filters.Length)
            {
                filters[directionalTargetIndex].Focus(NavigationMethod.Directional);
            }

            e.Handled = true;
        }

        private void FocusResults()
        {
            FiltersButton.Flyout?.Hide();
            if (ViewModel.IsRecentFilterSelected)
            {
                RecentExecutionLogList.FocusList(NavigationMethod.Tab);
            }
            else if (FilteredItems.ItemCount > 0)
            {
                if (FilteredItems.SelectedIndex < 0)
                {
                    FilteredItems.SelectedIndex = 0;
                }

                FocusSelectedActionResult();
            }
            else
            {
                SearchBoxInput.Focus(NavigationMethod.Tab);
            }
        }

        private void FocusSelectedActionResult()
        {
            var selectedItem = FilteredItems.SelectedItem;
            if (selectedItem != null &&
                FilteredItems.ContainerFromItem(selectedItem) is Control container)
            {
                container.Focus(NavigationMethod.Tab);
                return;
            }

            if (selectedItem != null)
            {
                FilteredItems.ScrollIntoView(selectedItem);
                Dispatcher.UIThread.Post(() =>
                {
                    if (FilteredItems.ContainerFromItem(selectedItem) is Control realizedContainer)
                    {
                        realizedContainer.Focus(NavigationMethod.Tab);
                    }
                });
            }
        }

        private Control[] GetFilterControls() =>
        [
            AllFilterButton,
            FavoritesFilterButton,
            RecentFilterButton,
            FiltersButton
        ];

        private void FavoritesFilter_Click(object? sender, RoutedEventArgs e)
        {
            ToggleQuickFilter(MainWindowViewModel.FavoritesCategoryFilter);
        }

        private void AllFilter_Click(object? sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.SelectedCategoryFilter = MainWindowViewModel.AllCategoryFilter;
            }

            if (sender is ToggleButton toggleButton)
            {
                toggleButton.IsChecked = true;
            }
        }

        private void RecentFilter_Click(object? sender, RoutedEventArgs e)
        {
            ToggleQuickFilter(MainWindowViewModel.RecentCategoryFilter);
        }

        private void ToggleQuickFilter(string filter)
        {
            if (ViewModel == null)
            {
                return;
            }

            ViewModel.SelectedCategoryFilter = ViewModel.SelectedCategoryFilter == filter
                ? MainWindowViewModel.AllCategoryFilter
                : filter;
        }

        private void CategoryFilter_OnTapped(object? sender, RoutedEventArgs e)
        {
            if (sender is not Border { DataContext: CategoryFilterOption filter } || ViewModel == null)
            {
                return;
            }

            ViewModel.SelectedCategoryFilter = filter.Key;
            this.FindControl<Button>("FiltersButton")?.Flyout?.Hide();
        }

        private void ClearCategoryFilter_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.SelectedCategoryFilter = MainWindowViewModel.AllCategoryFilter;
            }

            this.FindControl<Button>("FiltersButton")?.Flyout?.Hide();
            e.Handled = true;
        }

        public event EventHandler<ResultSelectedEventArgs> ResultSelected;

        public class ResultSelectedEventArgs : EventArgs
        {
            public ScriptConfigWithArgumentSet? Result { get; set; }
            public ExecutionLogAction? RecentExecution { get; set; }
            public bool AutoLaunch { get; set; }
            public ResultSelectedEventArgs()
            {
            }
        }

        private void OnResultSelected(ScriptConfigWithArgumentSet? v, bool autoLaunch) => ResultSelected?.Invoke(this, new ResultSelectedEventArgs(){Result = v, AutoLaunch = autoLaunch});

        private void OnRecentResultSelected(ExecutionLogAction execution) =>
            ResultSelected?.Invoke(this, new ResultSelectedEventArgs { RecentExecution = execution });
    }
}
