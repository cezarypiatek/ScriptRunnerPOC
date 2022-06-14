using Avalonia.Controls;
using Avalonia.Layout;
using ScriptRunner.GUI.Settings;
using static System.Double;

namespace ScriptRunner.GUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
}