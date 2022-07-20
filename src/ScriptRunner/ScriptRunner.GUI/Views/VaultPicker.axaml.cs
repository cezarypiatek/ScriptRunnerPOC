using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views
{
    public partial class VaultPicker : Window
    {
        public VaultPicker()
        {

            DataContext = VaultProvider.ReadFromVault();
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }



        private void Accept(object? sender, RoutedEventArgs e)
        {
            Close((SecretsCombo.SelectedItem as VaultEntry)?.Secret);
        }
    }
}
