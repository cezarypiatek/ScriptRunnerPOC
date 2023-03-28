using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using ReactiveUI;
using ScriptRunner.GUI.Infrastructure;

namespace ScriptRunner.GUI.ViewModels
{
    public class VaultViewModel : ViewModelBase
    {
        private readonly VaultProvider _vaultProvider;

        public ObservableCollection<VaultEntry> Entries
        {
            get => _entries;
            set => this.RaiseAndSetIfChanged(ref _entries, value);
        }

        public void RemoveVaultEntry(VaultEntry entry)
        {
            Entries.Remove(entry);
        }

        private ObservableCollection<VaultEntry> _entries;

        public VaultViewModel()
        {
        }

        public VaultViewModel(VaultProvider vaultProvider)
        {
            RemoveVaultEntryCommand = ReactiveCommand.Create<VaultEntry>(RemoveVaultEntry);
            _vaultProvider = vaultProvider;
            Entries = new ObservableCollection<VaultEntry>(_vaultProvider.ReadFromVault());
        }

        public ReactiveCommand<VaultEntry, Unit> RemoveVaultEntryCommand { get;  }

        public void AddNewVaultEntry()
        {
            Entries.Add(new VaultEntry());
        }

        public void SaveVault()
        {
            var date = Entries.ToList();
            _vaultProvider.UpdateVault(date);
        }
    }

    public class VaultEntry
    {
        public string Name { get; set; }
        public string Secret { get; set; }
    }
}
