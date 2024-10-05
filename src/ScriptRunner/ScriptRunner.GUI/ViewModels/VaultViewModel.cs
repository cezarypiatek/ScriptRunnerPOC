using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
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


        private readonly ObservableAsPropertyHelper<IEnumerable<VaultEntry>> _filteredEntries;
        public IEnumerable<VaultEntry> FilteredEntries => _filteredEntries.Value;


        public void RemoveVaultEntry(VaultEntry entry)
        {
            Entries.Remove(entry);
        }

        private ObservableCollection<VaultEntry> _entries;

        public VaultViewModel()
        {

        }

        public VaultViewModel(VaultProvider vaultProvider):this()
        {
            RemoveVaultEntryCommand = ReactiveCommand.Create<VaultEntry>(RemoveVaultEntry);
            _vaultProvider = vaultProvider;
            Entries = new ObservableCollection<VaultEntry>(_vaultProvider.ReadFromVault());
            this.Entries.ToObservableChangeSet()
                .ToCollection()
                .Select(entries => entries?.Where(x => (x.Name ?? "").StartsWith("!") == false) ?? Enumerable.Empty<VaultEntry>())
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x=>x.FilteredEntries, out _filteredEntries);
        }

        public ReactiveCommand<VaultEntry, Unit> RemoveVaultEntryCommand { get;  }

        public void AddNewVaultEntry()
        {
            Entries.Add(new VaultEntry());
        }

        public void SaveVault()
        {
            var date = Entries.Where(x => string.IsNullOrWhiteSpace(x.Name) == false).ToList();
            _vaultProvider.UpdateVault(date);
        }
    }

    public class VaultEntry
    {
        public string Name { get; set; }
        public string Secret { get; set; }
    }
}
