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
            var vaultPath = AppSettingsService.GetSettingsPathFor("Vault.dat");
            if (File.Exists(vaultPath))
            {
                File.Decrypt(vaultPath);
                var content = File.ReadAllText(vaultPath);
                File.Encrypt(vaultPath);
                var data = JsonSerializer.Deserialize<List<VaultEntry>>(content);
                Entries = new ObservableCollection<VaultEntry>(data?? new List<VaultEntry>());
            }
            else
            {
                Entries = new ObservableCollection<VaultEntry>()
                {

                };
            }
                
        }

        public void AddNewVaultEntry()
        {
            Entries.Add(new VaultEntry());
        }

        public void SaveVault()
        {
            var date = Entries.ToList();
            var vaultPath = AppSettingsService.GetSettingsPathFor("Vault.dat");
            File.WriteAllText(vaultPath, JsonSerializer.Serialize(date), Encoding.UTF8);
            File.Encrypt(vaultPath);
        }
    }

    public class VaultEntry
    {
        public string Name { get; set; }
        public string Secret { get; set; }
    }
}
