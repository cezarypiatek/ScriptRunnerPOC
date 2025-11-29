using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views;

public partial class ActionDetailsSection : UserControl
{
    private bool _isDetailsExpanded = false;
    private Border? _detailsSection;
    private TextBlock? _toggleDetailsText;

    public ActionDetailsSection()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _detailsSection = this.FindControl<Border>("DetailsSection");
        _toggleDetailsText = this.FindControl<TextBlock>("ToggleDetailsText");
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

    private void ToggleDetailsButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_detailsSection == null || _toggleDetailsText == null)
            return;

        _isDetailsExpanded = !_isDetailsExpanded;

        // Update button text
        _toggleDetailsText.Text = _isDetailsExpanded ? "Hide details" : "Show details";

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