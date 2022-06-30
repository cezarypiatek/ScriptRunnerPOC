using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindowViewModel ViewModel { get; }

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = this.ViewModel = new SettingsWindowViewModel();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CloseConfigSourceDialog(object? sender, RoutedEventArgs e)
        {
            ViewModel.SaveConfigScripts();
            Close();
        }
    }
}
