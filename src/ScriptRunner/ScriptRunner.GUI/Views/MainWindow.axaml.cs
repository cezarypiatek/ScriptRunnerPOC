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
using ScriptRunner.GUI.Services;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.ViewModels;
using Splat;
using static System.Double;

namespace ScriptRunner.GUI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private readonly IDisposable effectiveViewportChangedObservable;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = Locator.Current.GetService<MainWindowViewModel>();
        Title = $"ScriptRunner {this.GetType().Assembly.GetName().Version}";
        
        // Set up notification service with window reference
        var notificationService = Locator.Current.GetService<INotificationService>();
        notificationService?.SetMainWindow(this);
        
        // Set up scroll action for date navigation
        if (ViewModel != null)
        {
            ViewModel.ScrollToDateAction = ScrollToDate;
        }
        
        if (AppSettingsService.Load().Layout is { } layoutSettings)
        {
            
            if (this.Screens.ScreenFromWindow(this) is { Bounds: var currentScreenBounds })
            {
                Width = Math.Min(layoutSettings.Width, currentScreenBounds.Width);
                Height = Math.Min(layoutSettings.Height, currentScreenBounds.Height);

                if (layoutSettings.Left + Width <= currentScreenBounds.Width &&
                    layoutSettings.Top + Height <= currentScreenBounds.Height)
                {
                    Position = new PixelPoint(layoutSettings.Left, layoutSettings.Top);
                }    
            }

            this.effectiveViewportChangedObservable = Observable.FromEventPattern<EffectiveViewportChangedEventArgs>
                (
                    h => this.EffectiveViewportChanged += h,
                    h => this.EffectiveViewportChanged -= h
                ).Throttle(TimeSpan.FromMilliseconds(200))
                .Skip(1)
                .Where(x => x.EventArgs.EffectiveViewport is { Width: not NaN, Height: not NaN, Left: not NaN, Top: not NaN})
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(pattern =>
                {
                    AppSettingsService.UpdateLayoutSettings(settings =>
                    {
                        settings.Width = (int)pattern.EventArgs.EffectiveViewport.Width;
                        settings.Height = (int)pattern.EventArgs.EffectiveViewport.Height;
                        settings.Left = (int)pattern.EventArgs.EffectiveViewport.Left;
                        settings.Top = (int)pattern.EventArgs.EffectiveViewport.Top;
                    });
                });
        }
    }
    
    private async void ScrollToDate(DateTime date)
    {
        // Find the ExecutionLogList control
        var executionLogList = this.FindControl<ExecutionLogList>("ExecutionLogListControl");
        if (executionLogList != null)
        {
            await executionLogList.ScrollToDate(date);
        }
    }
}