using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
            Entries = vaultProvider.ReadFromVault().Where(x=>x.Name.StartsWith("!") == false).ToList();
        }
        
        public IReadOnlyList<VaultEntry> Entries { get; set; }

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
        public VaultEntry SelectedEntry { get; set; }
    }
}
