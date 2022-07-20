using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ReactiveUI;
using ScriptRunner.GUI.Settings;

namespace ScriptRunner.GUI.ViewModels
{
    public static class VaultProvider
    {
        public static IReadOnlyList<VaultEntry> ReadFromVault()
        {
            var vaultPath = AppSettingsService.GetSettingsPathFor("Vault.dat");
            if (File.Exists(vaultPath))
            {
                File.Decrypt(vaultPath);
                var content = File.ReadAllText(vaultPath);
                File.Encrypt(vaultPath);
                var data = JsonSerializer.Deserialize<List<VaultEntry>>(content);
                return data ?? new List<VaultEntry>();
            }
            return Array.Empty<VaultEntry>();
        }

        public static void UpdateVault(List<VaultEntry> date)
        {
            var vaultPath = AppSettingsService.GetSettingsPathFor("Vault.dat");
            File.WriteAllText(vaultPath, JsonSerializer.Serialize(date), Encoding.UTF8);
            File.Encrypt(vaultPath);
        }
    }

    public class VaultViewModel : ViewModelBase
    {
        public ObservableCollection<VaultEntry> Entries
        {
            get => _entries;
            set => this.RaiseAndSetIfChanged(ref _entries, value);
        }

        private ObservableCollection<VaultEntry> _entries;


        public VaultViewModel()
        {
            Entries = new ObservableCollection<VaultEntry>(VaultProvider.ReadFromVault());
        }

        public void AddNewVaultEntry()
        {
            Entries.Add(new VaultEntry());
        }

        public void SaveVault()
        {
            var date = Entries.ToList();
            VaultProvider.UpdateVault(date);
        }
      
    }

    public class VaultEntry
    {
        public string Name { get; set; }
        public string Secret { get; set; }
    }
}
