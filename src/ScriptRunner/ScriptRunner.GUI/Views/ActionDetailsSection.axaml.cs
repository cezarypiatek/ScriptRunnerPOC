using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views;

public partial class ActionDetailsSection : UserControl
{
    private bool _isDetailsExpanded = false;
    private Border? _detailsSection;
    private TextBlock? _toggleDetailsText;
    private Projektanker.Icons.Avalonia.Icon? _toggleDetailsIcon;
    private ScrollViewer? _actionParametersScrollViewer;
    private Viewbox? _actionParametersViewbox;
    private ItemsControl? _actionParametersItemsControl;

    public ActionDetailsSection()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _detailsSection = this.FindControl<Border>("DetailsSection");
        _toggleDetailsText = this.FindControl<TextBlock>("ToggleDetailsText");
        _toggleDetailsIcon = this.FindControl<Projektanker.Icons.Avalonia.Icon>("ToggleDetailsIcon");
        _actionParametersScrollViewer = this.FindControl<ScrollViewer>("ActionParametersScrollViewer");
        _actionParametersViewbox = this.FindControl<Viewbox>("ActionParametersViewbox");
        _actionParametersItemsControl = this.FindControl<ItemsControl>("ActionParametersItemsControl");
        _actionParametersItemsControl?.AddHandler(SelectingItemsControl.SelectionChangedEvent, OnParameterGroupSelectionChanged);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async void SaveCurrentParametersAsPredefined(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (viewModel?.SelectedAction == null)
            {
                return;
            }
            var popup = new PredefinedParameterSaveWindow();
            popup.DataContext = new SavePredefinedParameterVM()
            {
                UseNew = true,
                ExistingSets = viewModel.SelectedAction.PredefinedArgumentSets.Select(x => x.Description).ToList(),
                SelectedExisting = viewModel.SelectedArgumentSet?.Description
            };
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var sourceWindow = desktop.MainWindow;
                if (await popup.ShowDialog<string>(sourceWindow) is { } setName && string.IsNullOrWhiteSpace(setName) == false)
                {
                    if (setName == MainWindowViewModel.DefaultParameterSetName)
                    {
                       viewModel.SaveAsDefault();
                    }
                    else
                    {
                        viewModel.SaveAsPredefined(setName);
                    }
                }
            }
        }
    }

    private void SplitButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if(sender is Button {Flyout: {}} sp)
        {
            if(sp.Flyout.IsOpen)
            {
                sp.Flyout.Hide();
            }
            else sp.Flyout.ShowAt(sp);
        }
    }

    private void OnActionPanelScrollChange(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer sc && e.ExtentDelta.Y > 0)
        {
            sc.ScrollToHome();
        }
    }

    private void FitParametersToggle_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggle ||
            _actionParametersScrollViewer == null ||
            _actionParametersViewbox == null ||
            _actionParametersItemsControl == null)
        {
            return;
        }

        var fitToArea = toggle.IsChecked == true;

        var groupFitHosts = _actionParametersItemsControl
            .GetVisualDescendants()
            .OfType<ParameterFitHost>()
            .ToList();
        if (groupFitHosts.Count > 0)
        {
            foreach (var host in groupFitHosts)
            {
                host.SetFitToArea(fitToArea);
            }
            return;
        }

        if (fitToArea)
        {
            _actionParametersScrollViewer.Content = null;
            _actionParametersViewbox.Child = _actionParametersItemsControl;
        }
        else
        {
            _actionParametersViewbox.Child = null;
            _actionParametersScrollViewer.Content = _actionParametersItemsControl;
        }

        _actionParametersScrollViewer.IsVisible = !fitToArea;
        _actionParametersViewbox.IsVisible = fitToArea;
    }

    private void OnParameterGroupSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var fitToArea = this.FindControl<ToggleButton>("FitParametersToggle")?.IsChecked == true;
            var hosts = _actionParametersItemsControl?
                .GetVisualDescendants()
                .OfType<ParameterFitHost>() ?? Enumerable.Empty<ParameterFitHost>();
            foreach (var host in hosts)
            {
                host.SetFitToArea(fitToArea);
            }
        });
    }

    private void ToggleDetailsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_detailsSection == null || _toggleDetailsText == null)
            return;

        _isDetailsExpanded = !_isDetailsExpanded;

        // Update button text and icon
        _toggleDetailsText.Text = _isDetailsExpanded ? "Hide details" : "Show details";
        if (_toggleDetailsIcon != null)
        {
            _toggleDetailsIcon.Value = _isDetailsExpanded ? "fas fa-chevron-up" : "fas fa-chevron-down";
        }

        if (_isDetailsExpanded)
        {
            // Measure content to get target height
            var content = _detailsSection.Child;
            if (content != null)
            {
                var parentWidth = (_detailsSection.Parent as Control)?.Bounds.Width ?? this.Bounds.Width;
                if (parentWidth <= 0)
                    parentWidth = 800;
                
                content.Measure(new Size(parentWidth, double.PositiveInfinity));
                
                // Simply set the properties - transitions will animate automatically
                _detailsSection.MaxHeight = content.DesiredSize.Height;
                _detailsSection.Opacity = 1.0;
            }
        }
        else
        {
            // Simply set the properties - transitions will animate automatically
            _detailsSection.MaxHeight = 0;
            _detailsSection.Opacity = 0;
        }
    }
}
