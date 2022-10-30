using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ScriptRunner.GUI.Infrastructure.DataProtection;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Infrastructure;

public class VaultProvider
{
    const string VaultFileName = "Vault.dat";

    private readonly IDataProtector _dataProtector;

    public VaultProvider(IDataProtector dataProtector)
    {
        _dataProtector = dataProtector;
    }

    public IReadOnlyList<VaultEntry> ReadFromVault()
    {
        var vaultPath = AppSettingsService.GetSettingsPathFor(VaultFileName);
        if (File.Exists(vaultPath))
        {
            var encryptedData = File.ReadAllBytes(vaultPath);

            try
            {
                var data = _dataProtector.Unprotect(encryptedData);
                return JsonSerializer.Deserialize<List<VaultEntry>>(data) ?? new List<VaultEntry>();
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

    public void UpdateVault(List<VaultEntry> entries)
    {
        var vaultPath = AppSettingsService.GetSettingsPathFor(VaultFileName);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(entries);
        var protectedBytes = _dataProtector.Protect(bytes);
        File.WriteAllBytes(vaultPath, protectedBytes);
    }
}