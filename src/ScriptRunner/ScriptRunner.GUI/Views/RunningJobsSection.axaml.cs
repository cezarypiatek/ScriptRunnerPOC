using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
        if (sender is ScrollViewer sc && e.ExtentDelta.Y > 0)
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

    private void InputElement_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Height = Double.NaN;
        }
    }

    private void InputElement_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Height = 30;
        }
    }
}