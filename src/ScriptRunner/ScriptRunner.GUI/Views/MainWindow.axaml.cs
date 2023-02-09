using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Mixins;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using ReactiveUI;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.ViewModels;
using Splat;
using static System.Double;

namespace ScriptRunner.GUI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        ViewModel = Locator.Current.GetService<MainWindowViewModel>();
        this.WhenActivated((disposableRegistration) =>
        {
            this.OneWayBind(ViewModel, vm => vm.FilteredActionList, v=>v.ActionTree.Items).DisposeWith(disposableRegistration);
        });
        Title = $"ScriptRunner {this.GetType().Assembly.GetName().Version}";
        if (AppSettingsService.Load().Layout is { } layoutSettings)
        {
            Width = Math.Max(layoutSettings.Width, 600);
            Height = Math.Max(layoutSettings.Height, 600);
            MainGrid.ColumnDefinitions[0].Width = new GridLength(layoutSettings.ActionsPanelWidth);
            MainGrid.RowDefinitions[2].Height = new GridLength(layoutSettings.RunningJobsPanelHeight);
        }

        EffectiveViewportChanged += (_, _) =>
        {
            if (Width is not NaN && Height is not NaN)
            {
                AppSettingsService.UpdateLayoutSettings(settings =>
                {
                    settings.Width = (int)Width;
                    settings.Height = (int)Height;
                });
            }
        };

   
        MainGrid.LayoutUpdated += (sender, args) =>
        {
            AppSettingsService.UpdateLayoutSettings(settings =>
            {
                settings.ActionsPanelWidth = (int) MainGrid.ColumnDefinitions[0].ActualWidth;
                settings.RunningJobsPanelHeight = (int) MainGrid.RowDefinitions[2].ActualHeight;
            });
        };

    }


    public void AcceptCommand(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (sender is TextBox {DataContext: RunningJobViewModel viewModel})
            {
                viewModel.AcceptCommand();
            }
        }
    }

    private void ScrollChangedHandler(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer sc && e.ExtentDelta.Length > 0)
        {
            sc.ScrollToEnd();
        }
    }

    public async void SaveAsPredefined(object? sender, RoutedEventArgs e)
    {
        if (this.ViewModel?.SelectedAction == null)
        {
            return;
        }
        var popup = new PredefinedParameterSaveWindow();
        popup.DataContext = new SavePredefinedParameterVM()
        {
            UseNew = true,
            ExistingSets = this.ViewModel.SelectedAction.PredefinedArgumentSets.Select(x => x.Description).ToList(),
            SelectedExisting = this.ViewModel.SelectedArgumentSet?.Description
        };
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var sourceWindow = (sender as IControl)?.GetVisualRoot() as Window ?? desktop.MainWindow;
            if (await popup.ShowDialog<string>(sourceWindow) is { } setName && string.IsNullOrWhiteSpace(setName) == false)
            {
                if (setName == MainWindowViewModel.DefaultParameterSetName)
                {
                    this.ViewModel.SaveAsDefault();
                }
                else
                {
                    this.ViewModel.SaveAsPredefined(setName);
                }
            }
        }

    }

    private async void OpenSearchBox(object? sender, RoutedEventArgs e)
    {
        var popup = new SearchBox(this.ViewModel.Actions);
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var sourceWindow = (sender as IControl)?.GetVisualRoot() as Window ?? desktop.MainWindow;
            popup.KeyUp += (o, args) =>
            {
                if (args.Key == Key.Escape)
                {
                    popup.Close();
                }
            };
            
            if (await popup.ShowDialog<ScriptConfig>(sourceWindow) is { } selectedCommand)
            {
                this.ViewModel.SelectedAction = selectedCommand;
            }
        }
    }

}