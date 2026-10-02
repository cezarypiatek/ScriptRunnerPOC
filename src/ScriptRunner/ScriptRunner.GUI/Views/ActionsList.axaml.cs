using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views;

public partial class ActionsList : UserControl
{
    private Border? _previouslySelectedBorder;
    private bool _isInternalSelection;

    public ActionsList()
    {
        InitializeComponent();
        this.DataContextChanged += OnDataContextChanged;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
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
        if (sender is Border border && border.DataContext is CategoryFilterOption filter)
        {
            var category = filter.Key;
            if (DataContext is MainWindowViewModel viewModel)
			{
				if (viewModel.SelectedCategoryFilter != category)
				{
					viewModel.SelectedCategoryFilter = category;
				}
				else if (category != MainWindowViewModel.AllCategoryFilter)
				{
					viewModel.SelectedCategoryFilter = MainWindowViewModel.AllCategoryFilter;
				}
			}
        }
    }
}
