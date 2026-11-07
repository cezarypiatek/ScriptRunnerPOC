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
        public string? LoadError { get; }

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
            try
            {
                Entries = new ObservableCollection<VaultEntry>(
                    _vaultProvider.ReadFromVault()
                        .Where(entry => !VaultKeyPolicy.IsInternal(entry.Name)));
            }
            catch (Exception exception)
            {
                Entries = new ObservableCollection<VaultEntry>();
                LoadError = $"The Vault could not be read. The original file will not be modified. {exception.Message}";
            }
            Entries.ToObservableChangeSet()
                .ToCollection()
                .Select(entries => entries ?? Array.Empty<VaultEntry>())
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.FilteredEntries, out _filteredEntries);
        }

        public ReactiveCommand<VaultEntry, Unit> RemoveVaultEntryCommand { get; }
        public ReactiveCommand<VaultEntry, Unit> ClearExpiryCommand { get; }

        public void AddNewVaultEntry()
        {
            Entries.Add(new VaultEntry());
        }

        public bool TrySaveVault(out string? validationError)
        {
            if (LoadError is not null)
            {
                validationError = LoadError;
                return false;
            }

            var reservedKeys = Entries
                .Where(entry => VaultKeyPolicy.IsInternal(entry.Name))
                .Select(entry => entry.Name!)
                .ToList();
            if (reservedKeys.Count > 0)
            {
                validationError = "Vault keys beginning with '!' are reserved for ScriptRunner. " +
                                  "Rename or remove the following entries:\n\n" +
                                  string.Join("\n", reservedKeys.Select(key => $"• {key}"));
                return false;
            }

            var publicEntries = Entries
                .Where(entry =>
                    !string.IsNullOrWhiteSpace(entry.Name) &&
                    !VaultKeyPolicy.IsInternal(entry.Name))
                .ToList();
            var duplicateKeys = publicEntries
                .GroupBy(entry => entry.Name!, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();
            if (duplicateKeys.Count > 0)
            {
                validationError = "Vault keys must be unique. Rename or remove the duplicate entries:\n\n" +
                                  string.Join("\n", duplicateKeys.Select(key => $"• {key}"));
                return false;
            }

            // The editor intentionally hides managed entries. Re-read and merge them at save time
            // so an MCP execution or another in-process workflow cannot be overwritten by the
            // editor's older hidden-entry snapshot. Managed duplicates from an interrupted legacy
            // update are repaired deterministically by keeping the newest serialized entry.
            var internalEntries = _vaultProvider.ReadFromVault()
                .Where(entry => VaultKeyPolicy.IsInternal(entry.Name))
                .GroupBy(entry => entry.Name!, StringComparer.Ordinal)
                .Select(group => group.Last())
                .ToList();
            var entries = publicEntries.Concat(internalEntries).ToList();

            _vaultProvider.UpdateVault(entries);
            validationError = null;
            return true;
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
