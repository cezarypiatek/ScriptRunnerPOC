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
    private bool _isUserScrolling = false;

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
        if (sender is ScrollViewer sc && sc.DataContext is RunningJobViewModel viewModel)
        {
            // If content was added (extent changed), auto-scroll if follow output is enabled
            if (e.ExtentDelta.Y > 0 && viewModel.FollowOutput)
            {
                _isUserScrolling = false;
                sc.ScrollToEnd();
            }
            // If user manually scrolled (offset changed without extent change)
            else if (e.OffsetDelta.Y != 0 && e.ExtentDelta.Y == 0)
            {
                _isUserScrolling = true;
                // Check if user scrolled away from bottom
                var isAtBottom = Math.Abs(sc.Offset.Y - sc.ScrollBarMaximum.Y) < 1.0;
                if (!isAtBottom && viewModel.FollowOutput)
                {
                    viewModel.FollowOutput = false;
                }
                // If user scrolled back to bottom, re-enable follow output
                else if (isAtBottom && !viewModel.FollowOutput)
                {
                    viewModel.FollowOutput = true;
                }
            }
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