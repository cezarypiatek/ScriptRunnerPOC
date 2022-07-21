using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
                var contentEncrypted = File.ReadAllText(vaultPath);
                try
                {
                    var content = EncryptionHelper.Decrypt(contentEncrypted);
                    var data = JsonSerializer.Deserialize<List<VaultEntry>>(content);
                    return data ?? new List<VaultEntry>();
                }
                catch (Exception e)
                {
                    //TODO: Invalid key 
                    Console.WriteLine(e);
                    throw;
                }
            }
            return Array.Empty<VaultEntry>();
        }

        public static void UpdateVault(List<VaultEntry> data)
        {
            var vaultPath = AppSettingsService.GetSettingsPathFor("Vault.dat");
            File.WriteAllText(vaultPath, EncryptionHelper.Encrypt(JsonSerializer.Serialize(data)), Encoding.UTF8);
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
