using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views;

public partial class SideMenu : UserControl
{
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
            var recent = AppSettingsService.Load().Recent.Values.OrderByDescending(x => x.Timestamp).Take(5).ToArray();
            var popup = new SearchBox(viewModel?.Actions ?? new List<ScriptConfig>(), recent);
        
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var sourceWindow =  desktop.MainWindow;
                popup.KeyUp += (o, args) =>
                {
                    if (args.Key == Key.Escape)
                    {
                        popup.Close();
                    }
                };
            
                if (await popup.ShowDialog<ScriptConfigWithArgumentSet>(sourceWindow) is { } selectedCommand)
                {
                    viewModel.SelectedAction = selectedCommand.Config;
                    viewModel.SelectedArgumentSet = selectedCommand.ArgumentSet;
                }
            }
        }
      
    }
}