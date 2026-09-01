using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views;

public partial class McpConfigWindow : Window
{
    public McpConfigWindowViewModel ViewModel { get; }

    public McpConfigWindow()
    {
        InitializeComponent();
        DataContext = ViewModel = new McpConfigWindowViewModel();
    }

    public McpConfigWindow(string actionKey)
    {
        InitializeComponent();
        DataContext = ViewModel = new McpConfigWindowViewModel(actionKey);
        Title = $"MCP Configuration - {actionKey}";
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        await ViewModel.SaveAndApplyAsync();
        Close();
    }
}
