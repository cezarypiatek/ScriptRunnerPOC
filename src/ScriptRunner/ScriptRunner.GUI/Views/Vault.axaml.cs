using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ScriptRunner.GUI.ViewModels;
using Splat;

namespace ScriptRunner.GUI.Views
{
    public partial class Vault : Window
    {
        public Vault()
        {
            InitializeComponent();
            
            DataContext = this.ViewModel = Locator.Current.GetService<VaultViewModel>()!;
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public VaultViewModel ViewModel { get; set; }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CloseVaultDialog(object? sender, RoutedEventArgs e)
        {
            ViewModel.SaveVault();
            Close();
        }
    }
}
