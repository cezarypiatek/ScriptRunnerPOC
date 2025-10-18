using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views;

public partial class DatePickerOverlay : UserControl
{
    public DatePickerOverlay()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnOverlayClicked(object? sender, PointerPressedEventArgs e)
    {
        // Close the overlay when clicking on the background
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.IsDatePickerVisible = false;
        }
    }

    private void OnDateItemClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is DateGroupInfo dateInfo)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ScrollToDate(dateInfo.Date);
            }
        }
        e.Handled = true;
    }
}
