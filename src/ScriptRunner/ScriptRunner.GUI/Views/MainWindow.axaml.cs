using System;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Mixins;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
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
            this.Bind(ViewModel, vm => vm.ActionFilter, v => v.ActionFilter.Text).DisposeWith(disposableRegistration);
            this.OneWayBind(ViewModel, vm => vm.FilteredActionList, v=>v.ActionTree.Items).DisposeWith(disposableRegistration);
        });
        Title = $"ScriptRunner {this.GetType().Assembly.GetName().Version}";
        if (AppSettingsService.Load().Layout is { } layoutSettings)
        {
            Width = layoutSettings.Width;
            Height = layoutSettings.Height;
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
}