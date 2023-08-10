using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
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
        Title = $"ScriptRunner {this.GetType().Assembly.GetName().Version}";
        if (AppSettingsService.Load().Layout is { } layoutSettings)
        {
            Width = Math.Max(layoutSettings.Width, 600);
            Height = Math.Max(layoutSettings.Height, 600);
            // MainGrid.ColumnDefinitions[0].Width = new GridLength(layoutSettings.ActionsPanelWidth);
            // MainGrid.RowDefinitions[2].Height = new GridLength(layoutSettings.RunningJobsPanelHeight);
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
            //AppSettingsService.UpdateLayoutSettings(settings =>
            // {
            //     settings.ActionsPanelWidth = (int) MainGrid.ColumnDefinitions[0].ActualWidth;
            //     settings.RunningJobsPanelHeight = (int) MainGrid.RowDefinitions[2].ActualHeight;
            // });
        };

    }
    
    

  

}