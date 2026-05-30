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

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        await ViewModel.SaveAndApplyAsync();
    }
}
