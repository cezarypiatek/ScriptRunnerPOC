using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using ScriptRunner.GUI.ViewModels;
using Splat;

namespace ScriptRunner.GUI.Views
{
    public partial class Vault : Window
    {
        private bool _saveCompleted;

        public Vault()
        {
            InitializeComponent();
            
            DataContext = this.ViewModel = Locator.Current.GetService<VaultViewModel>()!;
            Closing += SaveBeforeClosing;
            Opened += ShowLoadError;
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
            Close();
        }

        private async void SaveBeforeClosing(object? sender, WindowClosingEventArgs e)
        {
            if (_saveCompleted)
            {
                return;
            }

            e.Cancel = true;
            if (ViewModel.LoadError is not null)
            {
                _saveCompleted = true;
                Close();
                return;
            }

            try
            {
                if (!ViewModel.TrySaveVault(out var validationError))
                {
                    var validationMessage = MessageBoxManager.GetMessageBoxStandard(
                        "Cannot save Vault",
                        validationError ?? "The Vault contains invalid entries.",
                        icon: MsBox.Avalonia.Enums.Icon.Warning);
                    await validationMessage.ShowWindowDialogAsync(this);
                    return;
                }

                _saveCompleted = true;
                Close();
            }
            catch (Exception exception)
            {
                var errorMessage = MessageBoxManager.GetMessageBoxStandard(
                    "Cannot save Vault",
                    $"The Vault could not be saved. {exception.Message}",
                    icon: MsBox.Avalonia.Enums.Icon.Error);
                await errorMessage.ShowWindowDialogAsync(this);
            }
        }

        private async void ShowLoadError(object? sender, EventArgs e)
        {
            if (ViewModel.LoadError is null)
            {
                return;
            }

            var errorMessage = MessageBoxManager.GetMessageBoxStandard(
                "Cannot open Vault",
                ViewModel.LoadError,
                icon: MsBox.Avalonia.Enums.Icon.Error);
            await errorMessage.ShowWindowDialogAsync(this);
        }
    }
}
