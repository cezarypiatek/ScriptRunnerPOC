using System;
using System.Security.Cryptography;
using System.Text;

namespace ScriptRunner.GUI.ViewModels;

public static class EncryptionHelper
{
    private static byte[] EntropyKey = Encoding.ASCII.GetBytes("80CD0C6D-74D3-4E6D-9E4F-ECA485E69FC7");

    public static string Encrypt(string value)
    {
        byte[] data = Encoding.ASCII.GetBytes(value);
        string protectedData = Convert.ToBase64String(ProtectedData.Protect(data, EntropyKey, DataProtectionScope.CurrentUser));
        return protectedData;
    }

    public static string Decrypt(string value)
    {
        byte[] protectedData = Convert.FromBase64String(value);
        string data = Encoding.ASCII.GetString(ProtectedData.Unprotect(protectedData, EntropyKey, DataProtectionScope.CurrentUser));
        return data;
    }
}