using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views;

public partial class SideMenu : UserControl
{
    private IDisposable openSearchBoxEventHandler;

    public SideMenu()
    {
        
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OpenSearchBox(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            openSearchBoxEventHandler?.Dispose();
            var settings = AppSettingsService.Load();
            var recent = settings.Recent?.Values.OrderByDescending(x => x.Timestamp).ToArray() ?? Array.Empty<RecentAction>();
            var popupContent = new SearchBox(
                viewModel.Actions ?? new List<ScriptConfig>(),
                recent,
                settings.FavoriteActions,
                viewModel.ExecutionLog);

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime {MainWindow: {} mainWindow})
            {
                var overlay = mainWindow.Find<Panel>("SearchOverlay");
                var container = mainWindow.Find<Panel>("SearchBoxContainer");
                
                if (overlay == null || container == null)
                {
                    return;
                }
                
                // Clear previous content and add new SearchBox
                container.Children.Clear();
                container.Children.Add(popupContent);
                
                // Show the overlay
                overlay.IsVisible = true;

                // Handle escape key to close
                popupContent.KeyUp += (_, args) =>
                {
                    if (args.Key == Key.Escape)
                    {
                        overlay.IsVisible = false;
                        container.Children.Clear();
                    }
                };
                
                // Handle clicking on the overlay background to close
                overlay.PointerPressed += (_, args) =>
                {
                    // Only close if clicking on the overlay itself, not the search box
                    if (args.Source == overlay)
                    {
                        overlay.IsVisible = false;
                        container.Children.Clear();
                    }
                };
                
                this.openSearchBoxEventHandler = Observable.FromEventPattern(popupContent, nameof(popupContent.ResultSelected)).Subscribe(pattern =>
                {
                    overlay.IsVisible = false;
                    container.Children.Clear();

                    if (pattern.EventArgs is not SearchBox.ResultSelectedEventArgs selection)
                    {
                        return;
                    }

                    if (selection.RecentExecution is { } recentExecution)
                    {
                        viewModel.IsStatisticsVisible = false;
                        viewModel.IsScriptListVisible = true;
                        viewModel.OpenRecentExecution(recentExecution);
                    }
                    else if (selection is {Result: { } selectedCommand, AutoLaunch: var autoLaunch})
                    {
                        // A palette result belongs to the Actions view, so keep the side-menu
                        // state and the visible content in sync with the selected action.
                        viewModel.IsStatisticsVisible = false;
                        viewModel.IsScriptListVisible = true;
                        viewModel.SelectedAction = selectedCommand.Config;
                        viewModel.SelectedArgumentSet = selectedCommand.ArgumentSet;
                        if (autoLaunch)
                        {
                            viewModel.RunScript();
                        }
                    }
                });
            }
        }
    }
}
