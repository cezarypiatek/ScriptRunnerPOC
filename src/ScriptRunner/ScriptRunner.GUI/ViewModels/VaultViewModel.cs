using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json.Serialization;
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

        public VaultViewModel(VaultProvider vaultProvider) : this()
        {
            RemoveVaultEntryCommand = ReactiveCommand.Create<VaultEntry>(RemoveVaultEntry);
            ClearExpiryCommand = ReactiveCommand.Create<VaultEntry>(entry => entry.ClearExpiry());
            _vaultProvider = vaultProvider;
            Entries = new ObservableCollection<VaultEntry>(_vaultProvider.ReadFromVault());
            Entries.ToObservableChangeSet()
                .ToCollection()
                .Select(entries => entries?.Where(x => (x.Name ?? "").StartsWith("!") == false) ?? Enumerable.Empty<VaultEntry>())
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.FilteredEntries, out _filteredEntries);
        }

        public ReactiveCommand<VaultEntry, Unit> RemoveVaultEntryCommand { get; }
        public ReactiveCommand<VaultEntry, Unit> ClearExpiryCommand { get; }

        public void AddNewVaultEntry()
        {
            Entries.Add(new VaultEntry());
        }

        public void SaveVault()
        {
            var entries = Entries.Where(x => string.IsNullOrWhiteSpace(x.Name) == false).ToList();
            _vaultProvider.UpdateVault(entries);
        }
    }

    public class VaultEntry : ReactiveObject
    {
        private string? _name;
        private string? _secret;
        private DateTimeOffset? _expiresAt;
        private decimal? _validForDays;
        private bool _isApplyingValidityPeriod;

        public string? Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public string? Secret
        {
            get => _secret;
            set => this.RaiseAndSetIfChanged(ref _secret, value);
        }

        public DateTimeOffset? ExpiresAt
        {
            get => _expiresAt;
            set
            {
                this.RaiseAndSetIfChanged(ref _expiresAt, value?.Date);
                if (!_isApplyingValidityPeriod)
                {
                    _validForDays = _expiresAt is { } expiry
                        ? Math.Max(0, (decimal)(expiry.Date - DateTimeOffset.Now.Date).TotalDays)
                        : null;
                    this.RaisePropertyChanged(nameof(ValidForDays));
                }
                this.RaisePropertyChanged(nameof(IsExpired));
                this.RaisePropertyChanged(nameof(HasExpiry));
                this.RaisePropertyChanged(nameof(ExpiryDisplay));
            }
        }

        [JsonIgnore]
        public decimal? ValidForDays
        {
            get => _validForDays;
            set
            {
                this.RaiseAndSetIfChanged(ref _validForDays, value);
                if (value is >= 0)
                {
                    _isApplyingValidityPeriod = true;
                    try
                    {
                        ExpiresAt = DateTimeOffset.Now.Date.AddDays((double)decimal.Truncate(value.Value));
                    }
                    finally
                    {
                        _isApplyingValidityPeriod = false;
                    }
                }
            }
        }

        [JsonIgnore]
        public bool IsExpired => ExpiresAt is { } expiry && expiry.Date < DateTimeOffset.Now.Date;

        [JsonIgnore]
        public bool HasExpiry => ExpiresAt != null;

        [JsonIgnore]
        public string ExpiryDisplay => ExpiresAt is { } expiry
            ? $"{(IsExpired ? "EXPIRED ·" : "Expires")} {expiry:yyyy-MM-dd}"
            : "No expiry";

        public void ClearExpiry()
        {
            _validForDays = null;
            this.RaisePropertyChanged(nameof(ValidForDays));
            ExpiresAt = null;
        }
    }
}
