using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
            catch (CryptographicException)
            {
                // Fallback for loading vault data saved using old format 
                return ReadFromVaultFallback(vaultPath);
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

    private IReadOnlyList<VaultEntry> ReadFromVaultFallback(string vaultPath)
    {
        var contentEncrypted = File.ReadAllText(vaultPath);
        try
        {
            byte[] protectedData = Convert.FromBase64String(contentEncrypted);
            var data = _dataProtector.Unprotect(protectedData);
            return JsonSerializer.Deserialize<List<VaultEntry>>(data) ?? new List<VaultEntry>();
        }
        catch (Exception e)
        {
            //TODO: Invalid key 
            Console.WriteLine(e);
            throw;
        }
    }

    public void UpdateVault(List<VaultEntry> entries)
    {
        var vaultPath = AppSettingsService.GetSettingsPathFor(VaultFileName);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(entries);
        var protectedBytes = _dataProtector.Protect(bytes);
        File.WriteAllBytes(vaultPath, protectedBytes);
    }
}