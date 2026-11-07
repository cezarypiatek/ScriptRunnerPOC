using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using ReactiveUI;
using ScriptRunner.GUI.Infrastructure;
using ScriptRunner.GUI.ViewModels;
using Splat;

namespace ScriptRunner.GUI.Views
{
    public class VaultPickerViewModel : ViewModelBase
    {
        public VaultPickerViewModel()
        {
        }

        public VaultPickerViewModel(VaultProvider vaultProvider)
        {
            try
            {
                Entries = vaultProvider.ReadFromVault()
                    .Where(entry => VaultKeyPolicy.IsUserSelectable(entry.Name))
                    .ToList();
            }
            catch (System.Exception exception)
            {
                Entries = new List<VaultEntry>();
                LoadError = $"The Vault could not be read. {exception.Message}";
            }
        }

        public string? LoadError { get; }
        
        public IReadOnlyList<VaultEntry> Entries { get; set; } = System.Array.Empty<VaultEntry>();

        public VaultEntry? SelectedEntry
        {
            get => _selectedEntry;
            set => this.RaiseAndSetIfChanged(ref _selectedEntry, value);
        }

        private VaultEntry? _selectedEntry;
    }

    public partial class VaultPicker : Window
    {
        public VaultPicker()
        {
            DataContext = this.ViewModel = Locator.Current.GetService<VaultPickerViewModel>()!;

            InitializeComponent();
            Opened += ShowLoadError;
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public VaultPickerViewModel ViewModel { get; set; }


        private void Accept(object? sender, RoutedEventArgs e)
        {
            if(SecretsCombo.SelectedItem is VaultEntry selectedEntry)
            {
                Close(new VaultEntryChoice()
                {
                    RememberBinding = this.Remember.IsChecked ?? false,
                    RememberBindingForSet = this.RememberForSet.IsChecked ?? false,
                    SelectedEntry = selectedEntry
                });
            }
            
        }

        private void Cancel(object? sender, RoutedEventArgs e) => Close();

        private async void ShowLoadError(object? sender, System.EventArgs e)
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
            Close();
        }

        private void Remember_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (Remember.IsChecked == true)
                RememberForSet.IsChecked = false;
        }

        private void RememberForSet_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (RememberForSet.IsChecked == true)
                Remember.IsChecked = false;
        }
    }

    public class VaultEntryChoice
    {
        public bool RememberBinding { get; set; }
        public bool RememberBindingForSet { get; set; }
        public required VaultEntry SelectedEntry { get; set; }
    }
}
