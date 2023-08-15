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
            var recent = AppSettingsService.Load().Recent.Values.OrderByDescending(x => x.Timestamp).Take(5).ToArray();
            var popupContent = new SearchBox(viewModel?.Actions ?? new List<ScriptConfig>(), recent);

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var popup = desktop.MainWindow.Find<Popup>("PopupContainer");
                popup.Child = popupContent;
                popup.VerticalOffset = desktop.MainWindow.Height * 0.3;

                popupContent.KeyUp += (o, args) =>
                {
                    if (args.Key == Key.Escape)
                    {
                        popup.Close();
                    }
                };
                popup.Open();
                this.openSearchBoxEventHandler= Observable.FromEventPattern(popupContent, nameof(popupContent.ResultSelected)).Subscribe(pattern =>
                {
                    popup.Close();

                    if (pattern.EventArgs is SearchBox.ResultSelectedEventArgs {Result: { } selectedCommand})
                    {
                        viewModel.SelectedAction = selectedCommand.Config;
                        viewModel.SelectedArgumentSet = selectedCommand.ArgumentSet;
                    }
                });
            }
        }
    }
}