using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views;

public partial class ActionsList : UserControl
{
    private Border? _previouslySelectedBorder;
    private bool _isInternalSelection;
    private readonly List<Border> _categoryBadges = new();

    public ActionsList()
    {
        InitializeComponent();
        this.DataContextChanged += OnDataContextChanged;
        this.Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Give the UI time to render, then find all category badges
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            FindAndTrackAllCategoryBadges();
        }, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.SelectedAction))
                {
                    // Only clear if this is an external selection change (not from our click handler)
                    if (!_isInternalSelection)
                    {
                        if (_previouslySelectedBorder != null)
                        {
                            _previouslySelectedBorder.Classes.Remove("selected");
                            _previouslySelectedBorder = null;
                        }
                    }
                }
                else if (args.PropertyName == nameof(MainWindowViewModel.SelectedCategoryFilter))
                {
                    // Update grayed state of all category badges when selection changes
                    UpdateCategoryBadgesGrayedState();
                }
                else if (args.PropertyName == nameof(MainWindowViewModel.AvailableCategories))
                {
                    // When categories change, re-scan for badges
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        _categoryBadges.Clear();
                        FindAndTrackAllCategoryBadges();
                        UpdateCategoryBadgesGrayedState();
                    }, Avalonia.Threading.DispatcherPriority.Loaded);
                }
            };
        }
    }

    private void ActionTile_OnTapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is TaggedScriptConfig taggedScriptConfig)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                // Set flag to prevent the PropertyChanged handler from clearing our selection
                _isInternalSelection = true;

                try
                {
                    // Remove selected class from previously selected border
                    if (_previouslySelectedBorder != null && _previouslySelectedBorder != border)
                    {
                        _previouslySelectedBorder.Classes.Remove("selected");
                    }

                    // Add selected class to clicked border immediately
                    border.Classes.Add("selected");
                    _previouslySelectedBorder = border;

                    // Set SelectedActionOrGroup to trigger the same behavior as tree view selection
                    viewModel.SelectedActionOrGroup = taggedScriptConfig;
                }
                finally
                {
                    // Reset flag after selection is complete
                    _isInternalSelection = false;
                }
            }
        }
    }

    private void ClearSearch_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ActionFilter = string.Empty;
        }
    }

    private void CategoryBadge_OnTapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is string category)
        {
            // Track this badge if not already tracked
            if (!_categoryBadges.Contains(border))
            {
                _categoryBadges.Add(border);
            }

            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SelectedCategoryFilter = category;
                // Gray state will be updated by PropertyChanged handler
            }
        }
    }

    private void UpdateCategoryBadgesGrayedState()
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        var selectedCategory = viewModel.SelectedCategoryFilter;
        var shouldGrayOut = !string.IsNullOrEmpty(selectedCategory) && selectedCategory != "All";

        // Update all tracked badges
        foreach (var badge in _categoryBadges.ToList())
        {
            if (badge.DataContext is string category)
            {
                if (shouldGrayOut && category != selectedCategory)
                {
                    badge.Classes.Add("grayed");
                }
                else
                {
                    badge.Classes.Remove("grayed");
                }
            }
        }
    }

    private void FindAndTrackAllCategoryBadges()
    {
        // Find all Border elements with the categoryBadge class
        var allBadges = this.GetVisualDescendants()
            .OfType<Border>()
            .Where(b => b.Classes.Contains("categoryBadge"))
            .ToList();

        foreach (var badge in allBadges)
        {
            if (!_categoryBadges.Contains(badge))
            {
                _categoryBadges.Add(badge);
            }
        }
    }
}