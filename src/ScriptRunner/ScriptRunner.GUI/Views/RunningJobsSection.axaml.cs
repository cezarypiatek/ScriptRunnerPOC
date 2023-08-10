using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views;

public partial class RunningJobsSection : UserControl
{
    public RunningJobsSection()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ScrollChangedHandler(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer sc && e.ExtentDelta.Length > 0)
        {
            sc.ScrollToEnd();
        }
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
}